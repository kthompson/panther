using System;
using System.Collections.Generic;
using System.Linq;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Lowering;

internal sealed class InlineTemporaries : TypedStatementListRewriter
{
    private readonly Symbol _method;
    private readonly Dictionary<Symbol, TypedExpression> _expressionsToInline =
        new Dictionary<Symbol, TypedExpression>();

    private InlineTemporaries(Symbol method)
    {
        _method = method;
    }

    protected override TypedStatement RewriteStatement(TypedStatement node)
    {
        if (
            node is TypedVariableDeclarationStatement { Expression: { } } varDecl
            && (
                varDecl.Variable.Name.StartsWith("temp$")
            //|| varDecl.Variable.Name.StartsWith("ctemp$")
            )
        )
        {
            varDecl.Variable.Delete();
            _expressionsToInline[varDecl.Variable] = varDecl.Expression;
            return new TypedNopStatement(node.Syntax);
        }

        return base.RewriteStatement(node);
    }

    protected override TypedExpression RewriteExpression(TypedExpression node)
    {
        if (
            node is TypedVariableExpression variableExpression
            && _expressionsToInline.TryGetValue(variableExpression.Variable, out var expression)
        )
        {
            _expressionsToInline.Remove(variableExpression.Variable);
            return RewriteExpression(expression);
        }

        return base.RewriteExpression(node);
    }

    protected override TypedExpression RewriteBlockExpression(TypedBlockExpression node)
    {
        if (node.Statements.IsEmpty) { }
        return base.RewriteBlockExpression(node);
    }

    public static TypedBlockExpression Lower(Symbol method, TypedBlockExpression blockExpression) =>
        new InlineTemporaries(method).Rewrite(blockExpression);
}
