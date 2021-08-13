using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    /// <summary>
    /// Access a local variable
    /// </summary>
    internal record BoundVariableExpression(SyntaxNode Syntax, Symbol Variable)
        : BoundExpression(Syntax)
    {
        public override Type Type { get; init; } = Variable.Type;
        public override BoundNodeKind Kind => BoundNodeKind.VariableExpression;
    }
}