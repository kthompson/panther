using System.IO;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Typing;

internal abstract record TypedNode(SyntaxNode Syntax)
{
    public abstract TypedNodeKind Kind { get; }

    public string ToPrintString()
    {
        using var writer = new StringWriter();
        this.WriteTo(writer);
        return writer.ToString();
    }

    public abstract void Accept(TypedNodeVisitor visitor);

    public abstract TResult Accept<TResult>(TypedNodeVisitor<TResult> visitor);
}
