using System.Collections.Immutable;

namespace Panther.CodeAnalysis.IL;

public record AssemblerNode;

public abstract record Instruction: AssemblerNode;

public record NoOperandInstruction: Instruction;

public record StringOperandInstruction(OpCode OpCode, string Operand): Instruction;
public record LabelOperandInstruction(OpCode OpCode, string Label): Instruction;
public record IntOperandInstruction(OpCode OpCode, int Operand): Instruction;
public record FunctionInstruction(OpCode OpCode, string Label, int LocalCount): Instruction;
public record CallInstruction(OpCode OpCode, string Label, int ArgumentCount): Instruction;


public record AssemblerListing(ImmutableArray<Instruction> Instructions);