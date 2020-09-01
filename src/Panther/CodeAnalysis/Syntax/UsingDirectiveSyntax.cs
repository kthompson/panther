namespace Panther.CodeAnalysis.Syntax
{
    public sealed partial class UsingDirectiveSyntax : SyntaxNode
    {
        public SyntaxToken UsingKeyword { get; }
        public SyntaxToken? UsingStyleKeyword { get; } // static or implicit
        public NameSyntax Name { get; }

        public UsingDirectiveSyntax(SyntaxTree syntaxTree, SyntaxToken? usingStyleKeyword, SyntaxToken usingKeyword, NameSyntax name)
            : base(syntaxTree)
        {
            UsingKeyword = usingKeyword;
            Name = name;
            UsingStyleKeyword = usingStyleKeyword;
        }
    }
}