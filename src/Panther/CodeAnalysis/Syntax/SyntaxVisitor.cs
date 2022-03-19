namespace Panther.CodeAnalysis.Syntax;

public partial class SyntaxVisitor
{
    public virtual void Visit(SyntaxNode node) => node.Accept(this);

    protected virtual void DefaultVisit(SyntaxNode node)
    {
    }
}

public abstract partial class SyntaxVisitor<TResult>
{
    public virtual TResult Visit(SyntaxNode node) => node.Accept(this);

    protected abstract TResult DefaultVisit(SyntaxNode node);
}