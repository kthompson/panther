namespace Panther.CodeAnalysis.Typing;

internal enum TypedUnaryOperatorKind
{
    Identity,
    Negation, // -
    LogicalNegation, // !
    BitwiseNegation, // ~
}
