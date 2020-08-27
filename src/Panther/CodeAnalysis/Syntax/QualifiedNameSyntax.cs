namespace Panther.CodeAnalysis.Syntax
{
    public sealed partial class QualifiedNameSyntax : NameSyntax
    {
        public NameSyntax Left { get; }
        public SyntaxToken DotToken { get; }
        public IdentifierNameSyntax Right { get; }

        public QualifiedNameSyntax(SyntaxTree syntaxTree, NameSyntax left, SyntaxToken dotToken, IdentifierNameSyntax right)
            : base(syntaxTree)
        {
            Left = left;
            DotToken = dotToken;
            Right = right;
        }

        public override string ToText() => $"{Left.ToText()}.{Right.ToText()}";
    }
}