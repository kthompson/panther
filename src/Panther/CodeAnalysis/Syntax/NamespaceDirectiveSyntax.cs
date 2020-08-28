namespace Panther.CodeAnalysis.Syntax
{
    public sealed partial class NamespaceDirectiveSyntax : MemberSyntax
    {
        public SyntaxToken NamespaceKeyword { get; }
        public NameSyntax Name { get; }

        public NamespaceDirectiveSyntax(SyntaxTree syntaxTree, SyntaxToken namespaceKeyword, NameSyntax name)
            : base(syntaxTree)
        {
            NamespaceKeyword = namespaceKeyword;
            Name = name;
        }
    }
}