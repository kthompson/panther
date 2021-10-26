namespace Panther.CodeAnalysis.Syntax
{
    public partial class SyntaxVisitor
    {
        public virtual void Visit(SyntaxNode node) => node.Accept(this);

        protected virtual void DefaultVisit(SyntaxNode node)
        {
        }

        public virtual void VisitTrivia(SyntaxTrivia node) => this.DefaultVisit(node);
        public virtual void VisitToken(SyntaxToken node) => this.DefaultVisit(node);
        public virtual void VisitLiteralExpression(LiteralExpressionSyntax node) => this.DefaultVisit(node);
    }

    public partial class SyntaxVisitor<TResult>
    {
        public virtual TResult Visit(SyntaxNode node) => node.Accept(this);

        protected virtual TResult DefaultVisit(SyntaxNode node)
        {
            return default!;
        }

        public virtual TResult VisitTrivia(SyntaxTrivia node) => this.DefaultVisit(node);
        public virtual TResult VisitToken(SyntaxToken node) => this.DefaultVisit(node);
        public virtual TResult VisitLiteralExpression(LiteralExpressionSyntax node) => this.DefaultVisit(node);
    }
}