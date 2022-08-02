namespace Panther.CodeAnalysis.Binding;

internal enum TypedUnaryOperatorKind
{
    Identity,
    Negation, // -
    LogicalNegation, // !
    BitwiseNegation, // ~
}
