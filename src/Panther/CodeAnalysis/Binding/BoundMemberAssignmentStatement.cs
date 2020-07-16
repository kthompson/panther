using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    internal class BoundMemberAssignmentStatement : BoundExpression
    {
        public FieldSymbol Field { get; }
        public BoundExpression Expression { get; }
        public override BoundNodeKind Kind => BoundNodeKind.MemberAssignmentStatement;

        public override TypeSymbol Type => TypeSymbol.Unit;

        public BoundMemberAssignmentStatement(SyntaxNode syntax, FieldSymbol field, BoundExpression expression)
            : base(syntax)
        {
            Field = field;
            Expression = expression;
        }
    }
}