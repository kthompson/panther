namespace Panther.CodeAnalysis.Syntax
{
    public sealed class FunctionDeclarationSyntax : MemberSyntax
    {
        public SyntaxToken DefKeyword { get; }
        public SyntaxToken Identifier { get; }
        public SyntaxToken OpenParenToken { get; }
        public SeparatedSyntaxList<ParameterSyntax> Parameters { get; }
        public SyntaxToken CloseParenToken { get; }
        public TypeAnnotationSyntax? TypeAnnotation { get; }
        public SyntaxToken EqualsToken { get; }
        public ExpressionSyntax Body { get; }

        public FunctionDeclarationSyntax(SyntaxToken defKeyword, SyntaxToken identifier, SyntaxToken openParenToken,
            SeparatedSyntaxList<ParameterSyntax> parameters, SyntaxToken closeParenToken,
            TypeAnnotationSyntax? typeAnnotation, SyntaxToken equalsToken, ExpressionSyntax body)
        {
            DefKeyword = defKeyword;
            Identifier = identifier;
            OpenParenToken = openParenToken;
            Parameters = parameters;
            CloseParenToken = closeParenToken;
            TypeAnnotation = typeAnnotation;
            EqualsToken = equalsToken;
            Body = body;
        }

        public override SyntaxKind Kind => SyntaxKind.FunctionDeclaration;
    }
}