using System.Collections.Generic;
using System.Collections.Immutable;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Syntax;

public class SyntaxTree
{
    public ImmutableArray<Diagnostic> Diagnostics { get; }
    public SourceFile File { get; }
    public CompilationUnitSyntax Root { get; }

    private SyntaxTree(SourceFile file)
    {
        File = file;
        var parser = new Parser(file);

        Root = parser.ParseCompilationUnit();
        Diagnostics = parser.Diagnostics.ToImmutableArray();
    }

    public static SyntaxTree LoadFile(string fileName)
    {
        var text = System.IO.File.ReadAllText(fileName);
        var sourceText = SourceFile.From(text, fileName);
        return Parse(sourceText);
    }

    public static SyntaxTree Parse(string source) => Parse(SourceFile.From(source));

    public static SyntaxTree Parse(SourceFile source) => new SyntaxTree(source);

    public static IEnumerable<SyntaxToken> ParseTokens(string source) =>
        ParseTokens(SourceFile.From(source));

    public static IEnumerable<SyntaxToken> ParseTokens(SourceFile sourceFile) =>
        ParseTokens(sourceFile, out _);

    public static IEnumerable<SyntaxToken> ParseTokens(
        SourceFile sourceFile,
        out ImmutableArray<Diagnostic> diagnostics
    )
    {
        var tokens = new List<SyntaxToken>();
        var lexer = new Lexer(sourceFile);

        while (true)
        {
            var token = lexer.NextToken();
            if (token.Kind == SyntaxKind.EndOfInputToken)
            {
                break;
            }

            tokens.Add(token);
        }

        diagnostics = lexer.Diagnostics.ToImmutableArray();

        // Creating the SyntaxTree has a side-effect of running the code above
        return tokens;
    }
}
