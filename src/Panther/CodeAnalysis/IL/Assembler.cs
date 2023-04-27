using System.Collections.Generic;
using System.Collections.Immutable;
using Panther.CodeAnalysis.Syntax;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.IL;

public class Assembler
{
    public Assembler()
    {
        // SourceFile.FromFile()
        // new Lexer();
    }
}

record Listing();

class AssemblyParser
{
    private readonly SourceFile _sourceFile;
    public DiagnosticBag Diagnostics { get; } = new DiagnosticBag();
    private ImmutableArray<SyntaxToken> _tokens;
    private int _tokenPosition = 0;

    public AssemblyParser(SourceFile sourceFile)
    {
        var lexer = new Lexer(sourceFile, GetKeywordKind);

        var tokens = new List<SyntaxToken>();
        while (true)
        {
            var token = lexer.NextToken();
            tokens.Add(token);
            if (token.Kind == SyntaxKind.EndOfInputToken)
            {
                break;
            }
        }
        _tokens = tokens.ToImmutableArray();
        _sourceFile = sourceFile;

        this.Diagnostics.AddRange(lexer.Diagnostics);
    }

    public Listing ParseListing()
    {
        while (CurrentKind != SyntaxKind.EndOfInputToken) { }
    }

    private SyntaxKind GetKeywordKind(string span) =>
        span switch
        {
            "add" => SyntaxKind.AddKeyword,
            "and" => SyntaxKind.AndKeyword,
            "br" => SyntaxKind.BrKeyword,
            "brfalse" => SyntaxKind.BrfalseKeyword,
            "brtrue" => SyntaxKind.BrtrueKeyword,
            "call" => SyntaxKind.CallKeyword,
            "ceq" => SyntaxKind.CeqKeyword,
            "cgt" => SyntaxKind.CgtKeyword,
            "clt" => SyntaxKind.CltKeyword,
            "div" => SyntaxKind.DivKeyword,
            "function" => SyntaxKind.FunctionKeyword,
            "label" => SyntaxKind.LabelKeyword,
            "ldarg" => SyntaxKind.LdargKeyword,
            "ldc" => SyntaxKind.LdcKeyword,
            "ldfld" => SyntaxKind.LdfldKeyword,
            "ldloc" => SyntaxKind.LdlocKeyword,
            "ldsfld" => SyntaxKind.LdsfldKeyword,
            "ldstr" => SyntaxKind.LdstrKeyword,
            "mul" => SyntaxKind.MulKeyword,
            "neg" => SyntaxKind.NegKeyword,
            "new" => SyntaxKind.NewKeyword,
            "nop" => SyntaxKind.NopKeyword,
            "not" => SyntaxKind.NotKeyword,
            "or" => SyntaxKind.OrKeyword,
            "pop" => SyntaxKind.PopKeyword,
            "ret" => SyntaxKind.RetKeyword,
            "starg" => SyntaxKind.StargKeyword,
            "stfld" => SyntaxKind.StfldKeyword,
            "stloc" => SyntaxKind.StlocKeyword,
            "stsfld" => SyntaxKind.StsfldKeyword,
            "sub" => SyntaxKind.SubKeyword,
            "xor" => SyntaxKind.XorKeyword,

            _ => SyntaxKind.IdentifierToken
        };

    private SyntaxToken TokenFromPosition(int pos) =>
        pos > _tokens.Length - 1 ? _tokens[^1] : _tokens[pos];

    private SyntaxToken CurrentToken => TokenFromPosition(_tokenPosition);
    private SyntaxKind CurrentKind => CurrentToken.Kind;

    private void NextToken()
    {
        _tokenPosition += 1;
    }

    private SyntaxToken Accept()
    {
        var token = CurrentToken;
        NextToken();
        return token;
    }

    private SyntaxToken Accept(SyntaxKind kind)
    {
        var currentToken = CurrentToken;
        if (currentToken.Kind == kind)
            return Accept();

        Diagnostics.ReportUnexpectedToken(currentToken.Location, currentToken.Kind, kind);

        return new SyntaxToken(_sourceFile, kind, currentToken.Position);
    }
}
