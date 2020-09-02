using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    /// <summary>
    /// Access a type in scope
    /// </summary>
    internal class BoundTypeExpression : BoundExpression
    {
        public override TypeSymbol Type { get; }
        public override BoundNodeKind Kind => BoundNodeKind.TypeExpression;

        public BoundTypeExpression(SyntaxNode syntax, TypeSymbol type)
            : base(syntax)
        {
            Type = type;
        }
    }
}