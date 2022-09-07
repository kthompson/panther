using System;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Typing;

namespace Panther.CodeAnalysis.Lowering;

internal sealed class LoweringPipeline
{
    public static TypedBlockExpression Lower(Symbol method, TypedStatement statement)
    {
        var debug = false;
        if (debug)
        {
            Console.WriteLine("==== Original Code ===");
            method.WriteTo(Console.Out);
            Console.Out.Write(" = ");
            statement.WriteTo(Console.Out);
            Console.Out.WriteLine();
        }

        var boundStatement = LoopLowerer.Lower(method, statement);
        if (debug)
        {
            Console.WriteLine("==== Lowered Code ===");
            method.WriteTo(Console.Out);
            Console.Out.Write(" = ");
            boundStatement.WriteTo(Console.Out);
            Console.Out.WriteLine();
        }

        var noIndexExpr = IndexExpressions.Lower(method, boundStatement);
        if (debug)
        {
            Console.WriteLine("==== Index Expressions ===");
            method.WriteTo(Console.Out);
            Console.Out.Write(" = ");
            noIndexExpr.WriteTo(Console.Out);
            Console.Out.WriteLine();
        }

        var tac = ThreeAddressCode.Lower(method, noIndexExpr);
        if (debug)
        {
            Console.WriteLine("==== Three Address Code ===");
            method.WriteTo(Console.Out);
            Console.Out.Write(" = ");
            tac.WriteTo(Console.Out);
            Console.Out.WriteLine();
        }

        var unitLessStatements = RemoveUnitAssignments.Lower(tac);
        if (debug)
        {
            Console.WriteLine("==== Remove Unit Assignments ===");
            method.WriteTo(Console.Out);
            Console.Out.Write(" = ");
            unitLessStatements.WriteTo(Console.Out);
            Console.Out.WriteLine();
        }

        var inlinedTemporaries = InlineTemporaries.Lower(method, unitLessStatements);
        if (debug)
        {
            Console.WriteLine("==== Inlined Temporaries ===");
            method.WriteTo(Console.Out);
            Console.Out.Write(" = ");
            inlinedTemporaries.WriteTo(Console.Out);
            Console.Out.WriteLine();
        }

        var deadCodeRemoval = DeadCodeRemoval.RemoveDeadCode(inlinedTemporaries);
        if (debug)
        {
            Console.WriteLine("==== Dead Code Removal ===");
            method.WriteTo(Console.Out);
            Console.Out.Write(" = ");
            deadCodeRemoval.WriteTo(Console.Out);
            Console.Out.WriteLine();
        }

        return deadCodeRemoval;
    }
}
