namespace Panther.CodeAnalysis.Syntax
{
    public sealed class ParameterSyntax : SyntaxNode
    {
        public SyntaxToken Identifier { get; }
        public TypeAnnotationSyntax TypeAnnotation { get; }

        public ParameterSyntax(SyntaxToken identifier, TypeAnnotationSyntax typeAnnotation)
        {
            Identifier = identifier;
            TypeAnnotation = typeAnnotation;
        }

        public override SyntaxKind Kind => SyntaxKind.Parameter;
    }
}