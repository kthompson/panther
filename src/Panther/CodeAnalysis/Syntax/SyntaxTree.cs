using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Net;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Syntax;

public class SyntaxTree
{
    private delegate void ParseHandler(SyntaxTree syntaxTree,
        out CompilationUnitSyntax root,
        out ImmutableArray<Diagnostic> diagnostics);

    public ImmutableArray<Diagnostic> Diagnostics { get; }
    public SourceFile File { get; }
    public CompilationUnitSyntax Root { get; }

    private SyntaxTree(SourceFile file, ParseHandler handler)
    {
        File = file;

        handler(this, out var root, out var diagnostics);

        Root = root;
        Diagnostics = diagnostics.ToImmutableArray();
    }

    public static SyntaxTree LoadFile(string fileName)
    {
        var text = System.IO.File.ReadAllText(fileName);
        var sourceText = SourceFile.From(text, fileName);
        return Parse(sourceText);
    }

    private static void Parse(SyntaxTree syntaxTree,
        out CompilationUnitSyntax root,
        out ImmutableArray<Diagnostic> diagnostics)
    {
        var parser = new Parser(syntaxTree);
        root = parser.ParseCompilationUnit();
        diagnostics = parser.Diagnostics.ToImmutableArray();
    }

    public static SyntaxTree Parse(string source) => Parse(SourceFile.From(source));

    public static SyntaxTree Parse(SourceFile source) => new SyntaxTree(source, Parse);

    public static IEnumerable<SyntaxToken> ParseTokens(string source) => ParseTokens(SourceFile.From(source));

    public static IEnumerable<SyntaxToken> ParseTokens(SourceFile sourceFile) =>
        ParseTokens(sourceFile, out var _);

    public static IEnumerable<SyntaxToken> ParseTokens(SourceFile sourceFile, out ImmutableArray<Diagnostic> diagnostics)
    {
        var tokens = new List<SyntaxToken>();

        void ParseTokens(SyntaxTree syntaxTree,
            out CompilationUnitSyntax root,
            out ImmutableArray<Diagnostic> diags)
        {
            var lexer = new Lexer(syntaxTree);

            while (true)
            {
                var token = lexer.NextToken();
                if (token.Kind == SyntaxKind.EndOfInputToken)
                {
                    root = new CompilationUnitSyntax(
                        syntaxTree,
                        ImmutableArray<UsingDirectiveSyntax>.Empty,
                        ImmutableArray<MemberSyntax>.Empty,
                        token
                    );
                    break;
                }

                tokens.Add(token);
            }

            diags = lexer.Diagnostics.ToImmutableArray();
        }

        // Creating the SyntaxTree has a side-effect of running the code above
        var tree = new SyntaxTree(sourceFile, ParseTokens);
        diagnostics = tree.Diagnostics;
        return tokens;
    }
}