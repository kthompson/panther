using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
                SyntaxKind.AssignmentStatement => BindAssignmentStatement((AssignmentStatementSyntax)syntax, scope),
                SyntaxKind.ExpressionStatement => BindExpressionStatement((ExpressionStatementSyntax)syntax, scope),
                _ => throw new Exception($"Unexpected syntax {syntax.Kind}")
            };
        }

        private BoundStatement BindAssignmentStatement(AssignmentStatementSyntax syntax, BoundScope scope)
        {
            var name = syntax.IdentifierToken.Text;
            var isReadOnly = syntax.ValOrVarToken.Kind == SyntaxKind.ValKeyword;
            var boundExpression = BindExpression(syntax.Expression, scope);

            var variable = new VariableSymbol(name, isReadOnly, boundExpression.Type);

            scope.TryDeclare(variable);
            //if (!_scope.TryDeclare(variable))
            //{
            //    Diagnostics.ReportVariableAlreadyDefined(syntax.IdentifierToken.Span, name);
            //}

            return new BoundAssignmentStatement(variable, boundExpression);
        }

        private BoundStatement BindExpressionStatement(ExpressionStatementSyntax syntax, BoundScope scope)
        {
            var expression = BindExpression(syntax.Expression, scope);

            return new BoundExpressionStatement(expression);
        }

        public BoundExpression BindExpression(ExpressionSyntax syntax, BoundScope scope)
        {
            return syntax.Kind switch
            {
                SyntaxKind.LiteralExpression => BindLiteralExpression((LiteralExpressionSyntax)syntax),
                SyntaxKind.UnitExpression => BindUnitExpression(),
                SyntaxKind.BinaryExpression => BindBinaryExpression((BinaryExpressionSyntax)syntax, scope),
                SyntaxKind.UnaryExpression => BindUnaryExpression((UnaryExpressionSyntax)syntax, scope),
                SyntaxKind.GroupExpression => BindGroupExpression((GroupExpressionSyntax)syntax, scope),
                SyntaxKind.NameExpression => BindNameExpression((NameExpressionSyntax)syntax, scope),
                SyntaxKind.BlockExpression => BindBlockExpression((BlockExpressionSyntax)syntax, scope),
                _ => throw new Exception($"Unexpected syntax {syntax.Kind}")
            };
        }

        private BoundExpression BindUnitExpression() => new BoundUnitExpression();

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
            var name = syntax.IdentifierToken.Text;

            if (scope.TryLookup(name, out var variable))
                return new BoundVariableExpression(variable);

            Diagnostics.ReportUndefinedName(syntax.IdentifierToken.Span, name);
            return new BoundLiteralExpression(0);
        }

        private BoundExpression BindGroupExpression(GroupExpressionSyntax syntax, BoundScope scope)
        {
            return BindExpression(syntax.Expression, scope);
        }

        private BoundExpression BindUnaryExpression(UnaryExpressionSyntax syntax, BoundScope scope)
        {
            var boundOperand = BindExpression(syntax.Operand, scope);
            var boundOperator = BoundUnaryOperator.Bind(syntax.OperatorToken.Kind, boundOperand.Type);

            if (boundOperator == null)
            {
                Diagnostics.ReportUndefinedUnaryOperator(syntax.OperatorToken.Span, syntax.OperatorToken.Text, boundOperand.Type);
                return boundOperand;
            }

            return new BoundUnaryExpression(boundOperator, boundOperand);
        }

        private BoundExpression BindBinaryExpression(BinaryExpressionSyntax syntax, BoundScope scope)
        {
            var left = BindExpression(syntax.Left, scope);
            var right = BindExpression(syntax.Right, scope);
            var boundOperator = BoundBinaryOperator.Bind(syntax.OperatorToken.Kind, left.Type, right.Type);

            if (boundOperator == null)
            {
                Diagnostics.ReportUndefinedBinaryOperator(syntax.OperatorToken.Span, syntax.OperatorToken.Text, left.Type, right.Type);
                return left;
            }
            return new BoundBinaryExpression(left, boundOperator, right);
        }

        private BoundExpression BindLiteralExpression(LiteralExpressionSyntax syntax)
        {
            var value = syntax.Value ?? 0;

            return new BoundLiteralExpression(value);
        }
    }
}