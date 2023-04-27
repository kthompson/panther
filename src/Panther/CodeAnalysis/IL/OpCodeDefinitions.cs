using System.Collections.Generic;

namespace Panther.CodeAnalysis.IL;

class OpCodeDefinitions
{
    private static OpCodeDefinition MakeDef(string name, params int[] widths) =>
        new OpCodeDefinition(name, widths);

    private const int AddrArg = 2;
    private const int I1Arg = 1;
    private const int I2Arg = 2;
    private const int I4Arg = 4;

    private static Dictionary<OpCode, OpCodeDefinition> All =
        new Dictionary<OpCode, OpCodeDefinition>
        {
            { OpCode.Add, MakeDef("Add") },
            { OpCode.And, MakeDef("And") },
            { OpCode.Br, MakeDef("Br", AddrArg) },
            { OpCode.Brfalse, MakeDef("Brfalse", AddrArg) },
            { OpCode.Brtrue, MakeDef("Brtrue", AddrArg) },
            { OpCode.Call, MakeDef("Call", AddrArg, I1Arg) },
            { OpCode.Ceq, MakeDef("Ceq") },
            { OpCode.Cgt, MakeDef("Cgt") },
            { OpCode.Clt, MakeDef("Clt") },
            { OpCode.Div, MakeDef("Div") },
            { OpCode.Function, MakeDef("Function", I1Arg) },
            { OpCode.Ldarg, MakeDef("Ldarg", I1Arg) },
            { OpCode.Ldc, MakeDef("Ldc", I4Arg) },
            { OpCode.Ldfld, MakeDef("Ldfld", I1Arg) },
            { OpCode.Ldloc, MakeDef("Ldloc", I1Arg) },
            { OpCode.Ldsfld, MakeDef("Ldsfld", AddrArg) },
            { OpCode.Ldstr, MakeDef("Ldstr", AddrArg) },
            { OpCode.Mul, MakeDef("Mul") },
            { OpCode.Neg, MakeDef("Neg") },
            { OpCode.New, MakeDef("New", I1Arg) },
            { OpCode.Nop, MakeDef("Nop") },
            { OpCode.Not, MakeDef("Not") },
            { OpCode.Or, MakeDef("Or") },
            { OpCode.Pop, MakeDef("Pop") },
            { OpCode.Ret, MakeDef("Ret") },
            { OpCode.Starg, MakeDef("Starg", I1Arg) },
            { OpCode.Stfld, MakeDef("Stfld", I1Arg) },
            { OpCode.Stloc, MakeDef("Stloc", I1Arg) },
            { OpCode.Stsfld, MakeDef("Stsfld", AddrArg) },
            { OpCode.Sub, MakeDef("Sub") },
            { OpCode.Xor, MakeDef("Xor") },
        };

    public static OpCodeDefinition? Get(OpCode op) =>
        All.TryGetValue(op, out var value) ? value : null;
}