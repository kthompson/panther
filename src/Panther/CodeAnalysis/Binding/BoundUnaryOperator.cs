using System.Linq;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding;

internal sealed class BoundUnaryOperator
{
    public SyntaxKind SyntaxKind { get; }
    public BoundUnaryOperatorKind Kind { get; }
    public Type OperandType { get; }
    public Type Type { get; }

    private BoundUnaryOperator(SyntaxKind syntaxKind, BoundUnaryOperatorKind kind, Type operandType, Type type)
    {
        SyntaxKind = syntaxKind;
        Kind = kind;
        OperandType = operandType;
        Type = type;
    }

    private BoundUnaryOperator(SyntaxKind syntaxKind, BoundUnaryOperatorKind kind, Type operandType)
        : this(syntaxKind, kind, operandType, operandType)
    {
    }

    private static readonly BoundUnaryOperator[] _operators =
    {
        new BoundUnaryOperator(SyntaxKind.BangToken, BoundUnaryOperatorKind.LogicalNegation, Type.Bool),
        new BoundUnaryOperator(SyntaxKind.PlusToken, BoundUnaryOperatorKind.Identity, Type.Int),
        new BoundUnaryOperator(SyntaxKind.DashToken, BoundUnaryOperatorKind.Negation, Type.Int),
        new BoundUnaryOperator(SyntaxKind.TildeToken, BoundUnaryOperatorKind.BitwiseNegation, Type.Int),
    };

    public static BoundUnaryOperator? Bind(SyntaxKind kind, Type operandType) =>
        _operators.FirstOrDefault(op => op.SyntaxKind == kind && op.OperandType == operandType);
}