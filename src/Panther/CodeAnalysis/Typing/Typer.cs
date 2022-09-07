using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Mono.Cecil;
using Panther.CodeAnalysis.Lowering;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;
using Panther.CodeAnalysis.Text;
using ArrayType = Panther.CodeAnalysis.Symbols.ArrayType;
using Type = Panther.CodeAnalysis.Symbols.Type;

namespace Panther.CodeAnalysis.Typing;

internal sealed class Typer
{
    private readonly bool _isScript;
    public DiagnosticBag Diagnostics { get; } = new DiagnosticBag();
    private int _labelCounter = 0;

    private readonly Stack<(TypedLabel BreakLabel, TypedLabel ContinueLabel)> _breakContinueLabels =
        new();
    private readonly Dictionary<Symbol, FunctionDeclarationSyntax> _functionDeclarations = new();
    private readonly Dictionary<Symbol, TypedBlockExpression> _constructorBodies = new();

    public Typer(bool isScript)
    {
        _isScript = isScript;
    }

    private void BindClassDeclaration(ClassDeclarationSyntax syntax, TypedScope parent)
    {
        var typeSymbol = parent.Symbol.NewClass(syntax.Identifier.Location, syntax.Identifier.Text);

        if (!parent.DefineSymbol(typeSymbol))
        {
            Diagnostics.ReportAmbiguousType(syntax.Location, syntax.Identifier.Text);
        }

        var parameters = ImmutableArray.CreateBuilder<Symbol>();
        var assignments = ImmutableArray.CreateBuilder<TypedStatement>();
        var seenFieldNames = new HashSet<string>();

        var ctor = typeSymbol.NewMethod(typeSymbol.Location, ".ctor");

        for (var index = 0; index < syntax.Fields.Count; index++)
        {
            var field = syntax.Fields[index];
            var fieldName = field.Identifier.Text;
            var fieldType = BindTypeAnnotation(field.TypeAnnotation, parent).Type;
            var fieldSymbol = typeSymbol
                .NewField(field.Identifier.Location, field.Identifier.Text, false)
                .WithType(fieldType);

            typeSymbol.DefineSymbol(fieldSymbol);

            if (!seenFieldNames.Add(fieldName))
            {
                Diagnostics.ReportDuplicateParameter(field.Location, fieldName);
            }
            else
            {
                var parameter = ctor.NewParameter(field.Identifier.Location, fieldName, index)
                    .WithType(fieldType);
                ctor.DefineSymbol(parameter);
                parameters.Add(parameter);
                assignments.Add(
                    new TypedAssignmentStatement(
                        field,
                        new TypedFieldExpression(field, null, fieldSymbol),
                        new TypedVariableExpression(field, parameter)
                    )
                );
            }
        }

        var typeScope = new TypedScope(parent, typeSymbol);
        var immParams = parameters.ToImmutableArray();
        ctor.WithType(new MethodType(immParams, Type.Unit));

        typeSymbol.DefineSymbol(ctor);
        _constructorBodies.Add(
            ctor,
            new TypedBlockExpression(
                syntax,
                assignments.ToImmutableArray(),
                new TypedUnitExpression(syntax)
            )
        );

        if (syntax.Template != null)
        {
            BindMembers(syntax.Template.Members, syntax, typeScope);
        }
    }

    private void BindObjectDeclaration(ObjectDeclarationSyntax objectDeclaration, TypedScope parent)
    {
        var typeSymbol = parent.Symbol.NewObject(
            objectDeclaration.Identifier.Location,
            objectDeclaration.Identifier.Text
        );
        typeSymbol.Type = new ClassType(typeSymbol);

        if (!parent.DefineSymbol(typeSymbol))
        {
            Diagnostics.ReportAmbiguousType(
                objectDeclaration.Location,
                objectDeclaration.Identifier.Text
            );
        }

        var scope = new TypedScope(parent, typeSymbol);

        BindMembers(objectDeclaration.Template.Members, objectDeclaration, scope);
    }

    private void BindMembers(
        ImmutableArray<MemberSyntax> members,
        SyntaxNode parent,
        TypedScope scope
    )
    {
        var (objectsAndClasses, rest) = members.Partition(
            member => member is ObjectDeclarationSyntax or ClassDeclarationSyntax
        );
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

        var boundStatements = ImmutableArray.CreateBuilder<TypedStatement>();

        // check statements for field declarations
        foreach (var memberSyntax in statements)
        {
            switch (memberSyntax)
            {
                case GlobalStatementSyntax(_, var statement):
                    // #error this should not be binding prior to defining fields?
                    boundStatements.Add(BindStatement(statement, scope, true));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(memberSyntax));
            }
        }

        var typeSymbol = scope.Symbol;

        if (!boundStatements.Any())
            return;

        var isObject = typeSymbol.IsObject;
        var ctorName = isObject ? ".cctor" : ".ctor";

        var existingCtor = typeSymbol.Constructors.FirstOrDefault();
        var ctorSymbol = existingCtor ?? typeSymbol.NewMethod(typeSymbol.Location, ctorName);

        if (isObject)
        {
            ctorSymbol.Flags |= SymbolFlags.Static;
        }

        if (existingCtor == null)
        {
            ctorSymbol.Type = new MethodType(ImmutableArray<Symbol>.Empty, Type.Unit);
            typeSymbol.DefineSymbol(ctorSymbol);
        }

        if (_constructorBodies.TryGetValue(ctorSymbol, out var existingBody))
        {
            boundStatements.Add(new TypedExpressionStatement(parent, existingBody));
        }

        var loweredBody = LoweringPipeline.Lower(
            ctorSymbol,
            new TypedExpressionStatement(
                parent,
                new TypedBlockExpression(
                    parent,
                    boundStatements.ToImmutable(),
                    new TypedUnitExpression(parent)
                )
            )
        );

        _constructorBodies[ctorSymbol] = loweredBody;
    }

    // private Symbol BindNamespace(NamespaceDirectiveSyntax namespaceDirective)
    // {
    //     throw new NotImplementedException();
    // }

    private static EntryPoint? BindEntryPoint(
        TypedScope boundScope,
        bool isScript,
        ImmutableArray<SyntaxTree> syntaxTrees,
        ImmutableArray<TypedStatement> globalStatements,
        Symbol? mainFunction,
        Typer typer
    ) =>
        isScript
            ? BindScriptEntryPoint(boundScope, globalStatements)
            : BindMainEntryPoint(boundScope, syntaxTrees, globalStatements, mainFunction, typer);

    private static EntryPoint? BindMainEntryPoint(
        TypedScope boundScope,
        ImmutableArray<SyntaxTree> syntaxTrees,
        ImmutableArray<TypedStatement> globalStatements,
        Symbol? mainFunction,
        Typer typer
    )
    {
        var firstStatementPerSyntaxTree = (
            from tree in syntaxTrees
            let firstStatement = tree.Root.Members.OfType<GlobalStatementSyntax>().FirstOrDefault()
            where firstStatement != null
            select firstStatement
        ).ToImmutableArray();

        // Global statements can only exist in one syntax tree
        if (firstStatementPerSyntaxTree.Length > 1)
        {
            foreach (var firstStatement2 in firstStatementPerSyntaxTree)
            {
                typer.Diagnostics.ReportGlobalStatementsCanOnlyExistInOneFile(
                    firstStatement2!.Location
                );
            }

            return null;
        }

        var hasGlobalStatements = globalStatements.Any();

        // if a main function exists, global statements cannot
        if (mainFunction != null && hasGlobalStatements)
        {
            typer.Diagnostics.ReportCannotMixMainAndGlobalStatements(mainFunction.Location);

            foreach (var firstStatement1 in firstStatementPerSyntaxTree)
            {
                typer.Diagnostics.ReportCannotMixMainAndGlobalStatements(firstStatement1!.Location);
            }

            return null;
        }

        // ensure main function signature is correct
        if (
            mainFunction != null
            && (
                mainFunction.Parameters.Any()
                || (mainFunction.ReturnType != Type.Unit && mainFunction.ReturnType != Type.Int)
            )
        )
        {
            typer.Diagnostics.ReportMainMustHaveCorrectSignature(mainFunction.Location);

            return null;
        }

        if (!hasGlobalStatements)
        {
            if (mainFunction != null)
                return new EntryPoint(false, mainFunction, null);

            return null;
        }

        var main =
            mainFunction
            ?? boundScope.Symbol
                .NewMethod(TextLocation.None, "main")
                .WithType(new MethodType(ImmutableArray<Symbol>.Empty, Type.Unit))
                .WithFlags(SymbolFlags.Static);

        var compilationUnit = globalStatements.First().Syntax;
        var body = LoweringPipeline.Lower(
            main,
            TypedStatementFromStatements(compilationUnit, globalStatements)
        );

        boundScope.DefineSymbol(main);

        return new EntryPoint(false, main, body);
    }

    private static EntryPoint? BindScriptEntryPoint(
        TypedScope boundScope,
        ImmutableArray<TypedStatement> globalStatements
    )
    {
        if (!globalStatements.Any())
            return null;

        var eval = boundScope.Symbol
            .NewMethod(TextLocation.None, "$eval")
            .WithType(Type.Any)
            .WithFlags(SymbolFlags.Static);

        var compilationUnit = globalStatements.First().Syntax;
        var boundStatementFromStatements = TypedStatementFromStatements(
            compilationUnit,
            globalStatements
        );

        // for our script function we need to return an object. if the expression is not an object then we will
        // create a conversion expression to convert it.
        if (boundStatementFromStatements.Expression.Type != Type.Any)
        {
            // what should we do when we have a unit expression?
            boundStatementFromStatements = new TypedExpressionStatement(
                boundStatementFromStatements.Syntax,
                new TypedConversionExpression(
                    boundStatementFromStatements.Syntax,
                    Type.Any,
                    boundStatementFromStatements.Expression
                )
            );
        }

        boundScope.DefineSymbol(eval);

        return new EntryPoint(
            true,
            eval,
            LoweringPipeline.Lower(eval, boundStatementFromStatements)
        );
    }

    private static bool IsTopLevelDeclaration(SyntaxNode member) =>
        member.Kind is SyntaxKind.ClassDeclaration or SyntaxKind.ObjectDeclaration;

    public static TypedAssembly BindAssembly(
        bool isScript,
        ImmutableArray<SyntaxTree> syntaxTrees,
        TypedAssembly? previous,
        ImmutableArray<AssemblyDefinition> references
    )
    {
        var (root, parentScope) = CreateParentScope(previous, references);
        var scope = new TypedScope(parentScope, root);
        var binder = new Typer(isScript);

        var defaultTypeAddedToNamespace = false;
        TypedType? defaultType = null;
        TypedScope? defaultTypeScope = null;

        var globalStatements = ImmutableArray.CreateBuilder<GlobalStatementSyntax>();
        var fileScopes = new Dictionary<SourceFile, TypedScope>();

        foreach (var tree in syntaxTrees)
        {
            var compilationUnit = tree.Root;
            var fileScope = new TypedScope(scope, $"FileScope[{tree.File.FileName}]");
            fileScopes[tree.File] = fileScope;

            var thisScope = fileScope;
            var allTopLevel = compilationUnit.Members.All(IsTopLevelDeclaration);
            var allGlobalStatements = compilationUnit.Members.All(
                member => member is GlobalStatementSyntax
            );

            if (!allTopLevel || !allGlobalStatements)
            {
                // TODO: global statements are not supported with a namespace
            }

            if (compilationUnit.Namespace != null)
            {
                var namespaceSymbol = binder.BindNamespace(compilationUnit.Namespace, root);
                thisScope = new TypedScope(thisScope, namespaceSymbol, "declarations");
            }

            foreach (var @using in compilationUnit.Usings)
            {
                // TODO: doing things this way assumes only allows us to import from external libraries
                // this should be fine for now
                var namespaceOrTypeSymbol = binder.BindUsing(@using, root);
                if (namespaceOrTypeSymbol != Symbol.None)
                    fileScope.ImportMembers(namespaceOrTypeSymbol);
            }

            var (objectsAndClasses, rest) = compilationUnit.Members.Partition(
                member => member is ObjectDeclarationSyntax or ClassDeclarationSyntax
            );
            var (functions, rest2) = rest.Partition(member => member is FunctionDeclarationSyntax);
            var theseGlobalStatements = rest2.OfType<GlobalStatementSyntax>().ToImmutableArray();

            // should only be one set of global statements
            // otherwise there would be an error
            globalStatements.AddRange(theseGlobalStatements);

            // bind classes and objects first
            binder.BindMembers(objectsAndClasses, compilationUnit, thisScope);

            if (functions.IsEmpty && theseGlobalStatements.IsEmpty)
                continue;

            defaultType = new TypedType(root, TextLocation.None, "$Program")
            {
                Flags = SymbolFlags.Object
            };
            defaultTypeScope = new TypedScope(fileScope, defaultType);

            // functions go into our default type scope
            // everything else can go in our global scope
            binder.BindMembers(functions, compilationUnit, defaultTypeScope);
        }

        var statements = globalStatements
            .Select(
                globalStatementSyntax =>
                    binder.BindGlobalStatement(globalStatementSyntax.Statement, defaultTypeScope!)
            )
            .ToImmutableArray();

        if (defaultType != null)
        {
            root.DefineSymbol(defaultType);
            defaultTypeAddedToNamespace = true;
        }

        var mains =
            from type in root.Types
            from member in type.Methods
            where member.Name == "main"
            select member;

        var mainFunction = mains.FirstOrDefault();

        var entryPoint = BindEntryPoint(
            defaultTypeScope
                ?? (
                    mainFunction == null
                        ? new TypedScope(scope)
                        : new TypedScope(scope, mainFunction.Owner)
                ),
            isScript,
            syntaxTrees,
            statements,
            mainFunction,
            binder
        );

        if (!defaultTypeAddedToNamespace && defaultType != null)
        {
            root.DefineSymbol(defaultType);
        }

        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        if (previous != null)
        {
            diagnostics.AddRange(previous.Diagnostics);
        }

        diagnostics.AddRange(syntaxTrees.SelectMany(tree => tree.Diagnostics));

        var methodDefinitions = ImmutableDictionary.CreateBuilder<Symbol, TypedBlockExpression>();

        // Map and lower all function definitions
        foreach (var boundType in root.Types)
        {
            // if this type is the "main" $Program type then we use the `defaultTypeScope` here otherwise
            // lookup the file scope(file that this type is declared in) as it has all the imports associated with
            // the file registered already. attach this type's scope to the file scope

            var fileScope =
                boundType.Name == "$Program"
                    ? defaultTypeScope
                    : fileScopes[boundType.Location.File];
            Debug.Assert(fileScope != null);
            var typeScope = new TypedScope(fileScope!, boundType);

            foreach (var methodSymbol in boundType.Methods)
            {
                var functionScope = new TypedScope(typeScope, methodSymbol);

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

                        var loweredBody = LoweringPipeline.Lower(
                            methodSymbol,
                            new TypedExpressionStatement(body.Syntax, body)
                        );

                        if (
                            methodSymbol.ReturnType != Type.Unit
                            && !ControlFlowGraph.AllBlocksReturn(loweredBody)
                        )
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

        return new TypedAssembly(
            Previous: previous,
            Diagnostics: diagnostics.ToImmutableArray(),
            EntryPoint: entryPoint,
            DefaultType: defaultType,
            RootSymbol: root,
            MethodDefinitions: methodDefinitions.ToImmutable(),
            References: references
        );
    }

    private Symbol BindNamespace(
        NamespaceDeclarationSyntax compilationUnitNamespace,
        Symbol symbol
    ) => BindNamespace(compilationUnitNamespace.Name, symbol, true);

    private Symbol BindNamespace(NameSyntax name, Symbol symbol, bool create)
    {
        if (name is QualifiedNameSyntax(_, var left, _, var right))
        {
            var nestedSymbol = BindNamespace(left, symbol, create);

            return BindNamespace(right, nestedSymbol, create);
        }

        return BindNamespace((IdentifierNameSyntax)name, symbol, create);
    }

    private static Symbol BindNamespace(
        IdentifierNameSyntax identifierNameSyntax,
        Symbol symbol,
        bool create
    )
    {
        var identifier = identifierNameSyntax.Identifier;
        var textName = identifier.Text;
        var existing = symbol.LookupNamespace(textName);

        if (existing != null)
        {
            return existing;
        }

        if (!create)
            return Symbol.None;

        return symbol.NewNamespace(identifier.Location, textName).Declare();
    }

    private Symbol BindUsing(UsingDirectiveSyntax usingDirective, Symbol symbol) =>
        BindNamespace(usingDirective.Name, symbol, false);

    private static TypedExpressionStatement TypedStatementFromStatements(
        SyntaxNode syntax,
        IReadOnlyCollection<TypedStatement> statements
    )
    {
        var expr = (statements.LastOrDefault() as TypedExpressionStatement)?.Expression;
        var stmts =
            expr == null
                ? statements.ToImmutableArray()
                : statements.Take(statements.Count - 1).ToImmutableArray();

        // this doesnt really feel like the correct syntax as we should have something that encompasses all of the statements
        return new TypedExpressionStatement(
            syntax,
            new TypedBlockExpression(syntax, stmts, expr ?? new TypedUnitExpression(syntax))
        );
    }

    private Symbol BindFunctionDeclaration(FunctionDeclarationSyntax syntax, TypedScope scope)
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
                Diagnostics.ReportDuplicateParameter(parameterSyntax.Location, parameterName);
            }
            else
            {
                var parameter = function
                    .NewParameter(parameterSyntax.Identifier.Location, parameterName, index)
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
            var functionScope = new TypedScope(
                scope,
                new TypedType(scope.Symbol, TextLocation.None, "<temp>")
            );
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
            Diagnostics.ReportAmbiguousMethod(
                syntax.Location,
                syntax.Identifier.Text,
                function.Parameters.Select(p => p.Name).ToImmutableArray()
            );
        }

        return function;
    }

    private TypedStatement BindGlobalStatement(StatementSyntax syntax, TypedScope scope) =>
        BindStatement(syntax, scope, true);

    private TypedStatement BindStatement(
        StatementSyntax syntax,
        TypedScope scope,
        bool isGlobal = false
    )
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

    private static bool IsSideEffectStatement(TypedStatement statement)
    {
        if (statement is TypedExpressionStatement es)
            return IsSideEffectExpression(es.Expression);

        return true;
    }

    private static bool IsSideEffectExpression(TypedExpression expression)
    {
        var exprKind = expression.Kind;

        if (expression is TypedWhileExpression boundWhileExpression)
        {
            return IsSideEffectExpression(boundWhileExpression.Body);
        }

        if (expression is TypedForExpression boundForExpression)
        {
            return IsSideEffectExpression(boundForExpression.Body);
        }

        if (expression is TypedIfExpression boundIfExpression)
        {
            return IsSideEffectExpression(boundIfExpression.Then)
                || IsSideEffectExpression(boundIfExpression.Else);
        }

        if (expression is TypedBlockExpression blockExpression)
        {
            return blockExpression.Statements.Any<TypedStatement>(IsSideEffectStatement)
                || IsSideEffectExpression(blockExpression.Expression);
        }

        return exprKind == TypedNodeKind.ErrorExpression
            || exprKind == TypedNodeKind.AssignmentExpression
            || exprKind == TypedNodeKind.CallExpression;
    }

    private TypedStatement BindStatementInternal(StatementSyntax syntax, TypedScope scope) =>
        syntax switch
        {
            BreakStatementSyntax breakStatementSyntax => BindBreakStatement(breakStatementSyntax),
            ContinueStatementSyntax continueStatementSyntax
                => BindContinueStatement(continueStatementSyntax),
            ExpressionStatementSyntax expressionStatementSyntax
                => BindExpressionStatement(expressionStatementSyntax, scope),
            VariableDeclarationStatementSyntax variableDeclarationStatementSyntax
                => BindVariableDeclarationStatement(variableDeclarationStatementSyntax, scope),
            _ => throw new ArgumentOutOfRangeException(nameof(syntax))
        };

    private TypedStatement BindContinueStatement(ContinueStatementSyntax syntax)
    {
        var label = GetContinueLabel();
        if (label == null)
        {
            Diagnostics.ReportInvalidBreakOrContinue(
                syntax.ContinueKeyword.Location,
                syntax.ContinueKeyword.Text
            );
            return new TypedExpressionStatement(syntax, new TypedErrorExpression(syntax));
        }

        return new TypedGotoStatement(syntax, label);
    }

    private TypedStatement BindBreakStatement(BreakStatementSyntax syntax)
    {
        var label = GetBreakLabel();
        if (label == null)
        {
            Diagnostics.ReportInvalidBreakOrContinue(
                syntax.BreakKeyword.Location,
                syntax.BreakKeyword.Text
            );
            return new TypedExpressionStatement(syntax, new TypedErrorExpression(syntax));
        }

        return new TypedGotoStatement(syntax, label);
    }

    public TypedLabel? GetBreakLabel() =>
        _breakContinueLabels.Count == 0 ? null : _breakContinueLabels.Peek().BreakLabel;

    public TypedLabel? GetContinueLabel() =>
        _breakContinueLabels.Count == 0 ? null : _breakContinueLabels.Peek().ContinueLabel;

    private TypedStatement BindVariableDeclarationStatement(
        VariableDeclarationStatementSyntax syntax,
        TypedScope scope
    )
    {
        // TODO: handle cases where we dont have an initializer
        var isReadOnly = syntax.ValOrVarToken.Kind == SyntaxKind.ValKeyword;
        var boundExpression = BindExpression(syntax.Initializer!.Expression, scope);
        var type = BindOptionalTypeAnnotation(syntax.TypeAnnotation, scope)?.Type;
        var expressionType = type ?? boundExpression.Type;

        var converted = BindConversion(
            syntax.Initializer.Expression.Location,
            boundExpression,
            expressionType
        );
        var variable = BindVariable(syntax.IdentifierToken, expressionType, isReadOnly, scope);

        return new TypedVariableDeclarationStatement(syntax, variable, converted);
    }

    private Symbol BindTypeAnnotation(TypeAnnotationSyntax syntaxTypeClause, TypedScope boundScope)
    {
        var type = BindTypeSymbol(syntaxTypeClause.Type, boundScope);
        if (type == null)
        {
            Diagnostics.ReportUndefinedType(
                syntaxTypeClause.Type.Location,
                syntaxTypeClause.Type.ToText()
            );
            return TypeSymbol.Error;
        }

        return type;
    }

    private Symbol? BindOptionalTypeAnnotation(
        TypeAnnotationSyntax? syntaxTypeClause,
        TypedScope scope
    ) => syntaxTypeClause == null ? null : BindTypeAnnotation(syntaxTypeClause, scope);

    private Symbol BindVariable(
        SyntaxToken identifier,
        Type expressionType,
        bool isReadOnly,
        TypedScope scope
    )
    {
        var name = identifier.Text ?? "??";
        var declare = !identifier.IsInsertedToken;

        Symbol variable;
        if (scope.Symbol.IsType)
        {
            variable = scope.Symbol
                .NewField(identifier.Location, name, isReadOnly)
                .WithType(expressionType);
            if (scope.Symbol.IsObject)
            {
                variable.Flags |= SymbolFlags.Static;
            }
        }
        else
        {
            variable = scope.Symbol
                .NewLocal(identifier.Location, name, isReadOnly)
                .WithType(expressionType);
        }

        if (declare && !scope.DefineSymbol(variable))
        {
            Diagnostics.ReportVariableAlreadyDefined(identifier.Location, name);
        }

        return variable;
    }

    private TypedStatement BindExpressionStatement(
        ExpressionStatementSyntax syntax,
        TypedScope scope
    )
    {
        var expression = BindExpression(syntax.Expression, scope);

        return new TypedExpressionStatement(
            syntax,
            expression.Type == Type.Error ? new TypedErrorExpression(syntax) : expression
        );
    }

    private TypedExpression BindExpression(ExpressionSyntax syntax, TypedScope scope) =>
        syntax switch
        {
            ArrayCreationExpressionSyntax arrayCreationExpressionSyntax
                => BindArrayCreationExpression(arrayCreationExpressionSyntax, scope),
            AssignmentExpressionSyntax assignmentExpressionSyntax
                => BindAssignmentExpression(assignmentExpressionSyntax, scope),
            BinaryExpressionSyntax binaryExpressionSyntax
                => BindBinaryExpression(binaryExpressionSyntax, scope),
            BlockExpressionSyntax blockExpressionSyntax
                => BindBlockExpression(blockExpressionSyntax, scope),
            CallExpressionSyntax callExpressionSyntax
                => BindCallExpression(callExpressionSyntax, scope),
            ForExpressionSyntax forExpressionSyntax
                => BindForExpression(forExpressionSyntax, scope),
            GroupExpressionSyntax groupExpressionSyntax
                => BindGroupExpression(groupExpressionSyntax, scope),
            IfExpressionSyntax ifExpressionSyntax => BindIfExpression(ifExpressionSyntax, scope),
            IndexExpressionSyntax indexExpressionSyntax
                => BindIndexExpression(indexExpressionSyntax, scope),
            LiteralExpressionSyntax literalExpressionSyntax
                => BindLiteralExpression(literalExpressionSyntax),
            MemberAccessExpressionSyntax memberAccessExpressionSyntax
                => BindMemberAccessExpression(memberAccessExpressionSyntax, scope),
            NameSyntax nameExpressionSyntax => BindNameExpression(nameExpressionSyntax, scope),
            NewExpressionSyntax newExpressionSyntax
                => BindNewExpression(newExpressionSyntax, scope),
            UnaryExpressionSyntax unaryExpressionSyntax
                => BindUnaryExpression(unaryExpressionSyntax, scope),
            UnitExpressionSyntax unit => BindUnitExpression(unit),
            WhileExpressionSyntax whileExpressionSyntax
                => BindWhileExpression(whileExpressionSyntax, scope),
            _ => throw new Exception($"Unexpected syntax {syntax.Kind}")
        };

    private TypedExpression BindArrayCreationExpression(
        ArrayCreationExpressionSyntax syntax,
        TypedScope scope
    )
    {
        var typeSymbol = BindTypeSymbol(syntax.Type, scope);
        if (typeSymbol == null)
        {
            Diagnostics.ReportUndefinedType(syntax.Type.Location, syntax.Type.ToText());
            return new TypedErrorExpression(syntax);
        }

        if (syntax.Initializer == null)
        {
            if (syntax.ArrayRank == null)
            {
                Diagnostics.ReportArrayCreationRequiresRankOrInitializer(syntax.Location);
                return new TypedErrorExpression(syntax);
            }

            if (syntax.ArrayRank is not { Value: int rank })
            {
                Diagnostics.ReportArrayRankMustBeAIntLiteral(syntax.Location);
                return new TypedErrorExpression(syntax);
            }

            return new TypedArrayCreationExpression(
                syntax,
                typeSymbol,
                rank,
                ImmutableArray<TypedExpression>.Empty
            );
        }

        var convertedArgs = syntax.Initializer.Arguments
            .Select(
                arg => BindConversion(arg.Location, BindExpression(arg, scope), typeSymbol.Type)
            )
            .ToImmutableArray();

        if (syntax.ArrayRank is { Value: int arrayRank } && arrayRank != convertedArgs.Length)
        {
            Diagnostics.ReportArrayRankMustMatchInitializerLength(syntax.Location);
            return new TypedErrorExpression(syntax);
        }

        return new TypedArrayCreationExpression(
            syntax,
            typeSymbol,
            convertedArgs.Length,
            convertedArgs
        );
    }

    private TypedExpression BindNewExpression(NewExpressionSyntax syntax, TypedScope scope)
    {
        var boundArguments = syntax.Arguments
            .Select(argument => BindExpression(argument, scope))
            .ToImmutableArray();

        var type = BindTypeSymbol(syntax.Type, scope);
        if (type == null)
            return new TypedErrorExpression(syntax);

        // find constructor from type that matches the arguments:
        var filteredSymbols = type.Constructors
            .Where(sym => (sym.IsMethod && sym.Parameters.Length == boundArguments.Length))
            .ToImmutableArray();

        switch (filteredSymbols.Length)
        {
            case 0:
                Diagnostics.ReportNoOverloads(
                    syntax.Type.Location,
                    syntax.Type.ToText(),
                    boundArguments.Select(arg => arg.Type.Symbol.Name).ToImmutableArray()
                );
                return new TypedErrorExpression(syntax);

            case 1:
                var constructor = filteredSymbols[0];
                var convertedArguments = BindArgumentsWithConversions(
                    syntax.Arguments,
                    constructor,
                    boundArguments
                );

                return new TypedNewExpression(syntax, constructor, convertedArguments);

            default:
                Diagnostics.ReportAmbiguousMethod(
                    syntax.Type.Location,
                    syntax.Type.ToText(),
                    boundArguments.Select(arg => arg.Type.Symbol.Name).ToImmutableArray()
                );
                return new TypedErrorExpression(syntax);
        }
    }

    private TypedExpression BindAssignmentExpression(
        AssignmentExpressionSyntax syntax,
        TypedScope scope
    )
    {
        // TODO support member access
        var boundLHS = BindExpression(syntax.Name, scope);
        switch (boundLHS)
        {
            case TypedErrorExpression:
                return boundLHS;

            case TypedVariableExpression variableExpression:
                return TypedAssignmentExpression(
                    syntax,
                    scope,
                    variableExpression.Variable,
                    boundLHS
                );

            case TypedFieldExpression fieldExpression:
                return TypedAssignmentExpression(syntax, scope, fieldExpression.Field, boundLHS);

            case TypedIndexExpression:
                return BindAssignmentExpression(syntax, boundLHS, scope);

            default:
                Diagnostics.ReportNotAssignable(syntax.Name.Location);
                return new TypedErrorExpression(syntax);
        }
    }

    private TypedExpression TypedAssignmentExpression(
        AssignmentExpressionSyntax syntax,
        TypedScope scope,
        Symbol symbol,
        TypedExpression boundLHS
    )
    {
        if (symbol.IsReadOnly)
        {
            Diagnostics.ReportReassignmentToVal(syntax.Name.Location, symbol.Name);

            return new TypedErrorExpression(syntax);
        }

        return BindAssignmentExpression(syntax, boundLHS, scope);
    }

    private TypedExpression BindAssignmentExpression(
        AssignmentExpressionSyntax syntax,
        TypedExpression boundLHS,
        TypedScope scope
    )
    {
        var boundRHS = BindExpression(syntax.Expression, boundLHS.Type, scope);

        return new TypedAssignmentExpression(syntax, boundLHS, boundRHS);
    }

    private TypedExpression BindExpression(
        ExpressionSyntax syntax,
        Type type,
        TypedScope scope,
        bool allowExplicit = false
    )
    {
        var expression = BindExpression(syntax, scope);
        return BindConversion(syntax.Location, expression, type, allowExplicit);
    }

    private TypedExpression BindConversion(
        TextLocation location,
        TypedExpression expression,
        Type type,
        bool allowExplicit = false
    )
    {
        var from = TypeResolver.Resolve(expression.Type);
        var to = TypeResolver.Resolve(type);
        var conversion = Conversion.Classify(from, to);

        if (!conversion.Exists)
        {
            if (from != Type.Error && to != Type.Error)
            {
                Diagnostics.ReportCannotConvert(location, from, to);
            }

            return new TypedErrorExpression(expression.Syntax);
        }

        if (!allowExplicit && conversion.IsExplicit)
        {
            Diagnostics.ReportCannotConvertImplicitly(location, from, to);
        }

        if (conversion.IsIdentity)
            return expression;

        return new TypedConversionExpression(expression.Syntax, to, expression);
    }

    private TypedNode BindMemberAccess(MemberAccessExpressionSyntax syntax, TypedScope scope)
    {
        // TODO: combine with BindMemberAccessExpression?
        var expr = BindExpression(syntax.Expression, scope);
        var type = expr.Type;
        var name = syntax.Name.Identifier.Text;
        var members = type.Symbol.LookupMembers(name).ToImmutableArray();

        if (expr.Kind == TypedNodeKind.ErrorExpression)
            return expr;

        // TODO add property support
        switch (members.Length)
        {
            case 0:
                Diagnostics.ReportMissingDefinition(syntax.Location, type, name);
                return new TypedErrorExpression(syntax);

            case 1:
                switch (members[0])
                {
                    case { IsField: true } field:
                        return new TypedFieldExpression(syntax, expr, field);

                    case { IsProperty: true } prop:
                        return new TypedPropertyExpression(syntax, expr, prop);

                    case { IsMethod: true }:
                        return new TypedMethodExpression(
                            syntax,
                            name,
                            expr,
                            members.Where(m => m.IsMethod).ToImmutableArray()
                        );

                    case { IsMember: true }:
                        return new TypedTypeExpression(syntax, members[0].Type);

                    default:
                        // TODO: new error message as we have members but they are not of any expected type
                        Diagnostics.ReportMissingDefinition(syntax.Location, type, name);
                        return new TypedErrorExpression(syntax);
                }

            default:
            {
                var methods = members.Where(m => m.IsMethod).ToImmutableArray();
                if (members.Length != methods.Length)
                {
                    // TODO: new error message as something is wrong here we have non-method members with the same name
                    Diagnostics.ReportMissingDefinition(syntax.Location, type, name);
                    return new TypedErrorExpression(syntax);
                }

                return new TypedMethodExpression(syntax, name, expr, methods);
            }
        }
    }

    private Symbol? BindTypeSymbol(NameSyntax syntax, TypedScope scope)
    {
        switch (syntax)
        {
            case IdentifierNameSyntax identifier:
            {
                var type = scope.LookupType(identifier.ToText());
                if (type == null)
                {
                    Diagnostics.ReportTypeNotFound(identifier.Location, identifier.ToText());
                    return null;
                }

                return type;
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

                                Diagnostics.ReportTypeNotFound(
                                    qualified.Location,
                                    qualified.ToText()
                                );
                                return null;
                            }

                            rootSymbols = rootSymbols[0]
                                .LookupMembers(names[0].ToText())
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
            case GenericNameSyntax genericNameSyntax:
                if (genericNameSyntax.Identifier.Text == "Array")
                {
                    var inner = genericNameSyntax.TypeArgumentList.ArgumentList[0];
                    var innerSymbol = BindTypeSymbol(inner, scope);
                    if (innerSymbol == null)
                        goto default;

                    return Type.ArrayOf(innerSymbol).Symbol;
                }
                goto default;

            default:
                throw new ArgumentOutOfRangeException(nameof(syntax));
        }
    }

    private TypedExpression BindMemberAccessExpression(
        MemberAccessExpressionSyntax syntax,
        TypedScope scope
    )
    {
        // TODO: combine with BindMemberMethodAccess?
        var node = BindMemberAccess(syntax, scope);
        if (node is TypedExpression expr)
        {
            return expr;
        }

        // TODO: fix this Type.Error put the appropriate TypeSymbol here
        Diagnostics.ReportMissingDefinition(syntax.Location, Type.Error, syntax.Name.ToText());
        return new TypedErrorExpression(syntax);
    }

    private TypedExpression BindCallExpression(CallExpressionSyntax syntax, TypedScope scope)
    {
        // TODO: we should be able to refactor this a bit by extracting all of the IdentifierNameSyntax steps

        var boundArguments = syntax.Arguments
            .Select(argument => BindExpression(argument, scope))
            .ToImmutableArray();

        if (syntax.Expression is IdentifierNameSyntax identifierNameSyntax)
        {
            return BindIdentifierCallExpression(
                syntax,
                identifierNameSyntax,
                boundArguments,
                scope
            );
        }

        if (syntax.Expression is MemberAccessExpressionSyntax memberAccessExpressionSyntax)
        {
            var expression = BindMemberAccess(memberAccessExpressionSyntax, scope);
            if (expression is TypedErrorExpression errorExpression)
                return errorExpression;

            if (expression is TypedMethodExpression boundMethodExpression)
            {
                var methods = boundMethodExpression.Methods
                    .Where(x => x.Parameters.Length == boundArguments.Length)
                    .ToImmutableArray();

                if (methods.Length == 0)
                {
                    Diagnostics.ReportNoOverloads(
                        syntax.Expression.Location,
                        boundMethodExpression.Name,
                        boundArguments.Select(arg => arg.Type.Symbol.Name).ToImmutableArray()
                    );

                    return new TypedErrorExpression(syntax);
                }

                if (methods.Length == 1)
                {
                    return BindCallExpressionToMethodSymbol(
                        syntax,
                        methods[0],
                        boundMethodExpression.Expression,
                        boundArguments
                    );
                }

                return ResolveMethodOverload(
                    syntax,
                    methods,
                    boundMethodExpression,
                    boundArguments
                );
            }

            Diagnostics.ReportUnsupportedFunctionCall(syntax.Expression.Location);
            return new TypedErrorExpression(syntax);
        }

        throw new ArgumentOutOfRangeException(nameof(syntax));
    }

    private TypedExpression ResolveMethodOverload(
        CallExpressionSyntax syntax,
        ImmutableArray<Symbol> methods,
        TypedMethodExpression boundMethodExpression,
        ImmutableArray<TypedExpression> boundArguments
    )
    {
        var symbol = MethodBindCost.Analyze(methods, boundArguments);
        if (symbol == null)
        {
            // TODO: improve type detection and binding when multiple valid methods exist
            Diagnostics.ReportAmbiguousMethod(
                syntax.Expression.Location,
                boundMethodExpression.Name,
                boundArguments.Select(arg => arg.Type.Symbol.Name).ToImmutableArray()
            );

            return new TypedErrorExpression(syntax);
        }

        return BindCallExpressionToMethodSymbol(
            syntax,
            symbol,
            boundMethodExpression.Expression,
            boundArguments
        );
    }

    private TypedExpression BindIdentifierCallExpression(
        CallExpressionSyntax syntax,
        NameSyntax identifierNameSyntax,
        ImmutableArray<TypedExpression> boundArguments,
        TypedScope scope
    )
    {
        // Bind Conversion
        var symbolName = identifierNameSyntax.ToText();
        var boundExpression = TryBindTypeConversion(syntax, symbolName, scope);
        if (boundExpression != null)
            return boundExpression;

        // Bind method
        var symbol = BindIdentifierForCallExpression(
            symbolName,
            identifierNameSyntax.Location,
            boundArguments,
            scope
        );
        if (symbol == null)
            return new TypedErrorExpression(syntax);

        return BindCallExpressionToSymbol(syntax, symbol, null, boundArguments);
    }

    private Symbol? BindIdentifierForCallExpression(
        string symbolName,
        TextLocation nameLocation,
        ImmutableArray<TypedExpression> boundArguments,
        TypedScope scope
    )
    {
        var symbols = scope.LookupSymbol(symbolName, false);

        switch (symbols.Length)
        {
            case 0:
                if (scope.IsRootScope)
                {
                    Diagnostics.ReportNoOverloads(
                        nameLocation,
                        symbolName,
                        boundArguments.Select(arg => arg.Type.Symbol.Name).ToImmutableArray()
                    );

                    return null;
                }
                else
                {
                    // check in parent scope
                    return BindIdentifierForCallExpression(
                        symbolName,
                        nameLocation,
                        boundArguments,
                        scope.Parent
                    );
                }
            case 1:
                return symbols[0];

            default:
                // TODO: see if we have method symbols and try to prune them to the correct one
                var filteredSymbols = symbols
                    .Where(sym => sym.IsMethod && sym.Parameters.Length == boundArguments.Length)
                    .ToImmutableArray();

                switch (filteredSymbols.Length)
                {
                    case 0:
                        Diagnostics.ReportNoOverloads(
                            nameLocation,
                            symbolName,
                            boundArguments.Select(arg => arg.Type.Symbol.Name).ToImmutableArray()
                        );
                        return null;

                    case 1:
                        return filteredSymbols[0];

                    default:
                        var symbol = MethodBindCost.Analyze(filteredSymbols, boundArguments);
                        if (symbol == null)
                        {
                            // TODO: improve type detection and binding when multiple valid methods exist
                            Diagnostics.ReportAmbiguousMethod(
                                nameLocation,
                                symbolName,
                                boundArguments
                                    .Select(arg => arg.Type.Symbol.Name)
                                    .ToImmutableArray()
                            );

                            return null;
                        }

                        return symbol;
                }
        }
    }

    private TypedExpression BindCallExpressionToSymbol(
        CallExpressionSyntax syntax,
        Symbol symbol,
        TypedExpression? expression,
        IReadOnlyList<TypedExpression> boundArguments
    )
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
                    return BindCallExpressionToMethodSymbol(
                        syntax,
                        ctor,
                        expression,
                        boundArguments
                    );
                }

                // TODO: report better error
                Diagnostics.ReportNoOverloads(
                    syntax.Expression.Location,
                    symbol.Name,
                    boundArguments.Select(arg => arg.Type.Symbol.Name).ToImmutableArray()
                );

                return new TypedErrorExpression(syntax);
            }
            case { IsMethod: true }:
                return BindCallExpressionToMethodSymbol(syntax, symbol, expression, boundArguments);

            case { IsValue: true }:
                Diagnostics.ReportNotAFunction(syntax.Expression.Location, symbol.Name);
                return new TypedErrorExpression(syntax);

            default:
                Diagnostics.ReportNoOverloads(
                    syntax.Expression.Location,
                    symbol.Name,
                    boundArguments.Select(arg => arg.Type.Symbol.Name).ToImmutableArray()
                );
                return new TypedErrorExpression(syntax);
        }
    }

    private TypedExpression BindCallExpressionToMethodSymbol(
        CallExpressionSyntax syntax,
        Symbol method,
        TypedExpression? expression,
        IReadOnlyList<TypedExpression> boundArguments
    )
    {
        var convertedArgs = BindArgumentsWithConversions(syntax.Arguments, method, boundArguments);

        return new TypedCallExpression(syntax, method, expression, convertedArgs);
    }

    private ImmutableArray<TypedExpression> BindArgumentsWithConversions(
        SeparatedSyntaxList<ExpressionSyntax> arguments,
        Symbol method,
        IReadOnlyList<TypedExpression> boundArguments
    )
    {
        var convertedArgs = ImmutableArray.CreateBuilder<TypedExpression>();
        for (var i = 0; i < arguments.Count; i++)
        {
            var argument = boundArguments[i];
            var parameter = method.Parameters[i];
            var convertedArgument = BindConversion(arguments[i].Location, argument, parameter.Type);
            convertedArgs.Add(convertedArgument);
        }

        return convertedArgs.ToImmutable();
    }

    private TypedExpression? TryBindTypeConversion(
        CallExpressionSyntax syntax,
        string symbolName,
        TypedScope scope
    )
    {
        if (syntax.Arguments.Count != 1)
            return null;

        var type = LookupBuiltinType(symbolName);
        if (type == null)
            return null;

        return BindExpression(syntax.Arguments[0], type, scope, allowExplicit: true);
    }

    private static Dictionary<string, Type> _builtinLookup =
        new()
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

    private TypedExpression BindForExpression(ForExpressionSyntax syntax, TypedScope scope)
    {
        var lowerTyped = BindExpression(syntax.FromExpression, scope);
        var upperTyped = BindExpression(syntax.ToExpression, scope);

        if (lowerTyped.Type != Type.Int)
        {
            Diagnostics.ReportTypeMismatch(
                syntax.FromExpression.Location,
                Type.Int,
                lowerTyped.Type
            );
            return new TypedErrorExpression(syntax);
        }

        if (upperTyped.Type != Type.Int)
        {
            Diagnostics.ReportTypeMismatch(syntax.ToExpression.Location, Type.Int, upperTyped.Type);
            return new TypedErrorExpression(syntax);
        }

        var newScope = new TypedScope(scope);
        var variable = BindVariable(syntax.Variable, Type.Int, true, newScope);

        var body = BindLoopBody(syntax.Body, newScope, out var breakLabel, out var continueLabel);

        return new TypedForExpression(
            syntax,
            variable,
            lowerTyped,
            upperTyped,
            body,
            breakLabel,
            continueLabel
        );
    }

    private TypedExpression BindWhileExpression(WhileExpressionSyntax syntax, TypedScope scope)
    {
        var condition = BindExpression(syntax.ConditionExpression, Type.Bool, scope);
        var expr = BindLoopBody(syntax.Body, scope, out var breakLabel, out var continueLabel);

        return new TypedWhileExpression(syntax, condition, expr, breakLabel, continueLabel);
    }

    private TypedExpression BindLoopBody(
        ExpressionSyntax syntax,
        TypedScope scope,
        out TypedLabel breakLabel,
        out TypedLabel continueLabel
    )
    {
        _labelCounter++;
        breakLabel = new TypedLabel($"break{_labelCounter}");
        continueLabel = new TypedLabel($"continue{_labelCounter}");

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

    private TypedExpression BindIfExpression(IfExpressionSyntax syntax, TypedScope scope)
    {
        var condition = BindExpression(syntax.ConditionExpression, scope);
        var then = BindExpression(syntax.ThenExpression, scope);
        var elseExpr = BindExpression(syntax.ElseExpression, scope);

        var thenType = TypeResolver.Resolve(then.Type);
        var elseType = TypeResolver.Resolve(elseExpr.Type);
        var conditionType = TypeResolver.Resolve(condition.Type);

        if (conditionType == Type.Error || thenType == Type.Error || elseType == Type.Error)
            return new TypedErrorExpression(syntax);

        if (thenType != elseType)
        {
            Diagnostics.ReportTypeMismatch(syntax.ElseExpression.Location, thenType, elseType);
            return new TypedErrorExpression(syntax);
        }

        if (conditionType != Type.Bool)
        {
            Diagnostics.ReportTypeMismatch(
                syntax.ConditionExpression.Location,
                Type.Bool,
                conditionType
            );
            return new TypedErrorExpression(syntax);
        }

        return new TypedIfExpression(syntax, condition, then, elseExpr);
    }

    private TypedExpression BindUnitExpression(UnitExpressionSyntax syntax) =>
        new TypedUnitExpression(syntax);

    private TypedExpression BindBlockExpression(BlockExpressionSyntax syntax, TypedScope scope)
    {
        var blockScope = new TypedScope(scope);
        var stmts = syntax.Statements
            .Select(stmt => BindStatement(stmt, blockScope))
            .ToImmutableArray();

        var expr = BindExpression(syntax.Expression, blockScope);

        return new TypedBlockExpression(syntax, stmts, expr);
    }

    private static (Symbol root, TypedScope parent) CreateParentScope(
        TypedAssembly? previous,
        ImmutableArray<AssemblyDefinition> references
    )
    {
        var stack = new Stack<TypedAssembly>();

        while (previous != null)
        {
            stack.Push(previous);
            previous = previous.Previous;
        }

        var (root, parent) = CreateRootScope(references);

        while (stack.Count > 0)
        {
            previous = stack.Pop();
            var scope = new TypedScope(parent);

            // import all members from the previous default type which is where top level
            var declaringType = previous.DefaultType;
            if (declaringType != null)
            {
                scope.ImportMembers(declaringType);
            }

            foreach (var type in previous.RootSymbol.Types)
            {
                scope.Import(type);
            }

            parent = scope;
        }

        return (root, parent);
    }

    private static (Symbol root, TypedScope rootScope) CreateRootScope(
        ImmutableArray<AssemblyDefinition> references
    )
    {
        var root = Symbol.NewRoot();
        var rootScope = new TypedScope(root, "root");

        // define default symbols
        // should be alias symbols that map to the real symbols
        rootScope.Import(root.NewClass(TextLocation.None, "any").WithType(Type.Any));
        rootScope.Import(root.NewClass(TextLocation.None, "int").WithType(Type.Int));
        rootScope.Import(root.NewClass(TextLocation.None, "char").WithType(Type.Char));
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
            select new ImportedTypeSymbol(root, "Predef", predef);

        var importedTypeSymbol = importedTypes.FirstOrDefault();
        if (importedTypeSymbol != null)
        {
            rootScope.Import(importedTypeSymbol);
            rootScope.ImportMembers(importedTypeSymbol);
        }

        var namespaceLookup = new Dictionary<string, Symbol>();

        // import all symbols from our references
        foreach (var reference in references)
        {
            foreach (var module in reference.Modules)
            {
                foreach (var type in module.Types)
                {
                    if (!type.IsClass || !type.IsPublic)
                        continue;

                    var namespaceSymbol = GetOrCreateNamespaceSymbol(
                        namespaceLookup,
                        root,
                        type.Namespace
                    );
                    namespaceSymbol.DefineSymbol(
                        new ImportedTypeSymbol(namespaceSymbol, type.Name, type)
                    );
                }
            }
        }

        return (root, rootScope);
    }

    private static Symbol GetOrCreateNamespaceSymbol(
        Dictionary<string, Symbol> namespaceLookup,
        Symbol root,
        string @namespace
    )
    {
        if (string.IsNullOrEmpty(@namespace))
            return root;

        if (namespaceLookup.TryGetValue(@namespace, out var namespaceSymbol))
        {
            return namespaceSymbol;
        }

        var allNamespaces = @namespace.Split(".");
        var symbol = root;
        foreach (var ns in allNamespaces)
        {
            var maybeNamespace = symbol.LookupNamespace(ns);
            if (maybeNamespace == null)
            {
                // create it
                symbol = symbol.NewNamespace(TextLocation.None, ns).Declare();
                continue;
            }

            symbol = maybeNamespace;
        }

        namespaceLookup[@namespace] = symbol;

        return symbol;
    }

    private TypedExpression BindNameExpression(NameSyntax syntax, TypedScope scope)
    {
        if (syntax is IdentifierNameSyntax simpleName)
        {
            var ident = simpleName.Identifier;
            if (ident.IsInsertedToken)
            {
                return new TypedErrorExpression(syntax);
            }

            var name = ident.Text;

            var variable = scope.LookupVariable(name);
            if (variable != null)
            {
                Debug.Assert(!variable.IsField);
                return new TypedVariableExpression(syntax, variable);
            }

            var field = scope.LookupField(name);
            if (field != null)
            {
                return new TypedFieldExpression(syntax, null, field);
            }

            var type = scope.LookupType(name);
            if (type != null)
                return new TypedTypeExpression(syntax, type.Type);

            var ns = scope.LookupNamespace(name);
            if (ns != null)
                return new TypedNamespaceExpression(syntax, ns);

            Diagnostics.ReportUndefinedName(ident.Location, name);
            return new TypedErrorExpression(syntax);
        }

        Diagnostics.ReportUnsupportedFieldAccess(syntax.Location, "<err>");
        return new TypedErrorExpression(syntax);
    }

    private TypedExpression BindGroupExpression(GroupExpressionSyntax syntax, TypedScope scope) =>
        BindExpression(syntax.Expression, scope);

    private TypedExpression BindUnaryExpression(UnaryExpressionSyntax syntax, TypedScope scope)
    {
        var boundOperand = BindExpression(syntax.Operand, scope);
        if (boundOperand.Type == Type.Error)
            return new TypedErrorExpression(syntax);

        var boundOperator = TypedUnaryOperator.Bind(syntax.OperatorToken.Kind, boundOperand.Type);

        if (boundOperator == null)
        {
            Diagnostics.ReportUndefinedUnaryOperator(
                syntax.OperatorToken.Location,
                syntax.OperatorToken.Text,
                boundOperand.Type
            );
            return new TypedErrorExpression(syntax);
        }

        return new TypedUnaryExpression(syntax, boundOperator, boundOperand);
    }

    private TypedExpression BindIndexExpression(IndexExpressionSyntax syntax, TypedScope scope)
    {
        var expr = BindExpression(syntax.Expression, scope);
        var index = BindExpression(syntax.Index, scope);

        if (expr.Type == Type.Error || index.Type == Type.Error)
            return new TypedErrorExpression(syntax);

        var symbol = expr.Type.Symbol;

        if (expr.Type is ArrayType && index.Type == Type.Int)
        {
            return new TypedIndexExpression(syntax, expr, index, null, null);
        }

        // TODO: support subtyping
        // HACK: add find by `get_` prefix since string has a different name for the default index operator
        // TODO: once we support attributes we would need to look for the type with the DefaultMember attribute
        var getters = symbol.Members.Where(m => m.IsMethod && m.Name.StartsWith("get_"));
        var getter = getters.FirstOrDefault(
            m => m.Parameters.Length == 1 && m.Parameters[0].Type == index.Type
        );
        var setter = symbol.Members
            .Where(m => m.IsMethod && m.Name.StartsWith("set_"))
            .FirstOrDefault(m => m.Parameters.Length == 2 && m.Parameters[0].Type == index.Type);

        if (getter == null && setter == null)
        {
            Diagnostics.ReportExpressionDoesNotSupportIndexOperator(syntax.Expression.Location);
            return new TypedErrorExpression(syntax);
        }

        return new TypedIndexExpression(syntax, expr, index, getter, setter);
    }

    private TypedExpression BindBinaryExpression(BinaryExpressionSyntax syntax, TypedScope scope)
    {
        var left = BindExpression(syntax.Left, scope);
        var right = BindExpression(syntax.Right, scope);
        var leftType = TypeResolver.Resolve(left.Type);
        var rightType = TypeResolver.Resolve(right.Type);
        var boundOperator = TypedBinaryOperator.Bind(
            syntax.OperatorToken.Kind,
            leftType,
            rightType
        );

        if (leftType == Type.Error || rightType == Type.Error)
            return new TypedErrorExpression(syntax);

        if (boundOperator == null)
        {
            Diagnostics.ReportUndefinedBinaryOperator(
                syntax.OperatorToken.Location,
                syntax.OperatorToken.Text,
                leftType,
                rightType
            );
            return new TypedErrorExpression(syntax);
        }

        return new TypedBinaryExpression(syntax, left, boundOperator, right);
    }

    private TypedExpression BindLiteralExpression(LiteralExpressionSyntax syntax)
    {
        var value = syntax.Value;
        return value == null
            ? new TypedErrorExpression(syntax)
            : new TypedLiteralExpression(syntax, value);
    }
}
