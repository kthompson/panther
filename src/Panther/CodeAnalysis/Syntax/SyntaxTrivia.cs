namespace Panther.CodeAnalysis.Syntax
{
    public sealed class SyntaxTrivia : SyntaxNode
    {
        public SyntaxTrivia(SyntaxTree syntaxTree, SyntaxKind kind, string text)
            : base(syntaxTree)
        {
            Kind = kind;
            Text = text;
        }

        public override SyntaxKind Kind { get; }
        public string Text { get; }
    }
}