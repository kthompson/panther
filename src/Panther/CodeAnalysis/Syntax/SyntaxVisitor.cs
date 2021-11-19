namespace Panther.CodeAnalysis.Syntax
{
    public partial class SyntaxVisitor
    {
        public virtual void Visit(SyntaxNode node) => node.Accept(this);

        protected virtual void DefaultVisit(SyntaxNode node)
        {
        }
    }

    public partial class SyntaxVisitor<TResult>
    {
        public virtual TResult Visit(SyntaxNode node) => node.Accept(this);

        protected virtual TResult DefaultVisit(SyntaxNode node)
        {
            return default!;
        }
    }
}