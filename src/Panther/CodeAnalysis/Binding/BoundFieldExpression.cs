using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    sealed class BoundFieldExpression : BoundExpression
    {
        public BoundFieldExpression(SyntaxNode syntax, string name, FieldSymbol field)
            : base(syntax)
        {
            Name = name;
            Field = field;
        }

        public string Name { get; }
        public FieldSymbol Field { get; }

        public override BoundNodeKind Kind => BoundNodeKind.FieldExpression;
        public override TypeSymbol Type => Field.Type;
    }
}