namespace Panther.CodeAnalysis.IL;

class OpCodes
{
    public static OpCode? GetOpCodeKind(string value) =>
        value switch
        {
            "add" => OpCode.Add,
            "and" => OpCode.And,
            "br" => OpCode.Br,
            "brfalse" => OpCode.Brfalse,
            "brtrue" => OpCode.Brtrue,
            "call" => OpCode.Call,
            "ceq" => OpCode.Ceq,
            "cgt" => OpCode.Cgt,
            "clt" => OpCode.Clt,
            "div" => OpCode.Div,
            "function" => OpCode.Function,
            "label" => OpCode.Label,
            "ldarg" => OpCode.Ldarg,
            "ldc" => OpCode.Ldc,
            "ldfld" => OpCode.Ldfld,
            "ldloc" => OpCode.Ldloc,
            "ldsfld" => OpCode.Ldsfld,
            "ldstr" => OpCode.Ldstr,
            "mul" => OpCode.Mul,
            "neg" => OpCode.Neg,
            "new" => OpCode.New,
            "nop" => OpCode.Nop,
            "not" => OpCode.Not,
            "or" => OpCode.Or,
            "pop" => OpCode.Pop,
            "ret" => OpCode.Ret,
            "starg" => OpCode.Starg,
            "stfld" => OpCode.Stfld,
            "stloc" => OpCode.Stloc,
            "stsfld" => OpCode.Stsfld,
            "sub" => OpCode.Sub,
            "xor" => OpCode.Xor,
            _ => null
        };
}