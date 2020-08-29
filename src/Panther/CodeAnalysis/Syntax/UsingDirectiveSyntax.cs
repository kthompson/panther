namespace Panther.CodeAnalysis.Syntax
{
    public sealed partial class UsingDirectiveSyntax : SyntaxNode
    {
        public SyntaxToken UsingKeyword { get; }
        public NameSyntax Name { get; }

        public UsingDirectiveSyntax(SyntaxTree syntaxTree, SyntaxToken usingKeyword, NameSyntax name)
            : base(syntaxTree)
        {
            UsingKeyword = usingKeyword;
            Name = name;
        }
    }
}