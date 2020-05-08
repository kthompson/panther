namespace Panther.CodeAnalysis.Syntax
{
    public sealed partial class FunctionDeclarationSyntax : MemberSyntax
    {
        public SyntaxToken DefKeyword { get; }
        public SyntaxToken Identifier { get; }
        public SyntaxToken OpenParenToken { get; }
        public SeparatedSyntaxList<ParameterSyntax> Parameters { get; }
        public SyntaxToken CloseParenToken { get; }
        public TypeAnnotationSyntax? TypeAnnotation { get; }
        public SyntaxToken EqualsToken { get; }
        public ExpressionSyntax Body { get; }

        public FunctionDeclarationSyntax(SyntaxTree syntaxTree, SyntaxToken defKeyword, SyntaxToken identifier, SyntaxToken openParenToken,
            SeparatedSyntaxList<ParameterSyntax> parameters, SyntaxToken closeParenToken,
            TypeAnnotationSyntax? typeAnnotation, SyntaxToken equalsToken, ExpressionSyntax body) : base(syntaxTree)
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
    }
}