using System.Collections.Generic;
using System.Linq;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Syntax
{
    public class SyntaxTree
    {
        public IReadOnlyList<Diagnostic> Diagnostics { get; }
        public SourceText Text { get; }
        public ExpressionSyntax Root { get; }
        public SyntaxToken EndOfInputToken { get; }

        public SyntaxTree(SourceText text, IEnumerable<Diagnostic> diagnostics, ExpressionSyntax root, SyntaxToken endOfInputToken)
        {
            Diagnostics = diagnostics.ToArray();
            Text = text;
            Root = root;
            EndOfInputToken = endOfInputToken;
        }

        public static SyntaxTree Parse(string source) => Parse(SourceText.From(source));

        public static SyntaxTree Parse(SourceText source)
        {
            var lexer = new Lexer(source);
            var parser = new Parser(lexer);
            return parser.Parse();
        }

        public static IEnumerable<SyntaxToken> ParseTokens(string source) => ParseTokens(SourceText.From(source));

        public static IEnumerable<SyntaxToken> ParseTokens(SourceText source)
        {
            var lexer = new Lexer(source);
            while (true)
            {
                var token = lexer.NextToken();
                if (token.Kind == SyntaxKind.EndOfInputToken)
                    break;

                yield return token;
            }
        }
    }
}