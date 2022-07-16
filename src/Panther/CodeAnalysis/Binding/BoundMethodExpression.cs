using System.Collections.Immutable;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding;

sealed record BoundMethodExpression : BoundNode
{
    public BoundMethodExpression(
        SyntaxNode syntax,
        string name,
        BoundExpression? expression,
        ImmutableArray<Symbol> methods
    ) : base(syntax)
    {
        Name = name;
        Expression = expression;
        Methods = methods;
    }

    public string Name { get; }
    public BoundExpression? Expression { get; }
    public ImmutableArray<Symbol> Methods { get; }

    public override BoundNodeKind Kind => BoundNodeKind.MethodExpression;

    public override void Accept(BoundNodeVisitor visitor) => visitor.VisitMethodExpression(this);

    public override TResult Accept<TResult>(BoundNodeVisitor<TResult> visitor) =>
        visitor.VisitMethodExpression(this);
}
