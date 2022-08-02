using System;
using System.Collections.Immutable;
using System.Linq;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Lowering;

sealed class ThreeAddressCode : TypedStatementListRewriter
{
    private readonly Symbol _method;
    private int _tempCount = 0;

    private ThreeAddressCode(Symbol method)
    {
        _method = method;
    }

    public static TypedBlockExpression Lower(Symbol method, TypedStatement boundStatement)
    {
        var tac = new ThreeAddressCode(method);
        tac.RewriteStatement(boundStatement);
        return tac.GetBlock(boundStatement.Syntax);
    }

    protected override TypedExpression RewriteBlockExpression(TypedBlockExpression node)
    {
        if (node.Statements.Length == 0)
            return RewriteExpression(node.Expression);

        foreach (var boundStatement in node.Statements)
        {
            // has side effect and will be added to the list of statements
            RewriteStatement(boundStatement);
        }

        var rewritten = this.RewriteExpression(node.Expression);
        if (rewritten.Kind == TypedNodeKind.LiteralExpression)
            return rewritten;

        // node.Expression had a nested block so assign it to a temp and return the variable
        return CreateTemporary(rewritten);
    }

    protected override TypedExpression RewriteCallExpression(TypedCallExpression node)
    {
        // since statements can exist inside of a block which is an argument, when we flatten the block statements
        // can get out of order if there are side effects in any of the arguments. In order to prevent this we need
        // to break out the evaluation of each argument and assign to a temporary variable in the correct order.
        // we can then access this temp variable later when we call the function
        var args = node.Arguments
            .Select(RewriteExpression)
            .Select(expr => CreateTemporary(expr, "ctemp"))
            .ToImmutableArray();

        var rewritten = node.Expression == null ? null : this.RewriteExpression(node.Expression);

        return new TypedCallExpression(node.Syntax, node.Method, rewritten, args);
    }

    protected override TypedExpression RewriteBinaryExpression(TypedBinaryExpression node)
    {
        var left = IsSimpleNode(node.Left) ? node.Left : CreateTemporary(node.Left);
        var right = IsSimpleNode(node.Right) ? node.Right : CreateTemporary(node.Right);
        var @operator = RewriteBinaryOperator(node.Operator);

        if (node.Left == left && node.Right == right && node.Operator == @operator)
            return node;

        return new TypedBinaryExpression(node.Syntax, left, @operator, right);
    }

    protected override TypedExpression RewriteUnaryExpression(TypedUnaryExpression node)
    {
        var rewrittenOp = RewriteExpression(node.Operand);
        var operand = IsSimpleNode(rewrittenOp) ? rewrittenOp : CreateTemporary(rewrittenOp);
        var @operator = RewriteUnaryOperator(node.Operator);
        if (node.Operand == operand && node.Operator == @operator)
            return node;

        return new TypedUnaryExpression(node.Syntax, @operator, operand);
    }

    protected override TypedExpression RewriteAssignmentExpression(TypedAssignmentExpression node)
    {
        RewriteStatement(new TypedAssignmentStatement(node.Syntax, node.Left, node.Right));

        return new TypedUnitExpression(node.Syntax);
    }

    protected override TypedExpression RewriteForExpression(TypedForExpression node)
    {
        throw new InvalidProgramException("No `for` expression should exist at this stage");
    }

    protected override TypedExpression RewriteWhileExpression(TypedWhileExpression node)
    {
        throw new InvalidProgramException("No `while` expression should exist at this stage");
    }

    protected override TypedExpression RewriteConversionExpression(TypedConversionExpression node)
    {
        var rewriteExpression = RewriteExpression(node.Expression);
        var expr = IsSimpleNode(rewriteExpression)
            ? rewriteExpression
            : CreateTemporary(rewriteExpression);
        if (expr == node.Expression)
            return node;

        return new TypedConversionExpression(node.Syntax, node.Type, expr);
    }

    private static bool IsSimpleNode(TypedExpression node) =>
        node.Kind == TypedNodeKind.VariableExpression
        || node.Kind == TypedNodeKind.LiteralExpression;

    private TypedExpression CreateTemporary(TypedExpression boundExpression, string prefix = "temp")
    {
        _tempCount++;
        var name = $"{prefix}${_tempCount:0000}";

        var tempVariable = _method
            .NewLocal(TextLocation.None, name, true)
            .WithType(boundExpression.Type)
            .Declare();

        // var tempVariable = new LocalVariableSymbol(name, true, boundExpression.Type, boundExpression.ConstantValue);
        _statements.Add(
            new TypedVariableDeclarationStatement(
                boundExpression.Syntax,
                tempVariable,
                boundExpression
            )
        );
        return new TypedVariableExpression(boundExpression.Syntax, tempVariable);
    }
}
