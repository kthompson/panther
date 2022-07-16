using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Authoring;

public sealed class ClassifiedSpan
{
    public TextSpan Span { get; }
    public Classification Classification { get; }

    public ClassifiedSpan(TextSpan span, Classification classification)
    {
        Span = span;
        Classification = classification;
    }
}
