using System.CodeDom.Compiler;
using Panther.IO;

namespace Panther.CodeAnalysis.IL;

internal static class InstructionPrinter
{
    public static void WriteTo(this Instruction node, IndentedTextWriter writer)
    {
        writer.Write($"IL_{node.Offset:X4}: ");
        writer.WriteKeyword(node.Code.ToString().ToLowerInvariant());
        WriteOperand(node.Operand1);
        WriteOperand(node.Operand2);
        writer.WriteLine();

        void WriteOperand(object? operand)
        {
            if (operand == null)
                return;

            writer.Write(" ");
            switch (operand)
            {
                case string s:
                    writer.WriteString(s);
                    break;
                case bool b:
                    writer.WriteNumber(b.ToString().ToLowerInvariant());
                    break;
                case int i:
                    writer.WriteNumber(i.ToString().ToLowerInvariant());
                    break;
                default:
                    writer.Write(operand.ToString());
                    break;
            }
        }
    }
}