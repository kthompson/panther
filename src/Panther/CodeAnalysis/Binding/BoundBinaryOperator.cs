using System;
using System.Linq;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;
using Type = Panther.CodeAnalysis.Symbols.Type;

namespace Panther.CodeAnalysis.Binding;

internal sealed class BoundBinaryOperator
{
    public SyntaxKind SyntaxKind { get; }
    public BoundBinaryOperatorKind Kind { get; }
    public Type LeftType { get; }
    public Type RightType { get; }
    public Type Type { get; }

    private BoundBinaryOperator(
        SyntaxKind syntaxKind,
        BoundBinaryOperatorKind kind,
        Type leftType,
        Type rightType,
        Type type
    )
    {
        SyntaxKind = syntaxKind;
        Kind = kind;
        LeftType = leftType;
        RightType = rightType;
        Type = type;
    }

    private BoundBinaryOperator(
        SyntaxKind syntaxKind,
        BoundBinaryOperatorKind kind,
        Type type,
        Type resultType
    ) : this(syntaxKind, kind, type, type, resultType) { }

    private BoundBinaryOperator(SyntaxKind syntaxKind, BoundBinaryOperatorKind kind, Type type)
        : this(syntaxKind, kind, type, type, type) { }

    private static readonly BoundBinaryOperator[] _operators =
    {
        new BoundBinaryOperator(SyntaxKind.PlusToken, BoundBinaryOperatorKind.Addition, Type.Int),
        new BoundBinaryOperator(
            SyntaxKind.PlusToken,
            BoundBinaryOperatorKind.Addition,
            Type.String
        ),
        new BoundBinaryOperator(
            SyntaxKind.DashToken,
            BoundBinaryOperatorKind.Subtraction,
            Type.Int
        ),
        new BoundBinaryOperator(
            SyntaxKind.StarToken,
            BoundBinaryOperatorKind.Multiplication,
            Type.Int
        ),
        new BoundBinaryOperator(SyntaxKind.SlashToken, BoundBinaryOperatorKind.Division, Type.Int),
        new BoundBinaryOperator(
            SyntaxKind.CaretToken,
            BoundBinaryOperatorKind.BitwiseXor,
            Type.Int
        ),
        new BoundBinaryOperator(
            SyntaxKind.AmpersandToken,
            BoundBinaryOperatorKind.BitwiseAnd,
            Type.Int
        ),
        new BoundBinaryOperator(SyntaxKind.PipeToken, BoundBinaryOperatorKind.BitwiseOr, Type.Int),
        new BoundBinaryOperator(
            SyntaxKind.EqualsEqualsToken,
            BoundBinaryOperatorKind.Equal,
            Type.Int,
            Type.Bool
        ),
        new BoundBinaryOperator(
            SyntaxKind.LessThanToken,
            BoundBinaryOperatorKind.LessThan,
            Type.Int,
            Type.Bool
        ),
        new BoundBinaryOperator(
            SyntaxKind.LessThanEqualsToken,
            BoundBinaryOperatorKind.LessThanOrEqual,
            Type.Int,
            Type.Bool
        ),
        new BoundBinaryOperator(
            SyntaxKind.GreaterThanToken,
            BoundBinaryOperatorKind.GreaterThan,
            Type.Int,
            Type.Bool
        ),
        new BoundBinaryOperator(
            SyntaxKind.GreaterThanEqualsToken,
            BoundBinaryOperatorKind.GreaterThanOrEqual,
            Type.Int,
            Type.Bool
        ),
        new BoundBinaryOperator(
            SyntaxKind.BangEqualsToken,
            BoundBinaryOperatorKind.NotEqual,
            Type.Int,
            Type.Bool
        ),
        new BoundBinaryOperator(
            SyntaxKind.AmpersandAmpersandToken,
            BoundBinaryOperatorKind.LogicalAnd,
            Type.Bool
        ),
        new BoundBinaryOperator(
            SyntaxKind.PipePipeToken,
            BoundBinaryOperatorKind.LogicalOr,
            Type.Bool
        ),
        new BoundBinaryOperator(
            SyntaxKind.EqualsEqualsToken,
            BoundBinaryOperatorKind.Equal,
            Type.Bool
        ),
        new BoundBinaryOperator(
            SyntaxKind.BangEqualsToken,
            BoundBinaryOperatorKind.NotEqual,
            Type.Bool
        ),
        new BoundBinaryOperator(
            SyntaxKind.EqualsEqualsToken,
            BoundBinaryOperatorKind.Equal,
            Type.String,
            Type.Bool
        ),
        new BoundBinaryOperator(
            SyntaxKind.BangEqualsToken,
            BoundBinaryOperatorKind.NotEqual,
            Type.String,
            Type.Bool
        ),
    };

    public static BoundBinaryOperator? Bind(SyntaxKind kind, Type leftType, Type rightType) =>
        _operators.FirstOrDefault(
            op => op.SyntaxKind == kind && op.LeftType == leftType && op.RightType == rightType
        );

    public static BoundBinaryOperator BindOrThrow(SyntaxKind kind, Type leftType, Type rightType) =>
        Bind(kind, leftType, rightType) ?? throw new Exception("Binary operator not found");
}
