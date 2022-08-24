namespace Panther.CodeAnalysis.Typing;

partial class TypedNodeVisitor
{
    public virtual void Visit(TypedNode node) => node.Accept(this);

    protected virtual void DefaultVisit(TypedNode node) { }

    public virtual void VisitTypeExpression(TypedTypeExpression node) => DefaultVisit(node);
}

partial class TypedNodeVisitor<TResult>
{
    public virtual TResult Visit(TypedNode node) => node.Accept(this);

    protected virtual TResult DefaultVisit(TypedNode node)
    {
        return default!;
    }

    public virtual TResult VisitTypeExpression(TypedTypeExpression node) => DefaultVisit(node);
}
