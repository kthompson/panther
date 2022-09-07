using System;
using System.Linq;
using Panther.CodeAnalysis.Syntax;
using Type = Panther.CodeAnalysis.Symbols.Type;

namespace Panther.CodeAnalysis.Typing;

internal sealed class TypedBinaryOperator
{
    public SyntaxKind SyntaxKind { get; }
    public TypedBinaryOperatorKind Kind { get; }
    public Type LeftType { get; }
    public Type RightType { get; }
    public Type Type { get; }

    private TypedBinaryOperator(
        TypedBinaryOperatorKind kind,
        SyntaxKind syntaxKind,
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

    private TypedBinaryOperator(
        TypedBinaryOperatorKind kind,
        SyntaxKind syntaxKind,
        Type type,
        Type resultType
    ) : this(kind, syntaxKind, type, type, resultType) { }

    private TypedBinaryOperator(TypedBinaryOperatorKind kind, SyntaxKind syntaxKind, Type type)
        : this(kind, syntaxKind, type, type, type) { }

    private static readonly TypedBinaryOperator[] _operators =
    {
        new TypedBinaryOperator(TypedBinaryOperatorKind.Addition, SyntaxKind.PlusToken, Type.Int),
        new TypedBinaryOperator(
            TypedBinaryOperatorKind.Addition,
            SyntaxKind.PlusToken,
            Type.String
        ),
        new TypedBinaryOperator(
            TypedBinaryOperatorKind.BitwiseAnd,
            SyntaxKind.AmpersandToken,
            Type.Int
        ),
        new TypedBinaryOperator(TypedBinaryOperatorKind.BitwiseOr, SyntaxKind.PipeToken, Type.Int),
        new TypedBinaryOperator(
            TypedBinaryOperatorKind.BitwiseXor,
            SyntaxKind.CaretToken,
            Type.Int
        ),
        new TypedBinaryOperator(TypedBinaryOperatorKind.Division, SyntaxKind.SlashToken, Type.Int),
        new TypedBinaryOperator(
            TypedBinaryOperatorKind.Equal,
            SyntaxKind.EqualsEqualsToken,
            Type.Bool
        ),
        new TypedBinaryOperator(
            TypedBinaryOperatorKind.Equal,
            SyntaxKind.EqualsEqualsToken,
            Type.Char,
            Type.Bool
        ),
        new TypedBinaryOperator(
            TypedBinaryOperatorKind.Equal,
            SyntaxKind.EqualsEqualsToken,
            Type.Int,
            Type.Bool
        ),
        new TypedBinaryOperator(
            TypedBinaryOperatorKind.Equal,
            SyntaxKind.EqualsEqualsToken,
            Type.String,
            Type.Bool
        ),
        new TypedBinaryOperator(
            TypedBinaryOperatorKind.GreaterThan,
            SyntaxKind.GreaterThanToken,
            Type.Char,
            Type.Bool
        ),
        new TypedBinaryOperator(
            TypedBinaryOperatorKind.GreaterThan,
            SyntaxKind.GreaterThanToken,
            Type.Int,
            Type.Bool
        ),
        new TypedBinaryOperator(
            TypedBinaryOperatorKind.GreaterThanOrEqual,
            SyntaxKind.GreaterThanEqualsToken,
            Type.Char,
            Type.Bool
        ),
        new TypedBinaryOperator(
            TypedBinaryOperatorKind.GreaterThanOrEqual,
            SyntaxKind.GreaterThanEqualsToken,
            Type.Int,
            Type.Bool
        ),
        new TypedBinaryOperator(
            TypedBinaryOperatorKind.LessThan,
            SyntaxKind.LessThanToken,
            Type.Char,
            Type.Bool
        ),
        new TypedBinaryOperator(
            TypedBinaryOperatorKind.LessThan,
            SyntaxKind.LessThanToken,
            Type.Int,
            Type.Bool
        ),
        new TypedBinaryOperator(
            TypedBinaryOperatorKind.LessThanOrEqual,
            SyntaxKind.LessThanEqualsToken,
            Type.Char,
            Type.Bool
        ),
        new TypedBinaryOperator(
            TypedBinaryOperatorKind.LessThanOrEqual,
            SyntaxKind.LessThanEqualsToken,
            Type.Int,
            Type.Bool
        ),
        new TypedBinaryOperator(
            TypedBinaryOperatorKind.LogicalAnd,
            SyntaxKind.AmpersandAmpersandToken,
            Type.Bool
        ),
        new TypedBinaryOperator(
            TypedBinaryOperatorKind.LogicalOr,
            SyntaxKind.PipePipeToken,
            Type.Bool
        ),
        new TypedBinaryOperator(
            TypedBinaryOperatorKind.Multiplication,
            SyntaxKind.StarToken,
            Type.Int
        ),
        new TypedBinaryOperator(
            TypedBinaryOperatorKind.NotEqual,
            SyntaxKind.BangEqualsToken,
            Type.Bool
        ),
        new TypedBinaryOperator(
            TypedBinaryOperatorKind.NotEqual,
            SyntaxKind.BangEqualsToken,
            Type.Char,
            Type.Bool
        ),
        new TypedBinaryOperator(
            TypedBinaryOperatorKind.NotEqual,
            SyntaxKind.BangEqualsToken,
            Type.Int,
            Type.Bool
        ),
        new TypedBinaryOperator(
            TypedBinaryOperatorKind.NotEqual,
            SyntaxKind.BangEqualsToken,
            Type.String,
            Type.Bool
        ),
        new TypedBinaryOperator(
            TypedBinaryOperatorKind.Subtraction,
            SyntaxKind.DashToken,
            Type.Int
        ),
    };

    public static TypedBinaryOperator? Bind(SyntaxKind kind, Type leftType, Type rightType) =>
        _operators.FirstOrDefault(
            op => op.SyntaxKind == kind && op.LeftType == leftType && op.RightType == rightType
        );

    public static TypedBinaryOperator BindOrThrow(SyntaxKind kind, Type leftType, Type rightType) =>
        Bind(kind, leftType, rightType) ?? throw new Exception("Binary operator not found");
}
