using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Syntax
{
    public class SyntaxTree
    {
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public SourceText Text { get; }
        public CompilationUnitSyntax Root { get; }

        private SyntaxTree(SourceText text)
        {
            var parser = new Parser(text);

            Root = parser.ParseCompilationUnit();
            Diagnostics = parser.Diagnostics.ToImmutableArray();
            Text = text;
        }

        public static SyntaxTree Parse(string source) => Parse(SourceText.From(source));

        public static SyntaxTree Parse(SourceText source) => new SyntaxTree(source);

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