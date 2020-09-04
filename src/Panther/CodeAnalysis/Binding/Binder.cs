using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks.Sources;
using Mono.Cecil;
using Panther.CodeAnalysis.Lowering;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class Binder
    {
        private readonly bool _isScript;
        public DiagnosticBag Diagnostics { get; } = new DiagnosticBag();
        private int _labelCounter = 0;

        private readonly Stack<(BoundLabel BreakLabel, BoundLabel ContinueLabel)> _breakContinueLabels =
            new Stack<(BoundLabel, BoundLabel)>();

        public Binder(bool isScript)
        {
            _isScript = isScript;
        }

        public static BoundGlobalScope BindGlobalScope(bool isScript, BoundGlobalScope? previous,
            ImmutableArray<SyntaxTree> syntaxTrees, ImmutableArray<AssemblyDefinition> references)
        {
            var parentScope = CreateParentScope(previous, references);
            var rootNamespace = new NamespaceSymbol("");
            var scope = new BoundScope(parentScope, rootNamespace);
            var binder = new Binder(isScript);


            var defaultType = new BoundType("$Program");
            var defaultTypeAddedToNamespace = false;
            var defaultTypeScope = new BoundScope(scope, defaultType);

            var globalStatements = ImmutableArray.CreateBuilder<GlobalStatementSyntax>();

            foreach (var tree in syntaxTrees)
            {
                // TODO: global statements are not supported with a namespace
                var compilationUnit = tree.Root;

                foreach (var namespaceDirective in compilationUnit.NamespaceDirectives)
                {
                    var namespaceSymbol = binder.BindNamespace(namespaceDirective);
                    scope = scope.EnterNamespace(namespaceSymbol);
                }

                foreach (var @using in compilationUnit.Usings)
                {
                    var namespaceOrTypeSymbol = binder.BindUsingDirective(@using);
                    scope.ImportMembers(namespaceOrTypeSymbol);
                }

                // should only be one set of global statements
                // otherwise there would be an error
                globalStatements.AddRange(compilationUnit.Statements);

                foreach (var member in tree.Root.Members)
                {
                    switch (member)
                    {
                        case FunctionDeclarationSyntax function:
                            // put top level functions declarations into the defaultType scope
                            binder.BindFunctionDeclaration(function, defaultTypeScope);
                            break;

                        case ObjectDeclarationSyntax objectDeclaration:
                            // put top level object declarations into the global scope
                            binder.BindObjectDeclaration(objectDeclaration, scope);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(member));
                    }
                }
            }

            var statements =
                globalStatements
                    .Select(globalStatementSyntax => binder.BindGlobalStatement(globalStatementSyntax.Statement, defaultTypeScope))
                    .ToImmutableArray();

            // top level variables get converted to fields on the default type
            var variables = defaultTypeScope.GetDeclaredVariables().OfType<FieldSymbol>();

            foreach (var variable in variables)
            {
                defaultType.DefineSymbol(variable);
            }

            if (defaultType.GetMembers().Any())
            {
                rootNamespace.DefineSymbol(defaultType);
                defaultTypeAddedToNamespace = true;
            }

            var mains = from type in rootNamespace.GetTypeMembers()
                        from member in type.GetMembers().OfType<SourceMethodSymbol>()
                        where member.Name == "main"
                        select member;

            var mainFunction = mains.FirstOrDefault();

            var entryPoint = BindEntryPoint(defaultTypeScope, isScript, syntaxTrees, statements, mainFunction, binder);

            if (!defaultTypeAddedToNamespace && defaultType.GetMembers().Any())
            {
                rootNamespace.DefineSymbol(defaultType);
            }

            var diagnostics = binder.Diagnostics.ToImmutableArray();

            if (previous != null)
            {
                diagnostics = diagnostics.InsertRange(0, previous.Diagnostics);
            }

            diagnostics = Enumerable.Aggregate(syntaxTrees, diagnostics,
                (current, syntaxTree) => current.InsertRange(0, syntaxTree.Diagnostics));

            return new BoundGlobalScope(previous, diagnostics,
                defaultType,
                entryPoint,
                rootNamespace,
                references
            );
        }

        private NamespaceOrTypeSymbol BindUsingDirective(UsingDirectiveSyntax usingDirective)
        {
            throw new NotImplementedException();
        }

        private void BindObjectDeclaration(ObjectDeclarationSyntax objectDeclaration, BoundScope parent)
        {
            var type = new BoundType(objectDeclaration.Identifier.Text);
            if (!parent.DefineSymbol(type))
            {
                Diagnostics.ReportAmbiguousType(objectDeclaration.Location, objectDeclaration.Identifier.Text);
            }

            var scope = new BoundScope(parent, type);
            foreach (var member in objectDeclaration.Members)
            {
                switch (member)
                {
                    case FunctionDeclarationSyntax functionDeclarationSyntax:
                        BindFunctionDeclaration(functionDeclarationSyntax, scope);

                        break;
                    case ObjectDeclarationSyntax childObjectDeclaration:
                        BindObjectDeclaration(childObjectDeclaration, scope);

                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(member));
                }
            }
        }

        private NamespaceSymbol BindNamespace(NamespaceDirectiveSyntax namespaceDirective)
        {
            throw new NotImplementedException();
        }

        private static EntryPoint? BindEntryPoint(BoundScope boundScope, bool isScript,
            ImmutableArray<SyntaxTree> syntaxTrees, ImmutableArray<BoundStatement> globalStatements,
            SourceMethodSymbol? mainFunction, Binder binder) =>
            isScript
                ? BindScriptEntryPoint(boundScope, globalStatements)
                : BindMainEntryPoint(boundScope, syntaxTrees, globalStatements, mainFunction, binder);

        private static EntryPoint BindMainEntryPoint(BoundScope boundScope, ImmutableArray<SyntaxTree> syntaxTrees,
            ImmutableArray<BoundStatement> globalStatements, SourceMethodSymbol? mainFunction, Binder binder)
        {
            var hasGlobalStatements = globalStatements.Any();

            var firstStatementPerSyntaxTree =
                (from tree in syntaxTrees
                 let firstStatement = tree.Root.Statements.OfType<GlobalStatementSyntax>().FirstOrDefault()
                 where firstStatement != null
                 select firstStatement)
                .ToImmutableArray();

            if (mainFunction != null)
            {
                // ensure main function signature is correct
                if (mainFunction.Parameters.Any() || mainFunction.ReturnType != TypeSymbol.Unit)
                {
                    binder.Diagnostics.ReportMainMustHaveCorrectSignature(
                        mainFunction.Declaration.Identifier.Location
                    );
                }

                // if a main function exists, global statements cannot
                if (hasGlobalStatements)
                {
                    binder.Diagnostics.ReportCannotMixMainAndGlobalStatements(
                        mainFunction.Declaration.Identifier.Location
                    );

                    foreach (var firstStatement1 in firstStatementPerSyntaxTree)
                    {
                        binder.Diagnostics.ReportCannotMixMainAndGlobalStatements(firstStatement1.Location);
                    }
                }

                return new EntryPoint(false, mainFunction, null);
            }

            // Global statements can only exist in one syntax tree
            if (firstStatementPerSyntaxTree.Length > 1)
            {
                foreach (var firstStatement2 in firstStatementPerSyntaxTree)
                {
                    binder.Diagnostics.ReportGlobalStatementsCanOnlyExistInOneFile(firstStatement2.Location);
                }
            }

            var main = new ImportedMethodSymbol("main", ImmutableArray<ParameterSymbol>.Empty,
                TypeSymbol.Unit);
            var compilationUnit = globalStatements.First().Syntax;
            var body = Lowerer.Lower(BoundStatementFromStatements(compilationUnit, globalStatements));

            boundScope.DefineSymbol(main);

            return new EntryPoint(false, main, body);
        }

        private static EntryPoint? BindScriptEntryPoint(BoundScope boundScope, ImmutableArray<BoundStatement> globalStatements)
        {
            if (!globalStatements.Any())
                return null;

            var eval = new ImportedMethodSymbol("$eval",
                ImmutableArray<ParameterSymbol>.Empty,
                TypeSymbol.Any
            );

            var compilationUnit = globalStatements.First().Syntax;
            var boundStatementFromStatements = BoundStatementFromStatements(compilationUnit, globalStatements);

            // for our script function we need to return an object. if the expression is not an object then we will
            // create a conversion expression to convert it.
            if (boundStatementFromStatements.Expression.Type != TypeSymbol.Any)
            {
                // what should we do when we have a unit expression?
                boundStatementFromStatements = new BoundExpressionStatement(
                    boundStatementFromStatements.Syntax,
                    new BoundConversionExpression(
                        boundStatementFromStatements.Syntax,
                        TypeSymbol.Any,
                        boundStatementFromStatements.Expression
                    )
                );
            }

            boundScope.DefineSymbol(eval);

            return new EntryPoint(true, eval, Lowerer.Lower(boundStatementFromStatements));
        }

        public static BoundAssembly BindAssembly(bool isScript, BoundAssembly? previous, BoundGlobalScope globalScope)
        {
            var parentScope = CreateParentScope(globalScope, globalScope.References);
            var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

            diagnostics.AddRange(globalScope.Diagnostics);

            foreach (var boundType in globalScope.Types)
            {
                foreach (var methodSymbol in boundType.GetMembers().OfType<SourceMethodSymbol>())
                {
                    var binder = new Binder(isScript);
                    var functionScope = new BoundScope(parentScope, methodSymbol);

                    var body = binder.BindExpression(methodSymbol.Declaration.Body, functionScope);

                    var loweredBody = Lowerer.Lower(new BoundExpressionStatement(body.Syntax, body));

                    if (methodSymbol.ReturnType != TypeSymbol.Unit && !ControlFlowGraph.AllBlocksReturn(loweredBody))
                    {
                        binder.Diagnostics.ReportAllPathsMustReturn(methodSymbol.Declaration.Identifier.Location);
                    }

                    boundType.DefineFunctionBody(methodSymbol, loweredBody);
                }
            }

            // define entry point
            if (globalScope.DefaultType != null && globalScope.EntryPoint != null && globalScope.EntryPoint.Body != null)
            {
                globalScope.DefaultType.DefineFunctionBody(globalScope.EntryPoint.Symbol, globalScope.EntryPoint.Body);
            }

            return new BoundAssembly(
                previous,
                diagnostics.ToImmutableArray(),
                globalScope.EntryPoint,
                globalScope.References,
                globalScope.Types
            );
        }

        private static BoundExpressionStatement BoundStatementFromStatements(SyntaxNode syntax,
            IReadOnlyCollection<BoundStatement> statements)
        {
            var expr = (statements.LastOrDefault() as BoundExpressionStatement)?.Expression;
            var stmts = expr == null
                ? statements.ToImmutableArray()
                : statements.Take(statements.Count - 1).ToImmutableArray();

            // this doesnt really feel like the correct syntax as we should have something that encompasses all of the statements
            return new BoundExpressionStatement(syntax,
                new BoundBlockExpression(syntax, stmts, expr ?? new BoundUnitExpression(syntax)));
        }

        private void BindFunctionDeclaration(FunctionDeclarationSyntax syntax, BoundScope scope)
        {
            var parameters = ImmutableArray.CreateBuilder<ParameterSymbol>();

            var seenParamNames = new HashSet<string>();

            for (var index = 0; index < syntax.Parameters.Count; index++)
            {
                var parameterSyntax = syntax.Parameters[index];
                var parameterName = parameterSyntax.Identifier.Text;
                var parameterType = BindTypeAnnotation(parameterSyntax.TypeAnnotation);

                if (!seenParamNames.Add(parameterName))
                {
                    Diagnostics.ReportParameterAlreadyDeclared(parameterSyntax.Location, parameterName);
                }
                else
                {
                    var parameter = new ParameterSymbol(parameterName, parameterType, index);
                    parameters.Add(parameter);
                }
            }

            var type = BindOptionalTypeAnnotation(syntax.TypeAnnotation);
            if (type == null)
            {
                // HACK: temporarily bind to body so that we can detect the type
                // var tempFunction = new SourceMethodSymbol(syntax.Identifier.Text, parameters.ToImmutable(),
                //     TypeSymbol.Unit, syntax);
                var functionScope = new BoundScope(scope, new BoundType("<temp>"));
                foreach (var parameterSymbol in parameters)
                {
                    functionScope.DefineSymbol(parameterSymbol);
                }

                var expr = BindExpression(syntax.Body, functionScope);
                type = expr.Type;

                if (type == TypeSymbol.Error)
                    Debugger.Break();
            }

            var function = new SourceMethodSymbol(syntax.Identifier.Text, parameters.ToImmutable(), type, syntax);

            if (!scope.DefineSymbol(function))
            {
                Diagnostics.ReportAmbiguousMethod(syntax.Location, syntax.Identifier.Text,
                    parameters.Select(p => p.Name).ToImmutableArray());
            }
        }

        private BoundStatement BindGlobalStatement(StatementSyntax syntax, BoundScope scope) =>
            BindStatement(syntax, scope, true);

        private BoundStatement BindStatement(StatementSyntax syntax, BoundScope scope, bool isGlobal = false)
        {
            var statement = BindStatementInternal(syntax, scope);

            if (_isScript && isGlobal)
                return statement;

            var isAllowed = IsSideEffectStatement(statement);
            if (!isAllowed)
            {
                var exprStatementSyntax = (ExpressionStatementSyntax)syntax;
                Diagnostics.ReportInvalidExpressionStatement(exprStatementSyntax.Expression.Location);
            }

            return statement;
        }

        private static bool IsSideEffectStatement(BoundStatement statement)
        {
            if (statement is BoundExpressionStatement es)
                return IsSideEffectExpression(es.Expression);

            return true;
        }

        private static bool IsSideEffectExpression(BoundExpression expression)
        {
            var exprKind = expression.Kind;

            if (expression is BoundWhileExpression boundWhileExpression)
            {
                return IsSideEffectExpression(boundWhileExpression.Body);
            }

            if (expression is BoundForExpression boundForExpression)
            {
                return IsSideEffectExpression(boundForExpression.Body);
            }

            if (expression is BoundIfExpression boundIfExpression)
            {
                return IsSideEffectExpression(boundIfExpression.Then) || IsSideEffectExpression(boundIfExpression.Else);
            }

            if (expression is BoundBlockExpression blockExpression)
            {
                return blockExpression.Statements.Any(IsSideEffectStatement) ||
                       IsSideEffectExpression(blockExpression.Expression);
            }

            return exprKind == BoundNodeKind.ErrorExpression ||
                   exprKind == BoundNodeKind.AssignmentExpression ||
                   exprKind == BoundNodeKind.CallExpression;
        }

        private BoundStatement BindStatementInternal(StatementSyntax syntax, BoundScope scope) =>
            syntax switch
            {
                ExpressionStatementSyntax expressionStatementSyntax => BindExpressionStatement(
                    expressionStatementSyntax, scope),
                VariableDeclarationStatementSyntax variableDeclarationStatementSyntax =>
                    BindVariableDeclarationStatement(variableDeclarationStatementSyntax, scope),
                _ => throw new ArgumentOutOfRangeException(nameof(syntax))
            };

        private BoundStatement BindContinueStatement(ContinueExpressionSyntax syntax)
        {
            var label = GetContinueLabel();
            if (label == null)
            {
                Diagnostics.ReportInvalidBreakOrContinue(syntax.ContinueKeyword.Location, syntax.ContinueKeyword.Text);
                return new BoundExpressionStatement(syntax, new BoundErrorExpression(syntax));
            }

            return new BoundGotoStatement(syntax, label);
        }

        private BoundStatement BindBreakStatement(BreakExpressionSyntax syntax)
        {
            var label = GetBreakLabel();
            if (label == null)
            {
                Diagnostics.ReportInvalidBreakOrContinue(syntax.BreakKeyword.Location, syntax.BreakKeyword.Text);
                return new BoundExpressionStatement(syntax, new BoundErrorExpression(syntax));
            }

            return new BoundGotoStatement(syntax, label);
        }

        public BoundLabel? GetBreakLabel() =>
            _breakContinueLabels.Count == 0 ? null : _breakContinueLabels.Peek().BreakLabel;

        public BoundLabel? GetContinueLabel() =>
            _breakContinueLabels.Count == 0 ? null : _breakContinueLabels.Peek().ContinueLabel;

        private BoundStatement BindVariableDeclarationStatement(VariableDeclarationStatementSyntax syntax,
            BoundScope scope)
        {
            var isReadOnly = syntax.ValOrVarToken.Kind == SyntaxKind.ValKeyword;
            var boundExpression = BindExpression(syntax.Expression, scope);
            var type = BindOptionalTypeAnnotation(syntax.TypeAnnotation);
            var expressionType = type ?? boundExpression.Type;

            var converted = BindConversion(syntax.Expression.Location, boundExpression, expressionType);
            var variable = BindVariable(syntax.IdentifierToken, expressionType, isReadOnly,
                boundExpression.ConstantValue, scope);

            return new BoundVariableDeclarationStatement(syntax, variable, converted);
        }

        private TypeSymbol BindTypeAnnotation(TypeAnnotationSyntax syntaxTypeClause)
        {
            var type = LookupType(syntaxTypeClause.IdentifierToken.Text);
            if (type == null)
            {
                Diagnostics.ReportUndefinedType(syntaxTypeClause.IdentifierToken.Location,
                    syntaxTypeClause.IdentifierToken.Text);
                return TypeSymbol.Error;
            }

            return type;
        }

        private TypeSymbol? BindOptionalTypeAnnotation(TypeAnnotationSyntax? syntaxTypeClause) =>
            syntaxTypeClause == null ? null : BindTypeAnnotation(syntaxTypeClause);

        private VariableSymbol BindVariable(SyntaxToken identifier, TypeSymbol expressionType, bool isReadOnly,
            BoundConstant? constantValue,
            BoundScope scope)
        {
            var name = identifier.Text ?? "??";
            var declare = !identifier.IsInsertedToken;
            var variable = scope.IsGlobalScope
                ? (VariableSymbol)new FieldSymbol(name, isReadOnly, expressionType, constantValue)
                : new LocalVariableSymbol(name, isReadOnly, expressionType, constantValue);

            if (declare && !scope.DefineSymbol(variable))
            {
                Diagnostics.ReportVariableAlreadyDefined(identifier.Location, name);
            }

            return variable;
        }

        private BoundStatement BindExpressionStatement(ExpressionStatementSyntax syntax, BoundScope scope)
        {
            var expression = BindExpression(syntax.Expression, scope);

            return new BoundExpressionStatement(syntax,
                expression.Type == TypeSymbol.Error ? new BoundErrorExpression(syntax) : expression);
        }

        private BoundExpression BindExpression(ExpressionSyntax syntax, BoundScope scope) =>
            syntax switch
            {
                BreakExpressionSyntax breakStatementSyntax => BindStatementToExpression(
                    BindBreakStatement(breakStatementSyntax)),
                ContinueExpressionSyntax continueStatementSyntax => BindStatementToExpression(
                    BindContinueStatement(continueStatementSyntax)),
                LiteralExpressionSyntax literalExpressionSyntax => BindLiteralExpression(literalExpressionSyntax),
                AssignmentExpressionSyntax assignmentExpressionSyntax => BindAssignmentExpression(
                    assignmentExpressionSyntax, scope),
                BinaryExpressionSyntax binaryExpressionSyntax => BindBinaryExpression(binaryExpressionSyntax, scope),
                UnaryExpressionSyntax unaryExpressionSyntax => BindUnaryExpression(unaryExpressionSyntax, scope),
                GroupExpressionSyntax groupExpressionSyntax => BindGroupExpression(groupExpressionSyntax, scope),
                NameSyntax nameExpressionSyntax => BindNameExpression(nameExpressionSyntax, scope),
                BlockExpressionSyntax blockExpressionSyntax => BindBlockExpression(blockExpressionSyntax, scope),
                IfExpressionSyntax ifExpressionSyntax => BindIfExpression(ifExpressionSyntax, scope),
                WhileExpressionSyntax whileExpressionSyntax => BindWhileExpression(whileExpressionSyntax, scope),
                ForExpressionSyntax forExpressionSyntax => BindForExpression(forExpressionSyntax, scope),
                CallExpressionSyntax callExpressionSyntax => BindCallExpression(callExpressionSyntax, scope),
                MemberAccessExpressionSyntax memberAccessExpressionSyntax => BindMemberAccessExpression(
                    memberAccessExpressionSyntax, scope),
                UnitExpressionSyntax unit => BindUnitExpression(unit),
                _ => throw new Exception($"Unexpected syntax {syntax.Kind}")
            };

        private BoundExpression BindMemberAccessExpression(MemberAccessExpressionSyntax syntax, BoundScope scope)
        {
            // TODO: combine with BindMemberMethodAccess?
            var expr = BindExpression(syntax.Expression, scope);
            var type = expr.Type;
            var name = syntax.Name.Identifier.Text;
            var field = type.GetMembers(name).OfType<FieldSymbol>().FirstOrDefault();
            if (field != null)
            {
                return new BoundFieldExpression(syntax, name, field);
            }

            // TODO add property support

            Diagnostics.ReportMissingDefinition(syntax.Location, type, name);
            return new BoundErrorExpression(syntax);
        }

        private BoundExpression BindAssignmentExpression(AssignmentExpressionSyntax syntax, BoundScope scope)
        {
            // TODO support member access
            var boundLHS = BindExpression(syntax.Name, scope);
            if (boundLHS is BoundErrorExpression)
                return boundLHS;

            if (boundLHS is BoundVariableExpression variableExpression)
            {
                var variable = variableExpression.Variable;
                if (variable.IsReadOnly)
                {
                    Diagnostics.ReportReassignmentToVal(syntax.Name.Location, variable.Name);

                    return new BoundErrorExpression(syntax);
                }

                var boundRHS = BindExpression(syntax.Expression, boundLHS.Type, scope);

                var convertedExpression = BindConversion(syntax.Expression.Location, boundRHS, variable.Type);

                return new BoundAssignmentExpression(syntax, variable, convertedExpression);
            }

            Diagnostics.ReportNotAssignable(syntax.Name.Location);
            return new BoundErrorExpression(syntax);
        }

        // hack to convert a statement into an expression
        private static BoundBlockExpression BindStatementToExpression(BoundStatement bindBreakStatement) =>
            new BoundBlockExpression(bindBreakStatement.Syntax, ImmutableArray.Create(bindBreakStatement),
                new BoundUnitExpression(bindBreakStatement.Syntax));

        private BoundExpression BindExpression(ExpressionSyntax syntax, TypeSymbol type, BoundScope scope,
            bool allowExplicit = false)
        {
            var expression = BindExpression(syntax, scope);
            return BindConversion(syntax.Location, expression, type, allowExplicit);
        }

        private BoundExpression BindConversion(TextLocation location, BoundExpression expression, TypeSymbol type,
            bool allowExplicit = false)
        {
            var conversion = Conversion.Classify(expression.Type, type);

            if (!conversion.Exists)
            {
                if (expression.Type != TypeSymbol.Error && type != TypeSymbol.Error)
                {
                    Diagnostics.ReportCannotConvert(location, expression.Type, type);
                }

                return new BoundErrorExpression(expression.Syntax);
            }

            if (!allowExplicit && conversion.IsExplicit)
            {
                Diagnostics.ReportCannotConvertImplicitly(location, expression.Type, type);
            }

            if (conversion.IsIdentity)
                return expression;

            return new BoundConversionExpression(expression.Syntax, type, expression);
        }

        private BoundExpression BindMethodExpression(ExpressionSyntax syntax, BoundScope scope)
        {
            switch (syntax)
            {
                case IdentifierNameSyntax identifierNameSyntax:
                    return BindMethod(identifierNameSyntax, scope);

                case MemberAccessExpressionSyntax memberAccessExpressionSyntax:
                    return BindMemberMethodAccess(memberAccessExpressionSyntax, scope);

                default:
                    throw new ArgumentOutOfRangeException(nameof(syntax));
            }
        }

        private BoundExpression BindMemberMethodAccess(MemberAccessExpressionSyntax syntax, BoundScope scope)
        {
            // TODO: combine with BindMemberAccessExpression?
            var expr = BindExpression(syntax.Expression, scope);
            var type = expr.Type;
            var name = syntax.Name.Identifier.Text;
            var methods = type.GetMembers(name).OfType<MethodSymbol>().ToImmutableArray();
            if (methods.Length > 0)
            {
                return new BoundMethodExpression(syntax, name, methods);
            }

            Diagnostics.ReportMissingDefinition(syntax.Location, type, name);
            return new BoundErrorExpression(syntax);
        }

        private BoundExpression BindMethod(IdentifierNameSyntax syntax, BoundScope scope)
        {
            var name = syntax.ToText();
            var symbols = scope.LookupMethod(name);
            if (symbols.Length != 0)
                return new BoundMethodExpression(syntax, name, symbols);

            var variable = scope.LookupVariable(name);
            if (variable != null)
            {
                Diagnostics.ReportNotAFunction(syntax.Location, name);
            }
            else
            {
                Diagnostics.ReportUndefinedFunction(syntax.Location, name);
            }

            return new BoundErrorExpression(syntax);
        }

        private BoundExpression BindCallExpression(CallExpressionSyntax syntax, BoundScope scope)
        {
            // Bind Conversion
            if (syntax.Arguments.Count == 1 && syntax.Expression is IdentifierNameSyntax identifierNameSyntax)
            {
                var type = LookupType(identifierNameSyntax.Identifier.Text);
                if (type != null)
                {
                    return BindExpression(syntax.Arguments[0], type, scope, allowExplicit: true);
                }
            }

            // bind regular functions
            var expression = BindMethodExpression(syntax.Expression, scope);
            if (expression is BoundErrorExpression)
                return expression;

            if (expression is BoundMethodExpression boundMethodExpression)
            {
                var boundArguments = syntax.Arguments.Select(argument => BindExpression(argument, scope)).ToList();
                var methods = boundMethodExpression.Methods.Where(x => x.Parameters.Length == boundArguments.Count)
                    .ToImmutableArray();

                if (methods.Length == 0)
                {
                    Diagnostics.ReportNoOverloads(syntax.Expression.Location, boundMethodExpression.Name,
                        boundArguments.Select(arg => arg.Type.Name).ToImmutableArray());

                    return new BoundErrorExpression(syntax);
                }

                if (methods.Length > 1)
                {
                    // TODO: improve type detection and binding when multiple valid methods exist
                    Diagnostics.ReportAmbiguousMethod(syntax.Expression.Location, boundMethodExpression.Name,
                        boundArguments.Select(arg => arg.Type.Name).ToImmutableArray());

                    return new BoundErrorExpression(syntax);
                }

                var convertedArgs = ImmutableArray.CreateBuilder<BoundExpression>();
                for (int i = 0; i < syntax.Arguments.Count; i++)
                {
                    var argument = boundArguments[i];
                    var parameter = methods[0].Parameters[i];
                    var convertedArgument = BindConversion(syntax.Arguments[i].Location, argument, parameter.Type);
                    convertedArgs.Add(convertedArgument);
                }

                return new BoundCallExpression(syntax, methods[0], convertedArgs.ToImmutable());
            }

            Diagnostics.ReportUnsupportedFunctionCall(syntax.Expression.Location);
            return new BoundErrorExpression(syntax);
        }

        private TypeSymbol? LookupType(string text)
        {
            var types = new[]
            {
                TypeSymbol.Any,
                TypeSymbol.Int,
                TypeSymbol.Bool,
                TypeSymbol.String,
                TypeSymbol.Unit,
            };

            return types.FirstOrDefault(type => type.Name == text);
        }

        private BoundExpression BindForExpression(ForExpressionSyntax syntax, BoundScope scope)
        {
            var lowerBound = BindExpression(syntax.FromExpression, scope);
            var upperBound = BindExpression(syntax.ToExpression, scope);

            if (lowerBound.Type != TypeSymbol.Int)
            {
                Diagnostics.ReportTypeMismatch(syntax.FromExpression.Location, TypeSymbol.Int, lowerBound.Type);
                return new BoundErrorExpression(syntax);
            }

            if (upperBound.Type != TypeSymbol.Int)
            {
                Diagnostics.ReportTypeMismatch(syntax.ToExpression.Location, TypeSymbol.Int, upperBound.Type);
                return new BoundErrorExpression(syntax);
            }

            var newScope = new BoundScope(scope);
            var variable = BindVariable(syntax.Variable, TypeSymbol.Int, true, null, newScope);

            var body = BindLoopBody(syntax.Body, newScope, out var breakLabel, out var continueLabel);

            return new BoundForExpression(syntax, variable, lowerBound, upperBound, body, breakLabel, continueLabel);
        }

        private BoundExpression BindWhileExpression(WhileExpressionSyntax syntax, BoundScope scope)
        {
            var condition = BindExpression(syntax.ConditionExpression, TypeSymbol.Bool, scope);
            var expr = BindLoopBody(syntax.Body, scope, out var breakLabel, out var continueLabel);

            return new BoundWhileExpression(syntax, condition, expr, breakLabel, continueLabel);
        }

        private BoundExpression BindLoopBody(ExpressionSyntax syntax, BoundScope scope, out BoundLabel breakLabel,
            out BoundLabel continueLabel)
        {
            _labelCounter++;
            breakLabel = new BoundLabel($"break{_labelCounter}");
            continueLabel = new BoundLabel($"continue{_labelCounter}");

            _breakContinueLabels.Push((breakLabel, continueLabel));

            try
            {
                return BindExpression(syntax, scope);
            }
            finally
            {
                _breakContinueLabels.Pop();
            }
        }

        private BoundExpression BindIfExpression(IfExpressionSyntax syntax, BoundScope scope)
        {
            var condition = BindExpression(syntax.ConditionExpression, scope);
            var then = BindExpression(syntax.ThenExpression, scope);
            var elseExpr = BindExpression(syntax.ElseExpression, scope);

            if (condition.Type == TypeSymbol.Error || then.Type == TypeSymbol.Error ||
                elseExpr.Type == TypeSymbol.Error)
                return new BoundErrorExpression(syntax);

            if (then.Type != elseExpr.Type)
            {
                Diagnostics.ReportTypeMismatch(syntax.ElseExpression.Location, then.Type, elseExpr.Type);
                return new BoundErrorExpression(syntax);
            }

            if (condition.Type != TypeSymbol.Bool)
            {
                Diagnostics.ReportTypeMismatch(syntax.ConditionExpression.Location, TypeSymbol.Bool, condition.Type);
                return new BoundErrorExpression(syntax);
            }

            return new BoundIfExpression(syntax, condition, then, elseExpr);
        }

        private BoundExpression BindUnitExpression(UnitExpressionSyntax syntax) => new BoundUnitExpression(syntax);

        private BoundExpression BindBlockExpression(BlockExpressionSyntax syntax, BoundScope scope)
        {
            var blockScope = new BoundScope(scope);
            var stmts = syntax.Statements.Select(stmt => BindStatement(stmt, blockScope)).ToImmutableArray();

            var expr = BindExpression(syntax.Expression, blockScope);

            return new BoundBlockExpression(syntax, stmts, expr);
        }

        private static BoundScope? CreateParentScope(BoundGlobalScope? previous, ImmutableArray<AssemblyDefinition> references)
        {
            var stack = new Stack<BoundGlobalScope>();

            while (previous != null)
            {
                stack.Push(previous);
                previous = previous.Previous;
            }

            var parent = CreateRootScope(references);

            while (stack.Count > 0)
            {
                previous = stack.Pop();
                var scope = new BoundScope(parent);

                // import all members from the previous default type which is where top level

                var declaringType = previous.DefaultType;
                if (declaringType != null)
                {
                    scope.ImportMembers(declaringType);
                }

                foreach (var type in previous.Types)
                {
                    scope.Import(type);
                }

                parent = scope;
            }

            return parent;
        }

        private static BoundScope CreateRootScope(ImmutableArray<AssemblyDefinition> references)
        {
            var rootScope = new BoundScope(null);

            // find Predef and add its functions
            var importedTypes =
                from assemblyDefinition in references
                from module in assemblyDefinition.Modules
                let predef = module.GetType("Panther.Predef")
                where predef != null
                // TODO: keep track of this import so we can know we need the assembly reference
                // TODO: this should be imported into the `Panther` namespace
                select new ImportedTypeSymbol("Predef", predef);

            var importedTypeSymbol = importedTypes.FirstOrDefault();
            if (importedTypeSymbol != null)
            {
                rootScope.Import(importedTypeSymbol);
                rootScope.ImportMembers(importedTypeSymbol);
            }

            return rootScope;
        }

        private BoundExpression BindNameExpression(NameSyntax syntax, BoundScope scope)
        {
            if (syntax is IdentifierNameSyntax simpleName)
            {
                var ident = simpleName.Identifier;
                if (ident.IsInsertedToken)
                {
                    return new BoundErrorExpression(syntax);
                }

                var name = ident.Text;

                var variable = scope.LookupVariable(name);
                if (variable != null)
                    return new BoundVariableExpression(syntax, variable);

                var type = scope.LookupType(name);
                if (type != null)
                    return new BoundTypeExpression(syntax, type);

                Diagnostics.ReportUndefinedName(ident.Location, name);
                return new BoundErrorExpression(syntax);
            }

            Diagnostics.ReportUnsupportedFieldAccess(syntax.Location, "<err>");
            return new BoundErrorExpression(syntax);
        }

        private BoundExpression BindGroupExpression(GroupExpressionSyntax syntax, BoundScope scope) =>
            BindExpression(syntax.Expression, scope);

        private BoundExpression BindUnaryExpression(UnaryExpressionSyntax syntax, BoundScope scope)
        {
            var boundOperand = BindExpression(syntax.Operand, scope);
            if (boundOperand.Type == TypeSymbol.Error)
                return new BoundErrorExpression(syntax);

            var boundOperator = BoundUnaryOperator.Bind(syntax.OperatorToken.Kind, boundOperand.Type);

            if (boundOperator == null)
            {
                Diagnostics.ReportUndefinedUnaryOperator(syntax.OperatorToken.Location, syntax.OperatorToken.Text,
                    boundOperand.Type);
                return new BoundErrorExpression(syntax);
            }

            return new BoundUnaryExpression(syntax, boundOperator, boundOperand);
        }

        private BoundExpression BindBinaryExpression(BinaryExpressionSyntax syntax, BoundScope scope)
        {
            var left = BindExpression(syntax.Left, scope);
            var right = BindExpression(syntax.Right, scope);
            var boundOperator = BoundBinaryOperator.Bind(syntax.OperatorToken.Kind, left.Type, right.Type);

            if (left.Type == TypeSymbol.Error || right.Type == TypeSymbol.Error)
                return new BoundErrorExpression(syntax);

            if (boundOperator == null)
            {
                Diagnostics.ReportUndefinedBinaryOperator(syntax.OperatorToken.Location, syntax.OperatorToken.Text,
                    left.Type, right.Type);
                return new BoundErrorExpression(syntax);
            }

            return new BoundBinaryExpression(syntax, left, boundOperator, right);
        }

        private BoundExpression BindLiteralExpression(LiteralExpressionSyntax syntax)
        {
            var value = syntax.Value;
            return value == null
                ? (BoundExpression)new BoundErrorExpression(syntax)
                : new BoundLiteralExpression(syntax, value);
        }
    }
}