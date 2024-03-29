﻿namespace Panther.CodeAnalysis.IL;

public enum OpCode : byte
{
    Add,
    And,
    Br,
    Brfalse,
    Brtrue,
    Call,
    Ceq,
    Cgt,
    Clt,
    Div,
    Function,
    Label,
    Ldarg,
    Ldc,
    Ldfld,
    Ldloc,
    Ldsfld,
    Ldstr,
    Mul,
    Neg,
    New,
    Nop,
    Not,
    Or,
    Pop,
    Ret,
    Starg,
    Stfld,
    Stloc,
    Stsfld,
    Sub,
    Xor,
}
