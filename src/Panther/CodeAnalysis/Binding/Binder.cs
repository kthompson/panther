using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using Mono.Cecil;
using Panther.CodeAnalysis.Lowering;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;
using Panther.CodeAnalysis.Text;
using Type = Panther.CodeAnalysis.Symbols.Type;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class Binder
    {
        private readonly bool _isScript;
        public DiagnosticBag Diagnostics { get; } = new DiagnosticBag();
        private int _labelCounter = 0;

        private readonly Stack<(BoundLabel BreakLabel, BoundLabel ContinueLabel)> _breakContinueLabels = new();
        private readonly Dictionary<Symbol, FunctionDeclarationSyntax> _functionDeclarations = new();
        private readonly Dictionary<Symbol, BoundBlockExpression> _constructorBodies = new();

        public Binder(bool isScript)
        {
            _isScript = isScript;
        }

        private void BindClassDeclaration(ClassDeclarationSyntax syntax, BoundScope scope)
        {
            var typeSymbol = scope.Symbol.NewClass(syntax.Identifier.Location, syntax.Identifier.Text);

            if (!scope.DefineSymbol(typeSymbol))
            {
                Diagnostics.ReportAmbiguousType(syntax.Location, syntax.Identifier.Text);
            }

            var parameters = ImmutableArray.CreateBuilder<Symbol>();
            var assignments = ImmutableArray.CreateBuilder<BoundStatement>();
            var seenFieldNames = new HashSet<string>();

            var ctor = typeSymbol.NewMethod(typeSymbol.Location, ".ctor");

            for (var index = 0; index < syntax.Fields.Count; index++)
            {
                var field = syntax.Fields[index];
                var fieldName = field.Identifier.Text;
                var fieldType = BindTypeAnnotation(field.TypeAnnotation, scope).Type;
                var fieldSymbol = typeSymbol.NewField(field.Identifier.Location, field.Identifier.Text, false)
                    .WithType(fieldType);

                typeSymbol.DefineSymbol(fieldSymbol);

                if (!seenFieldNames.Add(fieldName))
                {
                    Diagnostics.ReportParameterAlreadyDeclared(field.Location, fieldName);
                }
                else
                {
                    // +1 since arg 0 is `this`
                    var parameter = ctor.NewParameter(field.Identifier.Location, fieldName, index + 1).WithType(fieldType);
                    ctor.DefineSymbol(parameter);
                    parameters.Add(parameter);
                    assignments.Add(new BoundAssignmentStatement(field, fieldSymbol,
                        new BoundVariableExpression(field, parameter)));
                }
            }

            var typeScope = new BoundScope(scope, typeSymbol);
            var immParams = parameters.ToImmutableArray();
            ctor.WithType(new MethodType(immParams, Type.Unit));

            typeSymbol.DefineSymbol(ctor);
            _constructorBodies.Add(ctor, new BoundBlockExpression(syntax, assignments.ToImmutableArray(), new BoundUnitExpression(syntax)));

            if (syntax.Template != null)
            {
                BindMembers(syntax.Template.Members, typeScope);
            }
        }

        private Symbol BindUsingDirective(UsingDirectiveSyntax usingDirective)
        {
            throw new NotImplementedException();
        }

        private void BindObjectDeclaration(ObjectDeclarationSyntax objectDeclaration, BoundScope parent)
        {
            var type = new BoundType(parent.Symbol,
                    objectDeclaration.Identifier.Location,
                    objectDeclaration.Identifier.Text)
                .WithFlags(SymbolFlags.Object);
            if (!parent.DefineSymbol(type))
            {
                Diagnostics.ReportAmbiguousType(objectDeclaration.Location, objectDeclaration.Identifier.Text);
            }

            var scope = new BoundScope(parent, type);

            BindMembers(objectDeclaration.Template.Members, scope);
        }

        private void BindMembers(ImmutableArray<MemberSyntax> members, BoundScope scope)
        {
            var (objectsAndClasses, rest) =  members.Partition(member => member is ObjectDeclarationSyntax or ClassDeclarationSyntax);
            var (functions, statements) = rest.Partition(member => member is FunctionDeclarationSyntax);

            // define classes and objects first
            foreach (var member in objectsAndClasses)
            {
                switch (member)
                {
                    case ObjectDeclarationSyntax childObjectDeclaration:
                        BindObjectDeclaration(childObjectDeclaration, scope);
                        break;

                    case ClassDeclarationSyntax classDeclaration:
                        BindClassDeclaration(classDeclaration, scope);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(member));
                }
            }

            foreach (var member in functions)
            {
                switch (member)
                {
                    case FunctionDeclarationSyntax functionDeclarationSyntax:
                        var sym = BindFunctionDeclaration(functionDeclarationSyntax, scope);
                        _functionDeclarations.Add(sym, functionDeclarationSyntax);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(member));
                }
            }

            // // TODO
            // foreach (var statement in statements)
            // {
            //
            //     BindStatement(statement);
            // }
        }

        // private Symbol BindNamespace(NamespaceDirectiveSyntax namespaceDirective)
        // {
        //     throw new NotImplementedException();
        // }

        private static EntryPoint? BindEntryPoint(BoundScope boundScope, bool isScript,
            ImmutableArray<SyntaxTree> syntaxTrees, ImmutableArray<BoundStatement> globalStatements,
            Symbol? mainFunction, Binder binder) =>
            isScript
                ? BindScriptEntryPoint(boundScope, globalStatements)
                : BindMainEntryPoint(boundScope, syntaxTrees, globalStatements, mainFunction, binder);

        private static EntryPoint BindMainEntryPoint(BoundScope boundScope, ImmutableArray<SyntaxTree> syntaxTrees,
            ImmutableArray<BoundStatement> globalStatements, Symbol? mainFunction, Binder binder)
        {
            var hasGlobalStatements = globalStatements.Any();

            var firstStatementPerSyntaxTree =
                (from tree in syntaxTrees
                 let firstStatement = tree.Root.Members.OfType<GlobalStatementSyntax>().FirstOrDefault()
                 where firstStatement != null
                 select firstStatement)
                .ToImmutableArray();

            if (mainFunction != null)
            {
                // ensure main function signature is correct
                if (mainFunction.Parameters.Any() || (mainFunction.ReturnType != Type.Unit && mainFunction.ReturnType != Type.Int))
                {
                    binder.Diagnostics.ReportMainMustHaveCorrectSignature(
                        mainFunction.Location
                    );
                }

                // if a main function exists, global statements cannot
                if (hasGlobalStatements)
                {
                    binder.Diagnostics.ReportCannotMixMainAndGlobalStatements(
                        mainFunction.Location
                    );

                    foreach (var firstStatement1 in firstStatementPerSyntaxTree)
                    {
                        binder.Diagnostics.ReportCannotMixMainAndGlobalStatements(firstStatement1!.Location);
                    }
                }

                return new EntryPoint(false, mainFunction, null);
            }

            // Global statements can only exist in one syntax tree
            if (firstStatementPerSyntaxTree.Length > 1)
            {
                foreach (var firstStatement2 in firstStatementPerSyntaxTree)
                {
                    binder.Diagnostics.ReportGlobalStatementsCanOnlyExistInOneFile(firstStatement2!.Location);
                }
            }


            var main = boundScope.Symbol
                .NewMethod(TextLocation.None, "main")
                .WithType(new MethodType(ImmutableArray<Symbol>.Empty, Type.Unit))
                .WithFlags(SymbolFlags.Static);
            var compilationUnit = globalStatements.First().Syntax;
            var body = LoweringPipeline.Lower(main, BoundStatementFromStatements(compilationUnit, globalStatements));

            boundScope.DefineSymbol(main);

            return new EntryPoint(false, main, body);
        }

        private static EntryPoint? BindScriptEntryPoint(BoundScope boundScope, ImmutableArray<BoundStatement> globalStatements)
        {
            if (!globalStatements.Any())
                return null;

            var eval = boundScope.Symbol
                .NewMethod(TextLocation.None, "$eval")
                .WithType(Type.Any)
                .WithFlags(SymbolFlags.Static);

            var compilationUnit = globalStatements.First().Syntax;
            var boundStatementFromStatements = BoundStatementFromStatements(compilationUnit, globalStatements);

            // for our script function we need to return an object. if the expression is not an object then we will
            // create a conversion expression to convert it.
            if (boundStatementFromStatements.Expression.Type != Type.Any)
            {
                // what should we do when we have a unit expression?
                boundStatementFromStatements = new BoundExpressionStatement(
                    boundStatementFromStatements.Syntax,
                    new BoundConversionExpression(
                        boundStatementFromStatements.Syntax,
                        Type.Any,
                        boundStatementFromStatements.Expression
                    )
                );
            }

            boundScope.DefineSymbol(eval);

            return new EntryPoint(true, eval, LoweringPipeline.Lower(eval, boundStatementFromStatements));
        }

        private static bool IsTopLevelDeclaration(SyntaxNode member) =>
            member.Kind is SyntaxKind.NamespaceDeclaration or SyntaxKind.ClassDeclaration or SyntaxKind
                .ObjectDeclaration;

        public static BoundAssembly BindAssembly(bool isScript, ImmutableArray<SyntaxTree> syntaxTrees, BoundAssembly? previous,  ImmutableArray<AssemblyDefinition> references)
        {
            var (root, parentScope) = CreateParentScope(previous, references);
            var scope = new BoundScope(parentScope, root);
            var binder = new Binder(isScript);

            var defaultType = new BoundType(root, TextLocation.None, "$Program") {Flags = SymbolFlags.Object};
            var defaultTypeAddedToNamespace = false;
            var defaultTypeScope = new BoundScope(scope, defaultType);

            var globalStatements = ImmutableArray.CreateBuilder<GlobalStatementSyntax>();

            foreach (var tree in syntaxTrees)
            {
                var compilationUnit = tree.Root;
                var allTopLevel = compilationUnit.Members.All(IsTopLevelDeclaration);
                var allGlobalStatements = compilationUnit.Members.All(member => member is GlobalStatementSyntax);

                if (!allTopLevel || !allGlobalStatements)
                {
                    // TODO: global statements are not supported with a namespace
                }

                // foreach (var memberSyntax in compilationUnit.Members)
                // {
                //
                // }

                // foreach (var namespaceDirective in compilationUnit.NamespaceDirectives)
                // {
                //     var namespaceSymbol = binder.BindNamespace(namespaceDirective);
                //     scope = scope.EnterNamespace(namespaceSymbol);
                // }

                foreach (var @using in compilationUnit.Usings)
                {
                    var namespaceOrTypeSymbol = binder.BindUsingDirective(@using);
                    scope.ImportMembers(namespaceOrTypeSymbol);
                }

                // should only be one set of global statements
                // otherwise there would be an error
                globalStatements.AddRange(compilationUnit.Members.OfType<GlobalStatementSyntax>());

                var (functions, objectsAndClasses) = tree.Root.Members.Partition(member => member is FunctionDeclarationSyntax);

                // bind classes and objects first
                binder.BindMembers(objectsAndClasses, scope);

                // functions go into our default type scope
                // everything else can go in our global scope
                binder.BindMembers(functions, defaultTypeScope);
            }

            var statements =
                globalStatements
                    .Select(globalStatementSyntax => binder.BindGlobalStatement(globalStatementSyntax.Statement, defaultTypeScope))
                    .ToImmutableArray();

            // top level variables get converted to fields on the default type
            var variables = defaultTypeScope.GetDeclaredVariables().Where(sym => sym.IsField);

            foreach (var variable in variables)
            {
                defaultType.DefineSymbol(variable);
            }

            if (defaultType.Members.Any())
            {
                root.DefineSymbol(defaultType);
                defaultTypeAddedToNamespace = true;
            }

            var mains = from type in root.Types
                from member in type.Methods
                where member.Name == "main"
                select member;

            var mainFunction = mains.FirstOrDefault();

            var entryPoint = BindEntryPoint(defaultTypeScope, isScript, syntaxTrees, statements, mainFunction, binder);

            if (!defaultTypeAddedToNamespace && defaultType.Members.Any())
            {
                root.DefineSymbol(defaultType);
            }

            var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
            // var diagnostics = binder.Diagnostics.ToImmutableArray();

            if (previous != null)
            {
                diagnostics.AddRange(previous.Diagnostics);
            }

            diagnostics.AddRange(syntaxTrees.SelectMany(tree => tree.Diagnostics));

            var rootTypes = root.Types.ToImmutableArray();

            var methodDefinitions = ImmutableDictionary.CreateBuilder<Symbol, BoundBlockExpression>();

            // Map and lower all function definitions
            foreach (var boundType in rootTypes)
            {
                var typeScope = new BoundScope(parentScope, boundType);

                foreach (var methodSymbol in boundType.Methods)
                {
                    var functionScope = new BoundScope(typeScope, methodSymbol);

                    if (binder._constructorBodies.TryGetValue(methodSymbol, out var block))
                    {
                        methodDefinitions.Add(methodSymbol, block);
                    }
                    else if (binder._functionDeclarations.TryGetValue(methodSymbol, out var syntax))
                    {
                        if (syntax.Body != null)
                        {
                            // TODO: we should require some kind of attribute when there is no body ie "extern" or something
                            var body = binder.BindExpression(syntax.Body.Body, functionScope);

                            var loweredBody = LoweringPipeline.Lower(methodSymbol,
                                new BoundExpressionStatement(body.Syntax, body));

                            if (methodSymbol.ReturnType != Type.Unit && !ControlFlowGraph.AllBlocksReturn(loweredBody))
                            {
                                binder.Diagnostics.ReportAllPathsMustReturn(methodSymbol.Location);
                            }

                            methodDefinitions.Add(methodSymbol, loweredBody);
                        }
                    }
                }
            }

            // define entry point
            if (entryPoint is { Body: { } })
            {
                methodDefinitions.Add(entryPoint.Symbol, entryPoint.Body);
            }

            diagnostics.AddRange(binder.Diagnostics);

            return new BoundAssembly(
                previous,
                diagnostics.ToImmutableArray(),
                entryPoint,
                defaultType,
                rootTypes,
                methodDefinitions.ToImmutable(),
                references
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

        private Symbol BindFunctionDeclaration(FunctionDeclarationSyntax syntax, BoundScope scope)
        {
            var seenParamNames = new HashSet<string>();
            var function = scope.Symbol.NewMethod(syntax.Identifier.Location, syntax.Identifier.Text);

            if (!scope.Symbol.IsClass)
            {
                function.Flags |= SymbolFlags.Static;
            }

            for (var index = 0; index < syntax.Parameters.Count; index++)
            {
                var parameterSyntax = syntax.Parameters[index];
                var parameterName = parameterSyntax.Identifier.Text;
                var parameterType = BindTypeAnnotation(parameterSyntax.TypeAnnotation, scope).Type;

                if (!seenParamNames.Add(parameterName))
                {
                    Diagnostics.ReportParameterAlreadyDeclared(parameterSyntax.Location, parameterName);
                }
                else
                {
                    var parameter = function.NewParameter(parameterSyntax.Identifier.Location, parameterName, index)
                        .WithType(parameterType);

                    function.DefineSymbol(parameter);
                }
            }

            var type = BindOptionalTypeAnnotation(syntax.TypeAnnotation, scope)?.Type;
            if (type == null && syntax.Body != null)
            {
                // HACK: temporarily bind to body so that we can detect the type
                // var tempFunction = new SourceMethodSymbol(syntax.Identifier.Text, parameters.ToImmutable(),
                //     Type.Unit, syntax);
                var functionScope = new BoundScope(scope, new BoundType(scope.Symbol, TextLocation.None, "<temp>"));
                foreach (var parameterSymbol in function.Parameters)
                {
                    functionScope.DefineSymbol(parameterSymbol);
                }

                var expr = BindExpression(syntax.Body.Body, functionScope);
                type = expr.Type;

                if (type == Type.Error)
                    Debugger.Break();
            }

            function.Type = new MethodType(function.Parameters, type ?? Type.Error);


            if (!scope.DefineSymbol(function))
            {
                Diagnostics.ReportAmbiguousMethod(syntax.Location, syntax.Identifier.Text,
                    function.Parameters.Select(p => p.Name).ToImmutableArray());
            }

            return function;
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
                BreakStatementSyntax breakStatementSyntax => BindBreakStatement(breakStatementSyntax),
                ContinueStatementSyntax continueStatementSyntax => BindContinueStatement(continueStatementSyntax),
                ExpressionStatementSyntax expressionStatementSyntax => BindExpressionStatement(
                    expressionStatementSyntax, scope),
                VariableDeclarationStatementSyntax variableDeclarationStatementSyntax =>
                    BindVariableDeclarationStatement(variableDeclarationStatementSyntax, scope),
                _ => throw new ArgumentOutOfRangeException(nameof(syntax))
            };

        private BoundStatement BindContinueStatement(ContinueStatementSyntax syntax)
        {
            var label = GetContinueLabel();
            if (label == null)
            {
                Diagnostics.ReportInvalidBreakOrContinue(syntax.ContinueKeyword.Location, syntax.ContinueKeyword.Text);
                return new BoundExpressionStatement(syntax, new BoundErrorExpression(syntax));
            }

            return new BoundGotoStatement(syntax, label);
        }

        private BoundStatement BindBreakStatement(BreakStatementSyntax syntax)
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
            // TODO: handle cases where we dont have an initializer
            var isReadOnly = syntax.ValOrVarToken.Kind == SyntaxKind.ValKeyword;
            var boundExpression = BindExpression(syntax.Initializer!.Expression, scope);
            var type = BindOptionalTypeAnnotation(syntax.TypeAnnotation, scope)?.Type;
            var expressionType = type ?? boundExpression.Type;

            var converted = BindConversion(syntax.Initializer.Expression.Location, boundExpression, expressionType);
            var variable = BindVariable(syntax.IdentifierToken, expressionType, isReadOnly, scope);

            return new BoundVariableDeclarationStatement(syntax, variable, converted);
        }

        private Symbol BindTypeAnnotation(TypeAnnotationSyntax syntaxTypeClause, BoundScope boundScope)
        {
            var type = BindTypeSymbol(syntaxTypeClause.Type, boundScope);
            if (type == null)
            {
                Diagnostics.ReportUndefinedType(syntaxTypeClause.Type.Location, syntaxTypeClause.Type.ToText());
                return TypeSymbol.Error;
            }

            return type;
        }

        private Symbol? BindOptionalTypeAnnotation(TypeAnnotationSyntax? syntaxTypeClause, BoundScope scope) =>
            syntaxTypeClause == null ? null : BindTypeAnnotation(syntaxTypeClause, scope);

        private Symbol BindVariable(SyntaxToken identifier, Type expressionType, bool isReadOnly, BoundScope scope)
        {
            var name = identifier.Text ?? "??";
            var declare = !identifier.IsInsertedToken;

            Symbol variable;
            if (scope.IsGlobalScope)
            {
                variable = scope.Symbol.NewField(identifier.Location, name, isReadOnly).WithType(expressionType);
                if (scope.Symbol.IsObject)
                {
                    variable.Flags |= SymbolFlags.Static;
                }
            }
            else
            {
                variable = scope.Symbol.NewLocal(identifier.Location, name, isReadOnly).WithType(expressionType);
            }

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
                expression.Type == Type.Error ? new BoundErrorExpression(syntax) : expression);
        }

        private BoundExpression BindExpression(ExpressionSyntax syntax, BoundScope scope) =>
            syntax switch
            {
                AssignmentExpressionSyntax assignmentExpressionSyntax => BindAssignmentExpression(assignmentExpressionSyntax, scope),
                BinaryExpressionSyntax binaryExpressionSyntax => BindBinaryExpression(binaryExpressionSyntax, scope),
                BlockExpressionSyntax blockExpressionSyntax => BindBlockExpression(blockExpressionSyntax, scope),
                CallExpressionSyntax callExpressionSyntax => BindCallExpression(callExpressionSyntax, scope),
                ForExpressionSyntax forExpressionSyntax => BindForExpression(forExpressionSyntax, scope),
                GroupExpressionSyntax groupExpressionSyntax => BindGroupExpression(groupExpressionSyntax, scope),
                IfExpressionSyntax ifExpressionSyntax => BindIfExpression(ifExpressionSyntax, scope),
                LiteralExpressionSyntax literalExpressionSyntax => BindLiteralExpression(literalExpressionSyntax),
                MemberAccessExpressionSyntax memberAccessExpressionSyntax => BindMemberAccessExpression(memberAccessExpressionSyntax, scope),
                NameSyntax nameExpressionSyntax => BindNameExpression(nameExpressionSyntax, scope),
                NewExpressionSyntax newExpressionSyntax => BindNewExpression(newExpressionSyntax, scope),
                UnaryExpressionSyntax unaryExpressionSyntax => BindUnaryExpression(unaryExpressionSyntax, scope),
                UnitExpressionSyntax unit => BindUnitExpression(unit),
                WhileExpressionSyntax whileExpressionSyntax => BindWhileExpression(whileExpressionSyntax, scope),
                _ => throw new Exception($"Unexpected syntax {syntax.Kind}")
            };

        private BoundExpression BindNewExpression(NewExpressionSyntax syntax, BoundScope scope)
        {
            var boundArguments = syntax.Arguments.Select(argument => BindExpression(argument, scope))
                .ToImmutableArray();

            var type = BindTypeSymbol(syntax.Type, scope);
            if (type == null)
                return new BoundErrorExpression(syntax);

            // find constructor from type that matches the arguments:
            var filteredSymbols = type.Constructors.Where(sym =>
                (sym.IsMethod && sym.Parameters.Length == boundArguments.Length)
            ).ToImmutableArray();

            switch (filteredSymbols.Length)
            {
                case 0:
                    Diagnostics.ReportNoOverloads(
                        syntax.Type.Location,
                        syntax.Type.ToText(),
                        boundArguments.Select(arg => arg.Type.Symbol.Name).ToImmutableArray()
                    );
                    return new BoundErrorExpression(syntax);

                case 1:
                    var constructor = filteredSymbols[0];
                    var convertedArguments = BindArguments(syntax.Arguments, constructor, boundArguments);

                    return new BoundNewExpression(syntax, constructor, convertedArguments);

                default:
                    Diagnostics.ReportAmbiguousMethod(syntax.Type.Location, syntax.Type.ToText(),
                        boundArguments.Select(arg => arg.Type.Symbol.Name).ToImmutableArray());
                    return new BoundErrorExpression(syntax);
            }
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

        private BoundExpression BindExpression(ExpressionSyntax syntax, Type type, BoundScope scope,
            bool allowExplicit = false)
        {
            var expression = BindExpression(syntax, scope);
            return BindConversion(syntax.Location, expression, type, allowExplicit);
        }

        private BoundExpression BindConversion(TextLocation location, BoundExpression expression, Type type,
            bool allowExplicit = false)
        {
            var conversion = Conversion.Classify(expression.Type, type);

            if (!conversion.Exists)
            {
                if (expression.Type != Type.Error && type != Type.Error)
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

        private BoundNode BindMemberAccess(MemberAccessExpressionSyntax syntax, BoundScope scope)
        {
            // TODO: combine with BindMemberAccessExpression?
            var expr = BindExpression(syntax.Expression, scope);
            var type = expr.Type;
            var name = syntax.Name.Identifier.Text;
            var members = type.Symbol.LookupMembers(name).ToImmutableArray();

            if (expr.Kind == BoundNodeKind.ErrorExpression)
                return expr;

            // TODO add property support
            switch (members.Length)
            {
                case 0:
                    Diagnostics.ReportMissingDefinition(syntax.Location, type, name);
                    return new BoundErrorExpression(syntax);

                case 1:
                    switch (members[0])
                    {
                        case { IsField: true } field:
                            return new BoundFieldExpression(syntax, name, expr, field);

                        case { IsMethod: true }:
                            return new BoundMethodExpression(syntax, name, expr, members.Where(m => m.IsMethod).ToImmutableArray());
                        default:
                            // TODO: new error message as we have members but they are not of any expected type
                            Diagnostics.ReportMissingDefinition(syntax.Location, type, name);
                            return new BoundErrorExpression(syntax);
                    }

                default:
                {
                    var methods = members.Where(m => m.IsMethod).ToImmutableArray();
                    if (members.Length != methods.Length)
                    {
                        // TODO: new error message as something is wrong here we have non-method members with the same name
                        Diagnostics.ReportMissingDefinition(syntax.Location, type, name);
                        return new BoundErrorExpression(syntax);
                    }

                    return new BoundMethodExpression(syntax, name, expr, methods);
                }
            }
        }

        private Symbol? BindTypeSymbol(NameSyntax syntax, BoundScope scope)
        {
            switch (syntax)
            {
                case IdentifierNameSyntax identifier:
                {
                    var matchingSymbols = scope
                        .LookupSymbol(identifier.ToText())
                        .Where(x => x.IsType)
                        .ToImmutableArray();
                    switch (matchingSymbols.Length)
                    {
                        case 0:
                            Diagnostics.ReportTypeNotFound(identifier.Location, identifier.ToText());
                            return null;
                        case 1:
                            return matchingSymbols[0];
                        default:
                            // we should never have the same type name in the same scope
                            throw new InvalidOperationException();
                    }
                }
                case QualifiedNameSyntax qualified:
                {
                    var names = qualified.ToIdentifierNames().ToList();

                    var rootSymbols = scope
                        .LookupSymbol(names[0].ToText())
                        .Where(x => x.IsType || x.IsNamespace)
                        .ToImmutableArray();

                    names.RemoveAt(0);

                    while (true)
                    {
                        switch (rootSymbols.Length)
                        {
                            case 0:
                                Diagnostics.ReportTypeNotFound(qualified.Location, qualified.ToText());
                                return null;
                            case 1:
                                // search all rights
                                if (names.Count == 0)
                                {
                                    if (rootSymbols[0].IsType)
                                    {
                                        return rootSymbols[0];
                                    }

                                    Diagnostics.ReportTypeNotFound(qualified.Location, qualified.ToText());
                                    return null;
                                }

                                rootSymbols = rootSymbols[0].LookupMembers(names[0].ToText())
                                    .Where(x => x.IsType || x.IsNamespace)
                                    .ToImmutableArray();

                                names.RemoveAt(0);
                                continue;
                            default:
                                // we should never have the same type name in the same scope
                                throw new InvalidOperationException();
                        }
                    }
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(syntax));
            }
        }

        private BoundExpression BindMemberAccessExpression(MemberAccessExpressionSyntax syntax, BoundScope scope)
        {
            // TODO: combine with BindMemberMethodAccess?
            var node = BindMemberAccess(syntax, scope);
            if (node is BoundExpression expr)
            {
                return expr;
            }

            // TODO: fix this Type.Error put the appropriate TypeSymbol here
            Diagnostics.ReportMissingDefinition(syntax.Location, Type.Error, syntax.Name.ToText());
            return new BoundErrorExpression(syntax);
        }

        private BoundExpression BindCallExpression(CallExpressionSyntax syntax, BoundScope scope)
        {
            // TODO: we should be able to refactor this a bit by extracting all of the IdentifierNameSyntax steps
            switch (syntax.Expression)
            {
                case IdentifierNameSyntax identifierNameSyntax:
                {
                    return BindIdentifierCallExpression(syntax, identifierNameSyntax, scope);
                }
                case MemberAccessExpressionSyntax memberAccessExpressionSyntax:
                {
                    var expression = BindMemberAccess(memberAccessExpressionSyntax, scope);
                    if (expression is BoundErrorExpression errorExpression)
                        return errorExpression;

                    if (expression is BoundMethodExpression boundMethodExpression)
                    {
                        var boundArguments = syntax.Arguments.Select(argument => BindExpression(argument, scope))
                            .ToList();
                        var methods = boundMethodExpression.Methods
                            .Where(x => x.Parameters.Length == boundArguments.Count)
                            .ToImmutableArray();

                        if (methods.Length == 0)
                        {
                            Diagnostics.ReportNoOverloads(syntax.Expression.Location, boundMethodExpression.Name,
                                boundArguments.Select(arg => arg.Type.Symbol.Name).ToImmutableArray());

                            return new BoundErrorExpression(syntax);
                        }

                        if (methods.Length > 1)
                        {
                            // TODO: improve type detection and binding when multiple valid methods exist
                            Diagnostics.ReportAmbiguousMethod(syntax.Expression.Location, boundMethodExpression.Name,
                                boundArguments.Select(arg => arg.Type.Symbol.Name).ToImmutableArray());

                            return new BoundErrorExpression(syntax);
                        }

                        return BindCallExpressionToMethodSymbol(syntax, methods[0], boundMethodExpression.Expression, boundArguments);
                    }

                    Diagnostics.ReportUnsupportedFunctionCall(syntax.Expression.Location);
                    return new BoundErrorExpression(syntax);
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(syntax));
            }
        }

        private BoundExpression BindIdentifierCallExpression(CallExpressionSyntax syntax, IdentifierNameSyntax identifierNameSyntax, BoundScope scope)
        {
            // Bind Conversion
            var symbolName = identifierNameSyntax.ToText();
            var boundExpression = TryBindTypeConversion(syntax, symbolName, scope);
            if (boundExpression != null)
                return boundExpression;

            // Bind method
            var boundArguments = syntax.Arguments.Select(argument => BindExpression(argument, scope)).ToList();

            return BindIdentifierCallExpressionFromScope(syntax, identifierNameSyntax, symbolName, boundArguments, scope);
        }

        private BoundExpression BindIdentifierCallExpressionFromScope(CallExpressionSyntax syntax,
            IdentifierNameSyntax identifierNameSyntax, string symbolName,
            List<BoundExpression> boundArguments, BoundScope scope)
        {
            var symbols = scope.LookupSymbol(symbolName, false);

            switch (symbols.Length)
            {
                case 0:
                    if (scope.IsRootScope)
                    {
                        Diagnostics.ReportNoOverloads(
                            identifierNameSyntax.Location,
                            symbolName,
                            boundArguments.Select(arg => arg.Type.Symbol.Name).ToImmutableArray()
                        );

                        return new BoundErrorExpression(syntax);
                    }
                    else
                    {
                        return BindIdentifierCallExpressionFromScope(syntax, identifierNameSyntax, symbolName, boundArguments, scope.Parent);
                    }
                case 1:
                    return BindCallExpressionToSymbol(syntax, symbols[0], null,  boundArguments);

                default:
                    // TODO: see if we have method symbols and try to prune them to the correct one
                    var filteredSymbols = symbols.Where(sym =>
                        (sym.IsMethod && sym.Parameters.Length == boundArguments.Count)
                        ).ToImmutableArray();

                    switch (filteredSymbols.Length)
                    {
                        case 0:
                            Diagnostics.ReportNoOverloads(
                                identifierNameSyntax.Location,
                                symbolName,
                                boundArguments.Select(arg => arg.Type.Symbol.Name).ToImmutableArray()
                            );
                            return new BoundErrorExpression(syntax);

                        case 1:
                            return BindCallExpressionToSymbol(syntax, filteredSymbols[0], null, boundArguments);

                        default:
                            Diagnostics.ReportAmbiguousMethod(identifierNameSyntax.Location, symbolName,
                                boundArguments.Select(arg => arg.Type.Symbol.Name).ToImmutableArray());
                            return new BoundErrorExpression(syntax);
                    }
            }
        }

        private BoundExpression BindCallExpressionToSymbol(CallExpressionSyntax syntax, Symbol symbol,
            BoundExpression? expression, IReadOnlyList<BoundExpression> boundArguments)
        {
            switch (symbol)
            {
                case { IsClass: true }:
                {
                    // bind constructor
                    // see if there is an applicable constructor
                    var ctor = symbol.LookupMethod(".ctor").FirstOrDefault();
                    if (ctor != null && ctor.Parameters.Length == boundArguments.Count)
                    {
                        return BindCallExpressionToMethodSymbol(syntax, ctor, expression, boundArguments);
                    }

                    // TODO: report better error
                    Diagnostics.ReportNoOverloads(syntax.Expression.Location, symbol.Name,
                        boundArguments.Select(arg => arg.Type.Symbol.Name).ToImmutableArray());

                    return new BoundErrorExpression(syntax);
                }
                case { IsMethod: true }:
                    return BindCallExpressionToMethodSymbol(syntax, symbol, expression, boundArguments);

                case { IsValue : true }:
                    Diagnostics.ReportNotAFunction(syntax.Expression.Location, symbol.Name);
                    return new BoundErrorExpression(syntax);

                default:
                    Diagnostics.ReportNoOverloads(syntax.Expression.Location, symbol.Name,
                        boundArguments.Select(arg => arg.Type.Symbol.Name).ToImmutableArray());
                    return new BoundErrorExpression(syntax);
            }
        }

        private BoundExpression BindCallExpressionToMethodSymbol(CallExpressionSyntax syntax, Symbol method, BoundExpression? expression, IReadOnlyList<BoundExpression> boundArguments)
        {
            var convertedArgs = BindArguments(syntax.Arguments, method, boundArguments);

            return new BoundCallExpression(syntax, method, expression, convertedArgs);
        }

        private ImmutableArray<BoundExpression> BindArguments(SeparatedSyntaxList<ExpressionSyntax> arguments, Symbol method, IReadOnlyList<BoundExpression> boundArguments)
        {
            var convertedArgs = ImmutableArray.CreateBuilder<BoundExpression>();
            for (var i = 0; i < arguments.Count; i++)
            {
                var argument = boundArguments[i];
                var parameter = method.Parameters[i];
                var convertedArgument = BindConversion(arguments[i].Location, argument, parameter.Type);
                convertedArgs.Add(convertedArgument);
            }

            return convertedArgs.ToImmutable();
        }

        private BoundExpression? TryBindTypeConversion(CallExpressionSyntax syntax, string symbolName, BoundScope scope)
        {
            if (syntax.Arguments.Count != 1)
                return null;

            var type = LookupBuiltinType(symbolName);
            if (type == null)
                return null;

            return BindExpression(syntax.Arguments[0], type, scope, allowExplicit: true);
        }

        private static Dictionary<string, Type> _builtinLookup = new()
        {
            ["any"] = Type.Any,
            ["int"] = Type.Int,
            ["bool"] = Type.Bool,
            ["string"] = Type.String,
            ["unit"] = Type.Unit,
        };

        private Type? LookupBuiltinType(string text)
        {
            if (!_builtinLookup.TryGetValue(text, out var type))
                return null;

            return type;
        }

        private BoundExpression BindForExpression(ForExpressionSyntax syntax, BoundScope scope)
        {
            var lowerBound = BindExpression(syntax.FromExpression, scope);
            var upperBound = BindExpression(syntax.ToExpression, scope);

            if (lowerBound.Type != Type.Int)
            {
                Diagnostics.ReportTypeMismatch(syntax.FromExpression.Location, Type.Int, lowerBound.Type);
                return new BoundErrorExpression(syntax);
            }

            if (upperBound.Type != Type.Int)
            {
                Diagnostics.ReportTypeMismatch(syntax.ToExpression.Location, Type.Int, upperBound.Type);
                return new BoundErrorExpression(syntax);
            }

            var newScope = new BoundScope(scope);
            var variable = BindVariable(syntax.Variable, Type.Int, true, newScope);

            var body = BindLoopBody(syntax.Body, newScope, out var breakLabel, out var continueLabel);

            return new BoundForExpression(syntax, variable, lowerBound, upperBound, body, breakLabel, continueLabel);
        }

        private BoundExpression BindWhileExpression(WhileExpressionSyntax syntax, BoundScope scope)
        {
            var condition = BindExpression(syntax.ConditionExpression, Type.Bool, scope);
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

            if (condition.Type == Type.Error || then.Type == Type.Error ||
                elseExpr.Type == Type.Error)
                return new BoundErrorExpression(syntax);

            if (then.Type != elseExpr.Type)
            {
                Diagnostics.ReportTypeMismatch(syntax.ElseExpression.Location, then.Type, elseExpr.Type);
                return new BoundErrorExpression(syntax);
            }

            if (condition.Type != Type.Bool)
            {
                Diagnostics.ReportTypeMismatch(syntax.ConditionExpression.Location, Type.Bool, condition.Type);
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

        private static (Symbol root, BoundScope parent) CreateParentScope(BoundAssembly? previous,
            ImmutableArray<AssemblyDefinition> references)
        {
            var stack = new Stack<BoundAssembly>();

            while (previous != null)
            {
                stack.Push(previous);
                previous = previous.Previous;
            }

            var (root, parent) = CreateRootScope(references);

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

            return (root, parent);
        }

        private static (Symbol root, BoundScope rootScope) CreateRootScope(ImmutableArray<AssemblyDefinition> references)
        {
            var root = Symbol.NewRoot();
            var rootScope = new BoundScope(root);

            // define default symbols
            rootScope.Import(root.NewClass(TextLocation.None, "any").WithType(Type.Any));
            rootScope.Import(root.NewClass(TextLocation.None, "int").WithType(Type.Int));
            rootScope.Import(root.NewClass(TextLocation.None, "bool").WithType(Type.Bool));
            rootScope.Import(root.NewClass(TextLocation.None, "string").WithType(Type.String));
            rootScope.Import(root.NewClass(TextLocation.None, "unit").WithType(Type.Unit));

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

            return (root, rootScope);
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
                    return new BoundTypeExpression(syntax, type.Type);

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
            if (boundOperand.Type == Type.Error)
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

            if (left.Type == Type.Error || right.Type == Type.Error)
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