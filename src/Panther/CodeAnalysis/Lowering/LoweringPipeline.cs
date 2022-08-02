using System;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Lowering;

internal sealed class LoweringPipeline
{
    public static TypedBlockExpression Lower(Symbol method, TypedStatement statement)
    {
        var debug = false;
        if (debug)
        {
            Console.WriteLine("==== Original Code ===");
            statement.WriteTo(Console.Out);
        }

        var boundStatement = LoopLowerer.Lower(method, statement);
        if (debug)
        {
            Console.WriteLine("==== Lowered Code ===");
            boundStatement.WriteTo(Console.Out);
        }

        var noIndexExpr = IndexExpressions.Lower(method, boundStatement);
        if (debug)
        {
            Console.WriteLine("==== Index Expressions ===");
            noIndexExpr.WriteTo(Console.Out);
        }

        var tac = ThreeAddressCode.Lower(method, noIndexExpr);
        if (debug)
        {
            Console.WriteLine("==== Three Address Code ===");
            tac.WriteTo(Console.Out);
        }

        var unitLessStatements = RemoveUnitAssignments.Lower(tac);
        if (debug)
        {
            Console.WriteLine("==== Remove Unit Assignments ===");
            unitLessStatements.WriteTo(Console.Out);
        }

        var inlinedTemporaries = InlineTemporaries.Lower(method, unitLessStatements);
        if (debug)
        {
            Console.WriteLine("==== Inlined Temporaries ===");
            inlinedTemporaries.WriteTo(Console.Out);
        }

        var deadCodeRemoval = DeadCodeRemoval.RemoveDeadCode(inlinedTemporaries);
        if (debug)
        {
            Console.WriteLine("==== Dead Code Removal ===");
            deadCodeRemoval.WriteTo(Console.Out);
        }

        return deadCodeRemoval;
    }
}
