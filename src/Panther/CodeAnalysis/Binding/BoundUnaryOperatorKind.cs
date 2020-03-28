namespace Panther.CodeAnalysis.Binding
{
    internal enum BoundUnaryOperatorKind
    {
        Identity,
        Negation,        // -
        LogicalNegation, // !
        BitwiseNegation, // ~
    }
}