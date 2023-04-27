using System;
using System.Collections.Generic;
using System.IO;
using Panther.IO;

namespace Panther.CodeAnalysis.IL;

public class Instructions
{
    public static string Show(IList<byte> instructions)
    {
        var writer = new StringWriter();

        Print(instructions, writer);


        return writer.ToString();
    }

    private static void Print(IList<byte> instructions, StringWriter writer)
    {
        int pos = 0;
        while (pos < instructions.Count)
        {
            pos = PrintOne(instructions, pos, writer);
        }
    }

    private static int PrintOne(IList<byte> instructions, int pos, StringWriter writer)
    {
        var def = OpCodeDefinitions.Get((OpCode)instructions[pos]);
        if (def == null)
        {
            writer.WriteError($"Error: opcode not found");
            return pos + 1;
        }

        // var (_, offset) = def.ReadOperands(instructions, pos + 1);
        // writer.WritePunctuation(pos.ToString("x4"));
        // // TODO def.Print(operands, writer);
        // return offset;

        throw new NotImplementedException();
    }
}