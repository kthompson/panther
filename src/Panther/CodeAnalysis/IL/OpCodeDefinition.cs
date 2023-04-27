using System.Collections.Generic;
using System.Linq;

namespace Panther.CodeAnalysis.IL;

record OpCodeDefinition(string Name, IReadOnlyList<int> OperandWidths)
{
    public int OperandsWidth => OperandWidths.Sum();
    public int TotalWidth => OperandsWidth + 1;

    // public (object operands, int offset) ReadOperands(IList<byte> instructions, int position)
    // {
    //
    // }
}