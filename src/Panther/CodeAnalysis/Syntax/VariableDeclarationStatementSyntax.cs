using System.Collections.Generic;

namespace Panther.CodeAnalysis.Syntax
{
    public sealed partial class VariableDeclarationStatementSyntax : StatementSyntax
    {
        public SyntaxToken ValOrVarToken { get; }
        public SyntaxToken IdentifierToken { get; }
        public TypeAnnotationSyntax? TypeAnnotation { get; }
        public SyntaxToken EqualsToken { get; }
        public ExpressionSyntax Expression { get; }

        public VariableDeclarationStatementSyntax(SyntaxTree syntaxTree, SyntaxToken valOrVarToken,
            SyntaxToken identifierToken, TypeAnnotationSyntax? typeAnnotation, SyntaxToken equalsToken,
            ExpressionSyntax expression) : base(syntaxTree)
        {
            ValOrVarToken = valOrVarToken;
            IdentifierToken = identifierToken;
            TypeAnnotation = typeAnnotation;
            EqualsToken = equalsToken;
            Expression = expression;
        }
    }
}