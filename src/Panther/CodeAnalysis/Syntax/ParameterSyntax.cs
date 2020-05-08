namespace Panther.CodeAnalysis.Syntax
{
    public sealed partial class ParameterSyntax : SyntaxNode
    {
        public SyntaxToken Identifier { get; }
        public TypeAnnotationSyntax TypeAnnotation { get; }

        public ParameterSyntax(SyntaxTree syntaxTree, SyntaxToken identifier, TypeAnnotationSyntax typeAnnotation) : base(syntaxTree)
        {
            Identifier = identifier;
            TypeAnnotation = typeAnnotation;
        }
    }
}