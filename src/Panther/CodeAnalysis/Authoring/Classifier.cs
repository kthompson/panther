using System.Collections.Immutable;
using Panther.CodeAnalysis.Syntax;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Authoring
{
    public static class Classifier
    {
        public static ImmutableArray<ClassifiedSpan> Classify(SyntaxTree syntaxTree, TextSpan span)
        {
            var classifiedSpans = ImmutableArray.CreateBuilder<ClassifiedSpan>();
            ClassifyNode(syntaxTree.Root, span, classifiedSpans);

            return classifiedSpans.ToImmutable();
        }

        private static void ClassifyNode(SyntaxNode node, TextSpan span, ImmutableArray<ClassifiedSpan>.Builder builder)
        {
            if (!node.FullSpan.Overlaps(span))
                return;

            if (node is SyntaxToken token)
            {
                ClassifyToken(token, span, builder);
            }

            foreach (var child in node.GetChildren())
            {
                ClassifyNode(child, span, builder);
            }
        }

        private static void ClassifyToken(SyntaxToken token, TextSpan span,
            ImmutableArray<ClassifiedSpan>.Builder builder)
        {
            foreach (var trivia in token.LeadingTrivia)
                ClassifyTrivia(trivia, span, builder);

            AddClassification(token.Kind, token.Span, span, builder);

            foreach (var trivia in token.TrailingTrivia)
                ClassifyTrivia(trivia, span, builder);
        }

        private static void AddClassification(SyntaxKind tokenKind, TextSpan tokenSpan, TextSpan span, ImmutableArray<ClassifiedSpan>.Builder builder)
        {
            var intersection = tokenSpan.Intersection(span);
            if (intersection == null)
                return;

            if (intersection.Value.Length == 0)
                return;

            var classification = GetClassification(tokenKind);
            var classifiedSpan = new ClassifiedSpan(intersection.Value, classification);
            builder.Add(classifiedSpan);
        }

        private static Classification GetClassification(SyntaxKind tokenKind)
        {
            if (tokenKind.IsKeyword())
                return Classification.Keyword;

            if (tokenKind.IsComment())
                return Classification.Comment;

            return tokenKind switch
            {
                SyntaxKind.IdentifierToken => Classification.Identifier,
                SyntaxKind.NumberToken => Classification.Number,
                SyntaxKind.StringToken => Classification.String,
                _ => Classification.Text
            };
        }

        private static void ClassifyTrivia(SyntaxNode trivia, TextSpan span,
            ImmutableArray<ClassifiedSpan>.Builder builder) =>
            AddClassification(trivia.Kind, trivia.Span, span, builder);
    }
}