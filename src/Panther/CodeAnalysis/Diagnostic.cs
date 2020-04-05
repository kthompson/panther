using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis
{
    public sealed class Diagnostic
    {
        public TextSpan Span => Location.Span;
        public TextLocation Location { get; }
        public string Message { get; }

        public Diagnostic(TextLocation location, string message)
        {
            Location = location;
            Message = message;
        }

        public override string ToString() => Message;
    }
}