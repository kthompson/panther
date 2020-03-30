using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class Binder
    {
        public static BoundGlobalScope BindGlobalScope(BoundGlobalScope? previous, CompilationUnitSyntax syntax)
        {
            var parentScope = CreateParentScope(previous);
            var scope = new BoundScope(parentScope);
            var binder = new Binder();
            var statement = binder.BindStatement(syntax.Statement, scope);
            var variables = scope.GetDeclaredVariables();
            var diagnostics = binder.Diagnostics.ToImmutableArray();

            if (previous != null)
            {
                diagnostics = diagnostics.InsertRange(0, previous.Diagnostics);
            }

            return new BoundGlobalScope(previous, diagnostics, variables, statement);
        }

        public DiagnosticBag Diagnostics { get; } = new DiagnosticBag();

        private BoundStatement BindStatement(StatementSyntax syntax, BoundScope scope)
        {
            return syntax.Kind switch
            {
                SyntaxKind.VariableDeclarationStatement => BindVariableDeclarationStatement((VariableDeclarationStatementSyntax)syntax, scope),
                SyntaxKind.ExpressionStatement => BindExpressionStatement((ExpressionStatementSyntax)syntax, scope),
                _ => throw new Exception($"Unexpected syntax {syntax.Kind}")
            };
        }

        private BoundExpression BindAssignmentExpression(AssignmentExpressionSyntax syntax, BoundScope scope)
        {
            var boundExpression = BindExpression(syntax.Expression, scope);

            if (!scope.TryLookup(syntax.IdentifierToken.Text, out var variable))
            {
                Diagnostics.ReportUndefinedName(syntax.IdentifierToken.Span, syntax.IdentifierToken.Text);

                return BoundErrorExpression.Default;
            }

            if (variable.IsReadOnly)
            {
                Diagnostics.ReportReassignmentToVal(syntax.IdentifierToken.Span, syntax.IdentifierToken.Text);
                return BoundErrorExpression.Default;
            }

            if (variable.Type != boundExpression.Type)
            {
                Diagnostics.ReportTypeMismatch(syntax.Expression.Span, variable.Type, boundExpression.Type);
                return BoundErrorExpression.Default;
            }

            return new BoundAssignmentExpression(variable, boundExpression);
        }

        private BoundStatement BindVariableDeclarationStatement(VariableDeclarationStatementSyntax syntax, BoundScope scope)
        {
            var boundExpression = BindExpression(syntax.Expression, scope);
            var expressionType = boundExpression.Type;

            var variable = BindVariable(syntax.IdentifierToken, expressionType, syntax.ValOrVarToken.Kind == SyntaxKind.ValKeyword, scope);

            return new BoundVariableDeclarationStatement(variable, boundExpression);
        }

        private VariableSymbol BindVariable(SyntaxToken identifier, TypeSymbol expressionType, bool isReadOnly,
            BoundScope scope)
        {
            var name = identifier.Text ?? "??";
            var declare = !identifier.IsInsertedToken;
            var variable = new VariableSymbol(name, isReadOnly, expressionType);

            if (declare && !scope.TryDeclare(variable))
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

        public BoundExpression BindExpression(ExpressionSyntax syntax, BoundScope scope)
        {
            return syntax.Kind switch
            {
                SyntaxKind.LiteralExpression => BindLiteralExpression((LiteralExpressionSyntax)syntax),
                SyntaxKind.UnitExpression => BindUnitExpression(),
                SyntaxKind.AssignmentExpression => BindAssignmentExpression((AssignmentExpressionSyntax)syntax, scope),
                SyntaxKind.BinaryExpression => BindBinaryExpression((BinaryExpressionSyntax)syntax, scope),
                SyntaxKind.UnaryExpression => BindUnaryExpression((UnaryExpressionSyntax)syntax, scope),
                SyntaxKind.GroupExpression => BindGroupExpression((GroupExpressionSyntax)syntax, scope),
                SyntaxKind.NameExpression => BindNameExpression((NameExpressionSyntax)syntax, scope),
                SyntaxKind.BlockExpression => BindBlockExpression((BlockExpressionSyntax)syntax, scope),
                SyntaxKind.IfExpression => BindIfExpression((IfExpressionSyntax)syntax, scope),
                SyntaxKind.WhileExpression => BindWhileExpression((WhileExpressionSyntax)syntax, scope),
                SyntaxKind.ForExpression => BindForExpression((ForExpressionSyntax)syntax, scope),
                SyntaxKind.CallExpression => BindCallExpression((CallExpressionSyntax)syntax, scope),
                _ => throw new Exception($"Unexpected syntax {syntax.Kind}")
            };
        }

        private BoundExpression BindCallExpression(CallExpressionSyntax syntax, BoundScope scope)
        {
            var function = BuiltinFunctions.GetAll().SingleOrDefault(function => function.Name == syntax.IdentifierToken.Text);

            //if (!scope.TryLookup(syntax.IdentifierToken.Text, out var variable))
            if (function == null)
            {
                Diagnostics.ReportUndefinedFunction(syntax.IdentifierToken.Span, syntax.IdentifierToken.Text);
                return BoundErrorExpression.Default;
            }

            //if (!(variable.Type is FunctionSymbol functionType))
            //{
            //    Diagnostics.ReportNotAFunction(syntax.IdentifierToken.Span, syntax.IdentifierToken.Text);
            //    return BoundErrorExpression.Default;
            //}

            if (syntax.Arguments.Count != function.Parameters.Length)
            {
                Diagnostics.ReportIncorrectNumberOfArgumentsForFunction(syntax.IdentifierToken.Span,
                    syntax.IdentifierToken.Text, function.Parameters.Length, syntax.Arguments.Count);
                return BoundErrorExpression.Default;
            }

            var argList = new List<BoundExpression>();
            for (int i = 0; i < syntax.Arguments.Count; i++)
            {
                var argument = syntax.Arguments[i];
                var parameter = function.Parameters[i];

                var expr = BindExpression(argument, scope);
                if (expr.Type != parameter.Type)
                {
                    Diagnostics.ReportArgumentTypeMismatch(argument.Span, parameter.Name, parameter.Type, expr.Type);
                    continue;
                }

                argList.Add(expr);
            }

            if (argList.Count != function.Parameters.Length)
                return BoundErrorExpression.Default;

            return new BoundCallExpression(function, argList.ToImmutableArray());
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

            var body = BindExpression(syntax.Body, newScope);

            return new BoundForExpression(variable, lowerBound, upperBound, body);
        }

        private BoundExpression BindWhileExpression(WhileExpressionSyntax syntax, BoundScope scope)
        {
            var condition = BindExpression(syntax.ConditionExpression, scope);
            var expr = BindExpression(syntax.Body, scope);

            if (condition.Type == TypeSymbol.Error || expr.Type == TypeSymbol.Error)
                return BoundErrorExpression.Default;

            if (condition.Type != TypeSymbol.Bool)
            {
                Diagnostics.ReportTypeMismatch(syntax.ConditionExpression.Span, TypeSymbol.Bool, condition.Type);
                return BoundErrorExpression.Default;
            }

            return new BoundWhileExpression(condition, expr);
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

            BoundScope? parent = null;

            while (stack.Count > 0)
            {
                previous = stack.Pop();
                var scope = new BoundScope(parent);
                foreach (var v in previous.Variables)
                    scope.TryDeclare(v);

                parent = scope;
            }

            return parent;
        }

        private BoundExpression BindNameExpression(NameExpressionSyntax syntax, BoundScope scope)
        {
            if (syntax.IdentifierToken.IsInsertedToken)
            {
                return BoundErrorExpression.Default;
            }

            var name = syntax.IdentifierToken.Text;

            if (scope.TryLookup(name, out var variable))
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