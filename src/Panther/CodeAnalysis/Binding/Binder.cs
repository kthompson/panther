using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks.Sources;
using Panther.CodeAnalysis.Lowering;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class Binder
    {
        public DiagnosticBag Diagnostics { get; } = new DiagnosticBag();

        public static BoundGlobalScope BindGlobalScope(BoundGlobalScope? previous, CompilationUnitSyntax syntax)
        {
            var parentScope = CreateParentScope(previous);
            var scope = new BoundScope(parentScope, function: null);
            var binder = new Binder();

            foreach (var function in syntax.Members.OfType<FunctionDeclarationSyntax>())
                binder.BindFunctionDeclaration(function, scope);

            var statements = ImmutableArray.CreateBuilder<BoundStatement>();
            foreach (var globalStatementSyntax in syntax.Members.OfType<GlobalStatementSyntax>())
            {
                var boundStatement = binder.BindStatement(globalStatementSyntax.Statement, scope);
                statements.Add(boundStatement);
            }

            var variables = scope.GetDeclaredVariables();
            var functions = scope.GetDeclaredFunctions();
            var diagnostics = binder.Diagnostics.ToImmutableArray();

            if (previous != null)
            {
                diagnostics = diagnostics.InsertRange(0, previous.Diagnostics);
            }

            return new BoundGlobalScope(previous, diagnostics, variables, functions, statements.ToImmutable());
        }

        public static BoundProgram BindProgram(BoundGlobalScope globalScope)
        {
            var binder = new Binder();
            var parentScope = CreateParentScope(globalScope);

            var functionBodies = ImmutableDictionary.CreateBuilder<FunctionSymbol, BoundBlockExpression>();

            BoundGlobalScope? scope = globalScope;

            while (scope != null)
            {
                foreach (var function in scope.Functions)
                {
                    var functionScope = new BoundScope(parentScope, function);

                    var body = binder.BindExpression(function.Declaration.Body, functionScope);

                    var loweredBody = Lowerer.Lower(new BoundExpressionStatement(body));

                    if (function.ReturnType != TypeSymbol.Unit && !ControlFlowGraph.AllBlocksReturn(loweredBody))
                    {
                        binder.Diagnostics.ReportAllPathsMustReturn(function.Declaration.Identifier.Span);
                    }

                    functionBodies.Add(function, loweredBody);
                }

                scope = scope.Previous;
            }

            var statement = Lowerer.Lower(BoundStatementFromStatements(globalScope.Statements));

            return new BoundProgram(binder.Diagnostics.ToImmutableArray(), functionBodies.ToImmutable(), statement);
        }

        private static BoundExpressionStatement BoundStatementFromStatements(IReadOnlyCollection<BoundStatement> statements)
        {
            var expr = (statements.LastOrDefault() as BoundExpressionStatement)?.Expression;
            var stmts = expr == null
                ? statements.ToImmutableArray()
                : statements.Take(statements.Count - 1).ToImmutableArray();

            var statement = new BoundExpressionStatement(new BoundBlockExpression(stmts, expr ?? BoundUnitExpression.Default));
            return statement;
        }

        private void BindFunctionDeclaration(FunctionDeclarationSyntax syntax, BoundScope scope)
        {
            var parameters = ImmutableArray.CreateBuilder<ParameterSymbol>();

            var seenParamNames = new HashSet<string>();

            foreach (var parameterSyntax in syntax.Parameters)
            {
                var parameterName = parameterSyntax.Identifier.Text;
                var parameterType = BindTypeAnnotation(parameterSyntax.TypeAnnotation);

                if (!seenParamNames.Add(parameterName))
                {
                    Diagnostics.ReportParameterAlreadyDeclared(parameterSyntax.Span, parameterName);
                }
                else
                {
                    var parameter = new ParameterSymbol(parameterName, parameterType);
                    parameters.Add(parameter);
                }
            }

            var type = BindTypeAnnotation(syntax.TypeAnnotation) ?? TypeSymbol.Unit;

            var function = new FunctionSymbol(syntax.Identifier.Text, parameters.ToImmutable(), type, syntax);

            if (!scope.TryDeclareFunction(function))
            {
                Diagnostics.ReportFunctionAlreadyDeclared(syntax.Identifier.Span, function.Name);
            }
        }

        private BoundStatement BindStatement(StatementSyntax syntax, BoundScope scope) =>
            syntax switch
            {
                ExpressionStatementSyntax expressionStatementSyntax => BindExpressionStatement(expressionStatementSyntax, scope),
                VariableDeclarationStatementSyntax variableDeclarationStatementSyntax => BindVariableDeclarationStatement(variableDeclarationStatementSyntax, scope),
                _ => throw new ArgumentOutOfRangeException(nameof(syntax))
            };

        private BoundStatement BindContinueStatement(ContinueExpressionSyntax syntax, BoundScope scope)
        {
            var label = scope.GetContinueLabel();
            if (label == null)
            {
                Diagnostics.ReportInvalidBreakOrContinue(syntax.ContinueKeyword.Span, syntax.ContinueKeyword.Text);
                return new BoundExpressionStatement(BoundErrorExpression.Default);
            }

            return new BoundGotoStatement(label);
        }

        private BoundStatement BindBreakStatement(BreakExpressionSyntax syntax, BoundScope scope)
        {
            var label = scope.GetBreakLabel();
            if (label == null)
            {
                Diagnostics.ReportInvalidBreakOrContinue(syntax.BreakKeyword.Span, syntax.BreakKeyword.Text);
                return new BoundExpressionStatement(BoundErrorExpression.Default);
            }

            return new BoundGotoStatement(label);
        }

        private BoundExpression BindAssignmentExpression(AssignmentExpressionSyntax syntax, BoundScope scope)
        {
            var boundExpression = BindExpression(syntax.Expression, scope);

            if (!scope.TryLookupVariable(syntax.IdentifierToken.Text, out var variable))
            {
                Diagnostics.ReportUndefinedName(syntax.IdentifierToken.Span, syntax.IdentifierToken.Text);

                return BoundErrorExpression.Default;
            }

            if (variable.IsReadOnly)
            {
                Diagnostics.ReportReassignmentToVal(syntax.IdentifierToken.Span, syntax.IdentifierToken.Text);
                return BoundErrorExpression.Default;
            }

            var convertedExpression = BindConversion(syntax.Expression.Span, boundExpression, variable.Type);

            return new BoundAssignmentExpression(variable, convertedExpression);
        }

        private BoundStatement BindVariableDeclarationStatement(VariableDeclarationStatementSyntax syntax, BoundScope scope)
        {
            var isReadOnly = syntax.ValOrVarToken.Kind == SyntaxKind.ValKeyword;
            var boundExpression = BindExpression(syntax.Expression, scope);
            var type = BindTypeAnnotation(syntax.TypeAnnotation);
            var expressionType = type ?? boundExpression.Type;

            var converted = BindConversion(syntax.Expression.Span, boundExpression, expressionType);
            var variable = BindVariable(syntax.IdentifierToken, expressionType, isReadOnly, scope);

            return new BoundVariableDeclarationStatement(variable, converted);
        }

        private TypeSymbol? BindTypeAnnotation(TypeAnnotationSyntax? syntaxTypeClause)
        {
            if (syntaxTypeClause == null)
                return null;

            var type = LookupType(syntaxTypeClause.IdentifierToken.Text);

            if (type == null)
            {
                Diagnostics.ReportUndefinedType(syntaxTypeClause.IdentifierToken.Span, syntaxTypeClause.IdentifierToken.Text);
            }

            return type;
        }

        private VariableSymbol BindVariable(SyntaxToken identifier, TypeSymbol expressionType, bool isReadOnly,
            BoundScope scope)
        {
            var name = identifier.Text ?? "??";
            var declare = !identifier.IsInsertedToken;
            var variable = scope.IsGlobalScope
                ? (VariableSymbol)new GlobalVariableSymbol(name, isReadOnly, expressionType)
                : new LocalVariableSymbol(name, isReadOnly, expressionType);

            if (declare && !scope.TryDeclareVariable(variable))
            {
                Diagnostics.ReportVariableAlreadyDefined(identifier.Span, name);
            }

            return variable;
        }

        private BoundStatement BindExpressionStatement(ExpressionStatementSyntax syntax, BoundScope scope)
        {
            var expression = BindExpression(syntax.Expression, scope);

            return new BoundExpressionStatement(expression.Type == TypeSymbol.Error ? BoundErrorExpression.Default : expression);
        }

        private BoundExpression BindExpression(ExpressionSyntax syntax, BoundScope scope)
        {
            return syntax switch
            {
                BreakExpressionSyntax breakStatementSyntax => BindStatementToExpression(BindBreakStatement(breakStatementSyntax, scope)),
                ContinueExpressionSyntax continueStatementSyntax => BindStatementToExpression(BindContinueStatement(continueStatementSyntax, scope)),
                LiteralExpressionSyntax literalExpressionSyntax => BindLiteralExpression(literalExpressionSyntax),
                AssignmentExpressionSyntax assignmentExpressionSyntax => BindAssignmentExpression(assignmentExpressionSyntax, scope),
                BinaryExpressionSyntax binaryExpressionSyntax => BindBinaryExpression(binaryExpressionSyntax, scope),
                UnaryExpressionSyntax unaryExpressionSyntax => BindUnaryExpression(unaryExpressionSyntax, scope),
                GroupExpressionSyntax groupExpressionSyntax => BindGroupExpression(groupExpressionSyntax, scope),
                NameExpressionSyntax nameExpressionSyntax => BindNameExpression(nameExpressionSyntax, scope),
                BlockExpressionSyntax blockExpressionSyntax => BindBlockExpression(blockExpressionSyntax, scope),
                IfExpressionSyntax ifExpressionSyntax => BindIfExpression(ifExpressionSyntax, scope),
                WhileExpressionSyntax whileExpressionSyntax => BindWhileExpression(whileExpressionSyntax, scope),
                ForExpressionSyntax forExpressionSyntax => BindForExpression(forExpressionSyntax, scope),
                CallExpressionSyntax callExpressionSyntax => BindCallExpression(callExpressionSyntax, scope),
                UnitExpressionSyntax unit => BindUnitExpression(),
                _ => throw new Exception($"Unexpected syntax {syntax.Kind}")
            };
        }

        // hack to convert a statement into an expression
        private static BoundBlockExpression BindStatementToExpression(BoundStatement bindBreakStatement) =>
            new BoundBlockExpression(ImmutableArray.Create(bindBreakStatement), BoundUnitExpression.Default);

        private BoundExpression BindExpression(ExpressionSyntax syntax, TypeSymbol type, BoundScope scope, bool allowExplicit = false)
        {
            var expression = BindExpression(syntax, scope);
            return BindConversion(syntax.Span, expression, type, allowExplicit);
        }

        private BoundExpression BindConversion(TextSpan diagnosticsSpan, BoundExpression expression, TypeSymbol type, bool allowExplicit = false)
        {
            var conversion = Conversion.Classify(expression.Type, type);

            if (!conversion.Exists)
            {
                if (expression.Type != TypeSymbol.Error && type != TypeSymbol.Error)
                {
                    Diagnostics.ReportCannotConvert(diagnosticsSpan, expression.Type, type);
                }

                return BoundErrorExpression.Default;
            }

            if (!allowExplicit && conversion.IsExplicit)
            {
                Diagnostics.ReportCannotConvertImplicitly(diagnosticsSpan, expression.Type, type);
            }

            if (conversion.IsIdentity)
                return expression;

            return new BoundConversionExpression(type, expression);
        }

        private BoundExpression BindCallExpression(CallExpressionSyntax syntax, BoundScope scope)
        {
            if (syntax.Arguments.Count == 1 && LookupType(syntax.IdentifierToken.Text) is { } type)
                return BindExpression(syntax.Arguments[0], type, scope, allowExplicit: true);

            var boundArguments = syntax.Arguments.Select(argument => BindExpression(argument, scope)).ToList();
            var argTypes = boundArguments.Select(argument => argument.Type).ToImmutableArray();

            if (!scope.TryLookup(syntax.IdentifierToken.Text, out var symbol))
            {
                Diagnostics.ReportUndefinedFunction(syntax.IdentifierToken.Span, syntax.IdentifierToken.Text);
                return BoundErrorExpression.Default;
            }

            if (!(symbol is FunctionSymbol function))
            {
                Diagnostics.ReportNotAFunction(syntax.IdentifierToken.Span, syntax.IdentifierToken.Text);
                return BoundErrorExpression.Default;
            }

            if (syntax.Arguments.Count != function.Parameters.Length)
            {
                Diagnostics.ReportNoOverloads(syntax.IdentifierToken.Span, syntax.IdentifierToken.Text, argTypes.Select(arg => arg.Name).ToImmutableArray());
                return BoundErrorExpression.Default;
            }

            for (int i = 0; i < syntax.Arguments.Count; i++)
            {
                var argument = boundArguments[i];
                var parameter = function.Parameters[i];

                if (argument.Type != parameter.Type)
                {
                    Diagnostics.ReportArgumentTypeMismatch(syntax.Arguments[i].Span, parameter.Name, parameter.Type, argument.Type);
                    return BoundErrorExpression.Default;
                }
            }

            return new BoundCallExpression(function, boundArguments.ToImmutableArray());
        }

        private TypeSymbol? LookupType(string text)
        {
            var types = new[]
            {
                TypeSymbol.Int,
                TypeSymbol.Bool,
                TypeSymbol.String,
            };

            return types.FirstOrDefault(type => type.Name == text);
        }

        private BoundExpression BindForExpression(ForExpressionSyntax syntax, BoundScope scope)
        {
            var lowerBound = BindExpression(syntax.FromExpression, scope);
            var upperBound = BindExpression(syntax.ToExpression, scope);

            if (lowerBound.Type != TypeSymbol.Int)
            {
                Diagnostics.ReportTypeMismatch(syntax.FromExpression.Span, TypeSymbol.Int, lowerBound.Type);
                return BoundErrorExpression.Default;
            }

            if (upperBound.Type != TypeSymbol.Int)
            {
                Diagnostics.ReportTypeMismatch(syntax.ToExpression.Span, TypeSymbol.Int, upperBound.Type);
                return BoundErrorExpression.Default;
            }

            var newScope = new BoundScope(scope);
            var variable = BindVariable(syntax.VariableExpression.IdentifierToken, TypeSymbol.Int, true, newScope);

            var body = BindLoopBody(syntax.Body, newScope, out var breakLabel, out var continueLabel);

            return new BoundForExpression(variable, lowerBound, upperBound, body, breakLabel, continueLabel);
        }

        private BoundExpression BindWhileExpression(WhileExpressionSyntax syntax, BoundScope scope)
        {
            var condition = BindExpression(syntax.ConditionExpression, TypeSymbol.Bool, scope);
            var expr = BindLoopBody(syntax.Body, scope, out var breakLabel, out var continueLabel);

            return new BoundWhileExpression(condition, expr, breakLabel, continueLabel);
        }

        private BoundExpression BindLoopBody(ExpressionSyntax syntax, BoundScope scope, out BoundLabel breakLabel,
            out BoundLabel continueLabel)
        {
            scope.DeclareLoop(out breakLabel, out continueLabel);
            return BindExpression(syntax, scope);
        }

        private BoundExpression BindIfExpression(IfExpressionSyntax syntax, BoundScope scope)
        {
            var condition = BindExpression(syntax.ConditionExpression, scope);
            var then = BindExpression(syntax.ThenExpression, scope);
            var elseExpr = BindExpression(syntax.ElseExpression, scope);

            if (condition.Type == TypeSymbol.Error || then.Type == TypeSymbol.Error || elseExpr.Type == TypeSymbol.Error)
                return BoundErrorExpression.Default;

            if (then.Type != elseExpr.Type)
            {
                Diagnostics.ReportTypeMismatch(syntax.ElseExpression.Span, then.Type, elseExpr.Type);
                return BoundErrorExpression.Default;
            }

            if (condition.Type != TypeSymbol.Bool)
            {
                Diagnostics.ReportTypeMismatch(syntax.ConditionExpression.Span, TypeSymbol.Bool, condition.Type);
                return BoundErrorExpression.Default;
            }

            return new BoundIfExpression(condition, then, elseExpr);
        }

        private BoundExpression BindUnitExpression() => BoundUnitExpression.Default;

        private BoundExpression BindBlockExpression(BlockExpressionSyntax syntax, BoundScope scope)
        {
            var blockScope = new BoundScope(scope);
            var stmts = syntax.Statements.Select(stmt => BindStatement(stmt, blockScope)).ToImmutableArray();

            var expr = BindExpression(syntax.Expression, blockScope);

            return new BoundBlockExpression(stmts, expr);
        }

        private static BoundScope? CreateParentScope(BoundGlobalScope? previous)
        {
            var stack = new Stack<BoundGlobalScope>();

            while (previous != null)
            {
                stack.Push(previous);
                previous = previous.Previous;
            }

            var parent = CreateRootScope();

            while (stack.Count > 0)
            {
                previous = stack.Pop();
                var scope = new BoundScope(parent);
                foreach (var v in previous.Variables)
                    scope.TryDeclareVariable(v);

                foreach (var v in previous.Functions)
                    scope.TryDeclareFunction(v);

                parent = scope;
            }

            return parent;
        }

        private static BoundScope CreateRootScope()
        {
            var result = new BoundScope(null);
            foreach (var symbol in BuiltinFunctions.GetAll())
            {
                result.TryDeclareFunction(symbol);
            }

            return result;
        }

        private BoundExpression BindNameExpression(NameExpressionSyntax syntax, BoundScope scope)
        {
            if (syntax.IdentifierToken.IsInsertedToken)
            {
                return BoundErrorExpression.Default;
            }

            var name = syntax.IdentifierToken.Text;

            if (scope.TryLookupVariable(name, out var variable))
                return new BoundVariableExpression(variable);

            Diagnostics.ReportUndefinedName(syntax.IdentifierToken.Span, name);
            return BoundErrorExpression.Default;
        }

        private BoundExpression BindGroupExpression(GroupExpressionSyntax syntax, BoundScope scope) =>
            BindExpression(syntax.Expression, scope);

        private BoundExpression BindUnaryExpression(UnaryExpressionSyntax syntax, BoundScope scope)
        {
            var boundOperand = BindExpression(syntax.Operand, scope);
            if (boundOperand.Type == TypeSymbol.Error)
                return BoundErrorExpression.Default;

            var boundOperator = BoundUnaryOperator.Bind(syntax.OperatorToken.Kind, boundOperand.Type);

            if (boundOperator == null)
            {
                Diagnostics.ReportUndefinedUnaryOperator(syntax.OperatorToken.Span, syntax.OperatorToken.Text, boundOperand.Type);
                return BoundErrorExpression.Default;
            }

            return new BoundUnaryExpression(boundOperator, boundOperand);
        }

        private BoundExpression BindBinaryExpression(BinaryExpressionSyntax syntax, BoundScope scope)
        {
            var left = BindExpression(syntax.Left, scope);
            var right = BindExpression(syntax.Right, scope);
            var boundOperator = BoundBinaryOperator.Bind(syntax.OperatorToken.Kind, left.Type, right.Type);

            if (left.Type == TypeSymbol.Error || right.Type == TypeSymbol.Error)
                return BoundErrorExpression.Default;

            if (boundOperator == null)
            {
                Diagnostics.ReportUndefinedBinaryOperator(syntax.OperatorToken.Span, syntax.OperatorToken.Text, left.Type, right.Type);
                return BoundErrorExpression.Default;
            }
            return new BoundBinaryExpression(left, boundOperator, right);
        }

        private BoundExpression BindLiteralExpression(LiteralExpressionSyntax syntax)
        {
            var value = syntax.Value;
            return value == null ? (BoundExpression)BoundErrorExpression.Default : new BoundLiteralExpression(value);
        }
    }
}