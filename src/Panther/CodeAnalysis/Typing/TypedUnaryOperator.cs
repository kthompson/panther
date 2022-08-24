using System.Linq;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Typing;

internal sealed class TypedUnaryOperator
{
    public SyntaxKind SyntaxKind { get; }
    public TypedUnaryOperatorKind Kind { get; }
    public Type OperandType { get; }
    public Type Type { get; }

    private TypedUnaryOperator(
        SyntaxKind syntaxKind,
        TypedUnaryOperatorKind kind,
        Type operandType,
        Type type
    )
    {
        SyntaxKind = syntaxKind;
        Kind = kind;
        OperandType = operandType;
        Type = type;
    }

    private TypedUnaryOperator(SyntaxKind syntaxKind, TypedUnaryOperatorKind kind, Type operandType)
        : this(syntaxKind, kind, operandType, operandType) { }

    private static readonly TypedUnaryOperator[] _operators =
    {
        new TypedUnaryOperator(
            SyntaxKind.BangToken,
            TypedUnaryOperatorKind.LogicalNegation,
            Type.Bool
        ),
        new TypedUnaryOperator(SyntaxKind.PlusToken, TypedUnaryOperatorKind.Identity, Type.Int),
        new TypedUnaryOperator(SyntaxKind.DashToken, TypedUnaryOperatorKind.Negation, Type.Int),
        new TypedUnaryOperator(
            SyntaxKind.TildeToken,
            TypedUnaryOperatorKind.BitwiseNegation,
            Type.Int
        ),
    };

    public static TypedUnaryOperator? Bind(SyntaxKind kind, Type operandType) =>
        _operators.FirstOrDefault(op => op.SyntaxKind == kind && op.OperandType == operandType);
}
