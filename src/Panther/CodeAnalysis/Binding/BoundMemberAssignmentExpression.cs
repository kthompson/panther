using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    internal class BoundMemberAssignmentExpression : BoundExpression
    {
        public FieldSymbol Field { get; }
        public BoundExpression Expression { get; }
        public override BoundNodeKind Kind => BoundNodeKind.MemberAssignmentExpression;

        public override TypeSymbol Type => TypeSymbol.Unit;

        public BoundMemberAssignmentExpression(SyntaxNode syntax, FieldSymbol field, BoundExpression expression)
            : base(syntax)
        {
            Field = field;
            Expression = expression;
        }
    }
}