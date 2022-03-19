namespace Panther.CodeAnalysis.Binding;

partial class BoundNodeVisitor
{
    public virtual void Visit(BoundNode node) => node.Accept(this);

    protected virtual void DefaultVisit(BoundNode node)
    {
    }

    public virtual void VisitTypeExpression(BoundTypeExpression node) => DefaultVisit(node);
}
partial class BoundNodeVisitor<TResult>
{
    public virtual TResult Visit(BoundNode node) => node.Accept(this);

    protected virtual TResult DefaultVisit(BoundNode node)
    {
        return default!;
    }

    public virtual TResult VisitTypeExpression(BoundTypeExpression node) => DefaultVisit(node);
}