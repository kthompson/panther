using System.Collections.Generic;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.IL;

class AssemblyFacts
{
    private static readonly Dictionary<SyntaxKind, OpCode> OpCodeLookup = new Dictionary<
        SyntaxKind,
        OpCode
    >
    {
        [SyntaxKind.AddKeyword] = OpCode.Add,
        [SyntaxKind.AndKeyword] = OpCode.And,
        [SyntaxKind.BrKeyword] = OpCode.Br,
        [SyntaxKind.BrfalseKeyword] = OpCode.Brfalse,
        [SyntaxKind.BrtrueKeyword] = OpCode.Brtrue,
        [SyntaxKind.CallKeyword] = OpCode.Call,
        [SyntaxKind.CeqKeyword] = OpCode.Ceq,
        [SyntaxKind.CgtKeyword] = OpCode.Cgt,
        [SyntaxKind.CltKeyword] = OpCode.Clt,
        [SyntaxKind.DivKeyword] = OpCode.Div,
        [SyntaxKind.MethodKeyword] = OpCode.Function,
        [SyntaxKind.LabelKeyword] = OpCode.Label,
        [SyntaxKind.LdargKeyword] = OpCode.Ldarg,
        [SyntaxKind.LdcKeyword] = OpCode.Ldc,
        [SyntaxKind.LdfldKeyword] = OpCode.Ldfld,
        [SyntaxKind.LdlocKeyword] = OpCode.Ldloc,
        [SyntaxKind.LdsfldKeyword] = OpCode.Ldsfld,
        [SyntaxKind.LdstrKeyword] = OpCode.Ldstr,
        [SyntaxKind.MulKeyword] = OpCode.Mul,
        [SyntaxKind.NegKeyword] = OpCode.Neg,
        [SyntaxKind.NewKeyword] = OpCode.New,
        [SyntaxKind.NopKeyword] = OpCode.Nop,
        [SyntaxKind.NotKeyword] = OpCode.Not,
        [SyntaxKind.OrKeyword] = OpCode.Or,
        [SyntaxKind.PopKeyword] = OpCode.Pop,
        [SyntaxKind.RetKeyword] = OpCode.Ret,
        [SyntaxKind.StargKeyword] = OpCode.Starg,
        [SyntaxKind.StfldKeyword] = OpCode.Stfld,
        [SyntaxKind.StlocKeyword] = OpCode.Stloc,
        [SyntaxKind.StsfldKeyword] = OpCode.Stsfld,
        [SyntaxKind.SubKeyword] = OpCode.Sub,
        [SyntaxKind.XorKeyword] = OpCode.Xor,
    };

    public static OpCode? GetOpCode(SyntaxKind kind) =>
        OpCodeLookup.TryGetValue(kind, out var opCode) ? opCode : null;

    private static readonly Dictionary<string, SyntaxKind> KindLookup = new Dictionary<
        string,
        SyntaxKind
    >
    {
        [".class"] = SyntaxKind.ClassKeyword,
        [".field"] = SyntaxKind.FieldKeyword,
        [".method"] = SyntaxKind.MethodKeyword,
        [".entrypoint"] = SyntaxKind.EntryPointKeyword,
        [".static"] = SyntaxKind.StaticKeyword,
        ["add"] = SyntaxKind.AddKeyword,
        ["and"] = SyntaxKind.AndKeyword,
        ["br"] = SyntaxKind.BrKeyword,
        ["brfalse"] = SyntaxKind.BrfalseKeyword,
        ["brtrue"] = SyntaxKind.BrtrueKeyword,
        ["call"] = SyntaxKind.CallKeyword,
        ["ceq"] = SyntaxKind.CeqKeyword,
        ["cgt"] = SyntaxKind.CgtKeyword,
        ["clt"] = SyntaxKind.CltKeyword,
        ["div"] = SyntaxKind.DivKeyword,
        ["label"] = SyntaxKind.LabelKeyword,
        ["ldarg"] = SyntaxKind.LdargKeyword,
        ["ldc"] = SyntaxKind.LdcKeyword,
        ["ldfld"] = SyntaxKind.LdfldKeyword,
        ["ldloc"] = SyntaxKind.LdlocKeyword,
        ["ldsfld"] = SyntaxKind.LdsfldKeyword,
        ["ldstr"] = SyntaxKind.LdstrKeyword,
        ["mul"] = SyntaxKind.MulKeyword,
        ["neg"] = SyntaxKind.NegKeyword,
        ["new"] = SyntaxKind.NewKeyword,
        ["nop"] = SyntaxKind.NopKeyword,
        ["not"] = SyntaxKind.NotKeyword,
        ["or"] = SyntaxKind.OrKeyword,
        ["pop"] = SyntaxKind.PopKeyword,
        ["ret"] = SyntaxKind.RetKeyword,
        ["starg"] = SyntaxKind.StargKeyword,
        ["stfld"] = SyntaxKind.StfldKeyword,
        ["stloc"] = SyntaxKind.StlocKeyword,
        ["stsfld"] = SyntaxKind.StsfldKeyword,
        ["sub"] = SyntaxKind.SubKeyword,
        ["xor"] = SyntaxKind.XorKeyword,
    };

    public static SyntaxKind GetKeywordKind(string span) =>
        KindLookup.TryGetValue(span, out var kind) ? kind : SyntaxKind.IdentifierToken;
}
