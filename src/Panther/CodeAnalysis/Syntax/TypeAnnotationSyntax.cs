namespace Panther.CodeAnalysis.Syntax
{
    public sealed class TypeAnnotationSyntax : SyntaxNode
    {
        public SyntaxToken ColonToken { get; }
        public SyntaxToken IdentifierToken { get; }

        public TypeAnnotationSyntax(SyntaxToken colonToken, SyntaxToken identifierToken)
        {
            ColonToken = colonToken;
            IdentifierToken = identifierToken;
        }

        public override SyntaxKind Kind => SyntaxKind.TypeClause;
    }
}