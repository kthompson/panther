using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Lowering;

internal class TypedStatementListRewriter : TypedTreeRewriter
{
    protected readonly List<TypedStatement> _statements = new List<TypedStatement>();

    protected TypedStatementListRewriter() { }

    protected TypedBlockExpression GetBlock(SyntaxNode syntax)
    {
        var expr = (_statements.LastOrDefault() as TypedExpressionStatement)?.Expression;
        var stmts = expr == null ? _statements : _statements.Take(_statements.Count - 1);

        expr ??= new TypedUnitExpression(syntax);

        return new TypedBlockExpression(syntax, stmts.ToImmutableArray(), expr);
    }

    protected TypedBlockExpression Rewrite(TypedBlockExpression block)
    {
        foreach (var statement in block.Statements)
            RewriteStatement(statement);

        RewriteStatement(new TypedExpressionStatement(block.Syntax, block.Expression));

        return GetBlock(block.Syntax);
    }

    protected override TypedStatement RewriteStatement(TypedStatement node)
    {
        var statement = base.RewriteStatement(node);

        if (statement is not TypedNopStatement)
            _statements.Add(statement);

        return statement;
    }
}
