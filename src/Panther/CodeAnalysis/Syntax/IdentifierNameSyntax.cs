namespace Panther.CodeAnalysis.Syntax
{
    public sealed partial class IdentifierNameSyntax : NameSyntax
    {
        public SyntaxToken Identifier { get; }

        public IdentifierNameSyntax(SyntaxTree syntaxTree, SyntaxToken identifier) : base(syntaxTree)
        {
            Identifier = identifier;
        }

        public override string ToText() => Identifier.Text;
    }
}