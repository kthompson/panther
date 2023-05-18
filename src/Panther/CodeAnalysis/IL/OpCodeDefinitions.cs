using System.Collections.Generic;

namespace Panther.CodeAnalysis.IL;

class OpCodeDefinitions
{
    private static OpCodeDefinition MakeDef(string name, params int[] widths) =>
        new OpCodeDefinition(name, widths);

    private const int AddrArg = 2;
    private const int U8 = 1;
    private const int U16 = 2;
    private const int U32 = 4;

    private static Dictionary<OpCode, OpCodeDefinition> All = new Dictionary<
        OpCode,
        OpCodeDefinition
    >
    {
        { OpCode.Add, MakeDef("add") },
        { OpCode.And, MakeDef("and") },
        { OpCode.Br, MakeDef("br", AddrArg) },
        { OpCode.Brfalse, MakeDef("brfalse", AddrArg) },
        { OpCode.Brtrue, MakeDef("brtrue", AddrArg) },
        { OpCode.Call, MakeDef("call", AddrArg, U8) },
        { OpCode.Ceq, MakeDef("ceq") },
        { OpCode.Cgt, MakeDef("cgt") },
        { OpCode.Clt, MakeDef("clt") },
        { OpCode.Div, MakeDef("div") },
        // { OpCode.Function, MakeDef("function", AddrArg, U8) },
        { OpCode.Ldarg, MakeDef("ldarg", U8) },
        { OpCode.Ldc, MakeDef("ldc", U32) },
        { OpCode.Ldfld, MakeDef("ldfld", U8) },
        { OpCode.Ldloc, MakeDef("ldloc", U8) },
        { OpCode.Ldsfld, MakeDef("ldsfld", AddrArg) },
        { OpCode.Ldstr, MakeDef("ldstr", AddrArg) },
        { OpCode.Mul, MakeDef("mul") },
        { OpCode.Neg, MakeDef("neg") },
        { OpCode.New, MakeDef("new", U8) },
        { OpCode.Nop, MakeDef("nop") },
        { OpCode.Not, MakeDef("not") },
        { OpCode.Or, MakeDef("or") },
        { OpCode.Pop, MakeDef("pop") },
        { OpCode.Ret, MakeDef("ret") },
        { OpCode.Starg, MakeDef("starg", U8) },
        { OpCode.Stfld, MakeDef("stfld", U8) },
        { OpCode.Stloc, MakeDef("stloc", U8) },
        { OpCode.Stsfld, MakeDef("stsfld", AddrArg) },
        { OpCode.Sub, MakeDef("sub") },
        { OpCode.Xor, MakeDef("xor") },
    };

    public static OpCodeDefinition? Get(OpCode op) =>
        All.TryGetValue(op, out var value) ? value : null;
}
