namespace Panther.CodeAnalysis.Syntax
{
    public sealed partial class TypeAnnotationSyntax : SyntaxNode
    {
        public SyntaxToken ColonToken { get; }
        public SyntaxToken IdentifierToken { get; }

        public TypeAnnotationSyntax(SyntaxTree syntaxTree, SyntaxToken colonToken, SyntaxToken identifierToken) : base(syntaxTree)
        {
            ColonToken = colonToken;
            IdentifierToken = identifierToken;
        }
    }
}