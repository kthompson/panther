using System.Collections.Immutable;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding;

sealed record TypedMethodExpression : TypedNode
{
    public TypedMethodExpression(
        SyntaxNode syntax,
        string name,
        TypedExpression? expression,
        ImmutableArray<Symbol> methods
    ) : base(syntax)
    {
        Name = name;
        Expression = expression;
        Methods = methods;
    }

    public string Name { get; }
    public TypedExpression? Expression { get; }
    public ImmutableArray<Symbol> Methods { get; }

    public override TypedNodeKind Kind => TypedNodeKind.MethodExpression;

    public override void Accept(TypedNodeVisitor visitor) => visitor.VisitMethodExpression(this);

    public override TResult Accept<TResult>(TypedNodeVisitor<TResult> visitor) =>
        visitor.VisitMethodExpression(this);
}
