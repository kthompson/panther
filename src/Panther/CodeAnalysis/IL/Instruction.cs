namespace Panther.CodeAnalysis.IL;

public class Instruction
{
    public Instruction(int Offset, OpCode Code, object? Operand1, object? Operand2)
    {
        this.Offset = Offset;
        this.Code = Code;
        this.Operand1 = Operand1;
        this.Operand2 = Operand2;
    }

    public int Offset { get; init; }
    public OpCode Code { get; init; }
    public object? Operand1 { get; set; }
    public object? Operand2 { get; set; }

    public void Deconstruct(
        out int Offset,
        out OpCode Code,
        out object? Operand1,
        out object? Operand2
    )
    {
        Offset = this.Offset;
        Code = this.Code;
        Operand1 = this.Operand1;
        Operand2 = this.Operand2;
    }

    public override string ToString()
    {
        var op1 = Operand1 == null ? "" : $" {Operand1}";
        var op2 = Operand2 == null ? "" : $" {Operand2}";
        return $"IL_{Offset:X4}: {Code}{op1}{op2}";
    }
}
