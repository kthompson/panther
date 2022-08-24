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
        SyntaxKind syntaxKind,
        TypedBinaryOperatorKind kind,
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
        SyntaxKind syntaxKind,
        TypedBinaryOperatorKind kind,
        Type type,
        Type resultType
    ) : this(syntaxKind, kind, type, type, resultType) { }

    private TypedBinaryOperator(SyntaxKind syntaxKind, TypedBinaryOperatorKind kind, Type type)
        : this(syntaxKind, kind, type, type, type) { }

    private static readonly TypedBinaryOperator[] _operators =
    {
        new TypedBinaryOperator(SyntaxKind.PlusToken, TypedBinaryOperatorKind.Addition, Type.Int),
        new TypedBinaryOperator(
            SyntaxKind.PlusToken,
            TypedBinaryOperatorKind.Addition,
            Type.String
        ),
        new TypedBinaryOperator(
            SyntaxKind.DashToken,
            TypedBinaryOperatorKind.Subtraction,
            Type.Int
        ),
        new TypedBinaryOperator(
            SyntaxKind.StarToken,
            TypedBinaryOperatorKind.Multiplication,
            Type.Int
        ),
        new TypedBinaryOperator(SyntaxKind.SlashToken, TypedBinaryOperatorKind.Division, Type.Int),
        new TypedBinaryOperator(
            SyntaxKind.CaretToken,
            TypedBinaryOperatorKind.BitwiseXor,
            Type.Int
        ),
        new TypedBinaryOperator(
            SyntaxKind.AmpersandToken,
            TypedBinaryOperatorKind.BitwiseAnd,
            Type.Int
        ),
        new TypedBinaryOperator(SyntaxKind.PipeToken, TypedBinaryOperatorKind.BitwiseOr, Type.Int),
        new TypedBinaryOperator(
            SyntaxKind.EqualsEqualsToken,
            TypedBinaryOperatorKind.Equal,
            Type.Int,
            Type.Bool
        ),
        new TypedBinaryOperator(
            SyntaxKind.LessThanToken,
            TypedBinaryOperatorKind.LessThan,
            Type.Int,
            Type.Bool
        ),
        new TypedBinaryOperator(
            SyntaxKind.LessThanEqualsToken,
            TypedBinaryOperatorKind.LessThanOrEqual,
            Type.Int,
            Type.Bool
        ),
        new TypedBinaryOperator(
            SyntaxKind.GreaterThanToken,
            TypedBinaryOperatorKind.GreaterThan,
            Type.Int,
            Type.Bool
        ),
        new TypedBinaryOperator(
            SyntaxKind.GreaterThanEqualsToken,
            TypedBinaryOperatorKind.GreaterThanOrEqual,
            Type.Int,
            Type.Bool
        ),
        new TypedBinaryOperator(
            SyntaxKind.BangEqualsToken,
            TypedBinaryOperatorKind.NotEqual,
            Type.Int,
            Type.Bool
        ),
        new TypedBinaryOperator(
            SyntaxKind.AmpersandAmpersandToken,
            TypedBinaryOperatorKind.LogicalAnd,
            Type.Bool
        ),
        new TypedBinaryOperator(
            SyntaxKind.PipePipeToken,
            TypedBinaryOperatorKind.LogicalOr,
            Type.Bool
        ),
        new TypedBinaryOperator(
            SyntaxKind.EqualsEqualsToken,
            TypedBinaryOperatorKind.Equal,
            Type.Bool
        ),
        new TypedBinaryOperator(
            SyntaxKind.BangEqualsToken,
            TypedBinaryOperatorKind.NotEqual,
            Type.Bool
        ),
        new TypedBinaryOperator(
            SyntaxKind.EqualsEqualsToken,
            TypedBinaryOperatorKind.Equal,
            Type.Char,
            Type.Bool
        ),
        new TypedBinaryOperator(
            SyntaxKind.BangEqualsToken,
            TypedBinaryOperatorKind.NotEqual,
            Type.Char,
            Type.Bool
        ),
        new TypedBinaryOperator(
            SyntaxKind.EqualsEqualsToken,
            TypedBinaryOperatorKind.Equal,
            Type.String,
            Type.Bool
        ),
        new TypedBinaryOperator(
            SyntaxKind.BangEqualsToken,
            TypedBinaryOperatorKind.NotEqual,
            Type.String,
            Type.Bool
        ),
    };

    public static TypedBinaryOperator? Bind(SyntaxKind kind, Type leftType, Type rightType) =>
        _operators.FirstOrDefault(
            op => op.SyntaxKind == kind && op.LeftType == leftType && op.RightType == rightType
        );

    public static TypedBinaryOperator BindOrThrow(SyntaxKind kind, Type leftType, Type rightType) =>
        Bind(kind, leftType, rightType) ?? throw new Exception("Binary operator not found");
}
