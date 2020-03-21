using System.Collections.Generic;
using System.Linq;

namespace Panther.CodeAnalysis.Syntax
{
    public class SyntaxTree
    {
        public IReadOnlyList<string> Diagnostics { get; }
        public ExpressionSyntax Root { get; }
        public SyntaxToken EndOfInputToken { get; }

        public SyntaxTree(IEnumerable<string> diagnostics, ExpressionSyntax root, SyntaxToken endOfInputToken)
        {
            Diagnostics = diagnostics.ToArray();
            Root = root;
            EndOfInputToken = endOfInputToken;
        }

        public static SyntaxTree Parse(string source)
        {
            var lexer = new Lexer(source);
            var parser = new Parser(lexer);
            return parser.Parse();
        }
    }
}