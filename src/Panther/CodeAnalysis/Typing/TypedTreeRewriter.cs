using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Panther.CodeAnalysis.Typing;

internal abstract class TypedTreeRewriter
{
    protected virtual TypedExpression RewriteExpression(TypedExpression node) =>
        node switch
        {
            TypedAssignmentExpression boundAssignmentExpression
                => RewriteAssignmentExpression(boundAssignmentExpression),
            TypedArrayCreationExpression boundAssignmentArrayCreationExpression
                => RewriteArrayCreationExpression(boundAssignmentArrayCreationExpression),
            TypedBinaryExpression boundBinaryExpression
                => RewriteBinaryExpression(boundBinaryExpression),
            TypedBlockExpression boundBlockExpression
                => RewriteBlockExpression(boundBlockExpression),
            TypedCallExpression callExpression => RewriteCallExpression(callExpression),
            TypedConversionExpression conversionExpression
                => RewriteConversionExpression(conversionExpression),
            TypedErrorExpression errorExpression => RewriteErrorExpression(errorExpression),
            TypedFieldExpression boundFieldExpression
                => RewriteFieldExpression(boundFieldExpression),
            TypedForExpression boundForExpression => RewriteForExpression(boundForExpression),
            TypedIfExpression boundIfExpression => RewriteIfExpression(boundIfExpression),
            TypedIndexExpression boundIndexExpression
                => RewriteIndexExpression(boundIndexExpression),
            TypedLiteralExpression boundLiteralExpression
                => RewriteLiteralExpression(boundLiteralExpression),
            TypedNewExpression boundLiteralExpression
                => RewriteNewExpression(boundLiteralExpression),
            TypedNullExpression boundNullExpression => RewriteNullExpression(boundNullExpression),
            TypedPropertyExpression boundPropertyExpression
                => RewritePropertyExpression(boundPropertyExpression),
            TypedTypeExpression boundTypeExpression => RewriteTypeExpression(boundTypeExpression),
            TypedUnaryExpression boundUnaryExpression
                => RewriteUnaryExpression(boundUnaryExpression),
            TypedUnitExpression boundUnitExpression => RewriteUnitExpression(boundUnitExpression),
            TypedVariableExpression boundVariableExpression
                => RewriteVariableExpression(boundVariableExpression),
            TypedWhileExpression boundWhileExpression
                => RewriteWhileExpression(boundWhileExpression),
            _ => throw new ArgumentOutOfRangeException(nameof(node), node.GetType().FullName)
        };

    protected virtual TypedExpression RewriteArrayCreationExpression(
        TypedArrayCreationExpression node
    )
    {
        var arraySize = node.ArraySize != null ? RewriteExpression(node.ArraySize) : null;
        List<TypedExpression>? newArguments = null;

        for (var i = 0; i < node.Expressions.Length; i++)
        {
            var argument = node.Expressions[i];
            var newArgument = RewriteExpression(argument);

            if (newArgument != argument)
            {
                if (newArguments == null)
                {
                    // initialize the list with all the statements up to `i`
                    newArguments = new List<TypedExpression>();

                    for (var j = 0; j < i; j++)
                    {
                        newArguments.Add(node.Expressions[j]);
                    }
                }
            }

            newArguments?.Add(newArgument);
        }

        if (newArguments == null && arraySize == node.ArraySize)
            return node;

        return new TypedArrayCreationExpression(
            node.Syntax,
            node.ElementType,
            arraySize,
            newArguments?.ToImmutableArray() ?? node.Expressions
        );
    }

    protected virtual TypedExpression RewriteNullExpression(TypedNullExpression node) => node;

    protected virtual TypedExpression RewriteFieldExpression(TypedFieldExpression node) => node;

    protected virtual TypedExpression RewriteTypeExpression(TypedTypeExpression node) => node;

    protected virtual TypedExpression RewriteNewExpression(TypedNewExpression node)
    {
        List<TypedExpression>? newArguments = null;

        for (var i = 0; i < node.Arguments.Length; i++)
        {
            var argument = node.Arguments[i];
            var newArgument = RewriteExpression(argument);

            if (newArgument != argument)
            {
                if (newArguments == null)
                {
                    // initialize the list with all the statements up to `i`
                    newArguments = new List<TypedExpression>();

                    for (var j = 0; j < i; j++)
                    {
                        newArguments.Add(node.Arguments[j]);
                    }
                }
            }

            newArguments?.Add(newArgument);
        }

        if (newArguments == null)
            return node;

        return new TypedNewExpression(
            node.Syntax,
            node.Constructor,
            newArguments?.ToImmutableArray() ?? node.Arguments
        );
    }

    protected virtual TypedExpression RewritePropertyExpression(TypedPropertyExpression node)
    {
        var expression = RewriteExpression(node.Expression);

        if (node.Expression == expression)
            return node;

        return new TypedPropertyExpression(node.Syntax, expression, node.Property);
    }

    protected virtual TypedExpression RewriteIndexExpression(TypedIndexExpression node)
    {
        var expression = RewriteExpression(node.Expression);
        var index = RewriteExpression(node.Index);

        if (node.Expression == expression && node.Index == index)
            return node;

        return new TypedIndexExpression(node.Syntax, expression, index, node.Getter, node.Setter);
    }

    protected virtual TypedExpression RewriteConversionExpression(TypedConversionExpression node)
    {
        var expr = RewriteExpression(node.Expression);
        if (expr == node.Expression)
            return node;

        return new TypedConversionExpression(node.Syntax, node.Type, expr);
    }

    protected virtual TypedExpression RewriteCallExpression(TypedCallExpression node)
    {
        var lhs = node.Expression != null ? RewriteExpression(node.Expression) : null;

        List<TypedExpression>? newArguments = null;

        for (var i = 0; i < node.Arguments.Length; i++)
        {
            var argument = node.Arguments[i];
            var newArgument = RewriteExpression(argument);

            if (newArgument != argument)
            {
                if (newArguments == null)
                {
                    // initialize the list with all the statements up to `i`
                    newArguments = new List<TypedExpression>();

                    for (var j = 0; j < i; j++)
                    {
                        newArguments.Add(node.Arguments[j]);
                    }
                }
            }

            newArguments?.Add(newArgument);
        }

        if (newArguments == null && node.Expression == lhs)
            return node;

        return new TypedCallExpression(
            node.Syntax,
            node.Method,
            lhs,
            newArguments?.ToImmutableArray() ?? node.Arguments
        );
    }

    protected virtual TypedExpression RewriteWhileExpression(TypedWhileExpression node)
    {
        var cond = RewriteExpression(node.Condition);
        var body = RewriteExpression(node.Body);

        if (node.Condition == cond && node.Body == body)
            return node;

        return new TypedWhileExpression(
            node.Syntax,
            cond,
            body,
            node.BreakLabel,
            node.ContinueLabel
        );
    }

    protected virtual TypedExpression RewriteUnaryExpression(TypedUnaryExpression node)
    {
        var operand = RewriteExpression(node.Operand);
        var @operator = RewriteUnaryOperator(node.Operator);
        if (node.Operand == operand && node.Operator == @operator)
            return node;

        return new TypedUnaryExpression(node.Syntax, @operator, operand);
    }

    protected virtual TypedExpression RewriteIfExpression(TypedIfExpression node)
    {
        var cond = RewriteExpression(node.Condition);
        var then = RewriteExpression(node.Then);
        var @else = RewriteExpression(node.Else);

        if (node.Condition == cond && node.Then == then && node.Else == @else)
            return node;

        return new TypedIfExpression(node.Syntax, cond, then, @else);
    }

    protected virtual TypedExpression RewriteForExpression(TypedForExpression node)
    {
        var lowerTyped = RewriteExpression(node.LowerTyped);
        var upperTyped = RewriteExpression(node.UpperTyped);
        var body = RewriteExpression(node.Body);

        if (node.LowerTyped == lowerTyped && node.UpperTyped == upperTyped && node.Body == body)
            return node;

        return new TypedForExpression(
            node.Syntax,
            node.Variable,
            lowerTyped,
            upperTyped,
            body,
            node.BreakLabel,
            node.ContinueLabel
        );
    }

    protected virtual TypedExpression RewriteBlockExpression(TypedBlockExpression node)
    {
        List<TypedStatement>? statements = null;

        for (var i = 0; i < node.Statements.Length; i++)
        {
            var oldStatement = node.Statements[i];
            var newStatement = RewriteStatement(oldStatement);

            if (newStatement != oldStatement)
            {
                if (statements == null)
                {
                    // initialize the list with all the statements up to `i`
                    statements = new List<TypedStatement>();

                    for (var j = 0; j < i; j++)
                    {
                        statements.Add(node.Statements[j]);
                    }
                }
            }

            if (newStatement != null)
                statements?.Add(newStatement);
        }

        var expression = RewriteExpression(node.Expression);

        if (statements == null && node.Expression == expression)
            return node;

        statements ??= node.Statements.ToList<TypedStatement>();

        return new TypedBlockExpression(node.Syntax, statements.ToImmutableArray(), expression);
    }

    protected virtual TypedExpression RewriteBinaryExpression(TypedBinaryExpression node)
    {
        var left = RewriteExpression(node.Left);
        var right = RewriteExpression(node.Right);
        var @operator = RewriteBinaryOperator(node.Operator);

        if (node.Left == left && node.Right == right && node.Operator == @operator)
            return node;

        return new TypedBinaryExpression(node.Syntax, left, @operator, right);
    }

    protected virtual TypedExpression RewriteAssignmentExpression(TypedAssignmentExpression node)
    {
        var left = RewriteExpression(node.Left);
        var right = RewriteExpression(node.Right);
        if (left == node.Left && right == node.Right)
            return node;

        return new TypedAssignmentExpression(node.Syntax, left, right);
    }

    protected virtual TypedStatement RewriteStatement(TypedStatement node) =>
        node switch
        {
            TypedAssignmentStatement assignmentStatement
                => RewriteAssignmentStatement(assignmentStatement),
            TypedExpressionStatement expressionStatement
                => RewriteExpressionStatement(expressionStatement),
            TypedNopStatement nopStatement => RewriteNopStatement(nopStatement),
            TypedVariableDeclarationStatement variableDeclarationStatement
                => RewriteVariableDeclarationStatement(variableDeclarationStatement),
            TypedLabelStatement labelStatement => RewriteTypedLabelStatement(labelStatement),
            TypedGotoStatement gotoStatement => RewriteTypedGotoStatement(gotoStatement),
            TypedConditionalGotoStatement conditionalGotoStatement
                => RewriteTypedConditionalGotoStatement(conditionalGotoStatement),
            _
                => throw new ArgumentOutOfRangeException(
                    nameof(node),
                    $"Unexpected kind: {node.Kind}"
                )
        };

    protected virtual TypedStatement RewriteAssignmentStatement(TypedAssignmentStatement node)
    {
        var left = RewriteExpression(node.Left);
        var right = RewriteExpression(node.Right);
        if (left == node.Left && right == node.Right)
            return node;

        return new TypedAssignmentStatement(node.Syntax, left, right);
    }

    protected virtual TypedStatement RewriteTypedConditionalGotoStatement(
        TypedConditionalGotoStatement node
    )
    {
        var cond = RewriteExpression(node.Condition);
        if (node.Condition == cond)
            return node;

        return new TypedConditionalGotoStatement(
            node.Syntax,
            node.TypedLabel,
            cond,
            node.JumpIfTrue
        );
    }

    protected virtual TypedStatement RewriteExpressionStatement(TypedExpressionStatement node)
    {
        var expr = RewriteExpression(node.Expression);
        if (expr == node.Expression)
            return node;

        return new TypedExpressionStatement(node.Syntax, expr);
    }

    protected virtual TypedStatement RewriteVariableDeclarationStatement(
        TypedVariableDeclarationStatement node
    )
    {
        var expr = node.Expression == null ? null : RewriteExpression(node.Expression);
        if (expr == node.Expression)
            return node;

        return new TypedVariableDeclarationStatement(node.Syntax, node.Variable, expr);
    }

    protected virtual TypedExpression RewriteErrorExpression(TypedErrorExpression node) => node;

    protected virtual TypedStatement RewriteNopStatement(TypedNopStatement node) => node;

    protected virtual TypedStatement RewriteTypedGotoStatement(TypedGotoStatement node) => node;

    protected virtual TypedStatement RewriteTypedLabelStatement(TypedLabelStatement node) => node;

    protected virtual TypedUnaryOperator RewriteUnaryOperator(TypedUnaryOperator node) => node;

    protected virtual TypedExpression RewriteVariableExpression(TypedVariableExpression node) =>
        node;

    protected virtual TypedExpression RewriteUnitExpression(TypedUnitExpression node) => node;

    protected virtual TypedExpression RewriteLiteralExpression(TypedLiteralExpression node) => node;

    protected virtual TypedBinaryOperator RewriteBinaryOperator(TypedBinaryOperator node) => node;
}
