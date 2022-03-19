using System.Collections.Generic;
using System.Linq;
using Panther.CodeAnalysis.Binding;

namespace Panther.CodeAnalysis.Lowering;

internal static class DeadCodeRemoval
{
    public static BoundBlockExpression RemoveDeadCode(BoundBlockExpression block)
    {
        var controlFlow = ControlFlowGraph.Create(block);
        var reachableStatements = new HashSet<BoundStatement>(controlFlow.Blocks.SelectMany(basicBlock => basicBlock.Statements));

        var builder = block.Statements.ToBuilder();
        for (int i = builder.Count - 1; i >= 0; i--)
        {
            if (!reachableStatements.Contains(builder[i]))
                builder.RemoveAt(i);
        }

        return new BoundBlockExpression(block.Syntax, builder.ToImmutable(), block.Expression);
    }
}