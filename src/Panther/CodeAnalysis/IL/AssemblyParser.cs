using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Panther.CodeAnalysis.Syntax;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.IL;

class AssemblyParser
{
    private readonly SourceFile _sourceFile;
    public DiagnosticBag Diagnostics { get; } = new DiagnosticBag();
    private readonly ImmutableArray<SyntaxToken> _tokens;
    private int _tokenPosition = 0;

    public AssemblyParser(SourceFile sourceFile)
    {
        var lexer = new Lexer(sourceFile, AssemblyFacts.GetKeywordKind, true);

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

        Diagnostics.AddRange(lexer.Diagnostics);
    }

    public AssemblyListingSyntax ParseListing()
    {
        var classes = ImmutableArray.CreateBuilder<AssemblyClassDeclarationSyntax>();
        while (CurrentKind == SyntaxKind.ClassKeyword)
        {
            var instruction = ParseClassDeclaration();
            classes.Add(instruction);
        }

        var endToken = Accept(SyntaxKind.EndOfInputToken);

        return new AssemblyListingSyntax(_sourceFile, classes.ToImmutable(), endToken);
    }

    private AssemblyClassDeclarationSyntax ParseClassDeclaration()
    {
        var classKeyword = Accept();
        var name = Accept(SyntaxKind.IdentifierToken);
        var openBrace = Accept(SyntaxKind.OpenBraceToken);

        var fields = ImmutableArray.CreateBuilder<AssemblyFieldDeclarationSyntax>();
        while (CurrentKind == SyntaxKind.FieldKeyword)
        {
            var instruction = ParseFieldDeclaration();
            fields.Add(instruction);
        }

        var methods = ImmutableArray.CreateBuilder<AssemblyMethodDeclarationSyntax>();
        while (CurrentKind == SyntaxKind.MethodKeyword)
        {
            var instruction = ParseMethodDeclaration();
            methods.Add(instruction);
        }

        var closeBrace = Accept(SyntaxKind.CloseBraceToken);

        return new AssemblyClassDeclarationSyntax(
            _sourceFile,
            classKeyword,
            name,
            openBrace,
            fields.ToImmutable(),
            methods.ToImmutable(),
            closeBrace
        );
    }

    private AssemblyFieldDeclarationSyntax ParseFieldDeclaration()
    {
        var fieldKeyword = Accept();
        var staticToken = CurrentKind == SyntaxKind.StaticKeyword ? Accept() : null;
        // TODO: parse field type
        var name = Accept(SyntaxKind.IdentifierToken);

        return new AssemblyFieldDeclarationSyntax(_sourceFile, fieldKeyword, staticToken, name);
    }

    private AssemblyMethodDeclarationSyntax ParseMethodDeclaration()
    {
        var methodKeyword = Accept();
        var entryPointToken = CurrentKind == SyntaxKind.EntryPointKeyword ? Accept() : null;
        var name = Accept(SyntaxKind.IdentifierToken);

        var locals = Accept(SyntaxKind.NumberToken);

        var openParenToken = Accept(SyntaxKind.OpenParenToken);
        var parameters = ParseParameterList();
        var closeParenToken = Accept(SyntaxKind.CloseParenToken);
        var typeAnnotation = ParseTypeAnnotation();
        var openBrace = Accept(SyntaxKind.OpenBraceToken);

        var instructions = ImmutableArray.CreateBuilder<InstructionSyntax>();
        while (
            CurrentKind != SyntaxKind.MethodKeyword
            && CurrentKind != SyntaxKind.EndOfInputToken
            && CurrentKind != SyntaxKind.ClassKeyword
        )
        {
            var instruction = ParseInstruction();
            instructions.Add(instruction);
        }
        var closeBrace = Accept(SyntaxKind.CloseBraceToken);
        return new AssemblyMethodDeclarationSyntax(
            _sourceFile,
            methodKeyword,
            entryPointToken,
            name,
            locals,
            openParenToken,
            parameters,
            closeParenToken,
            typeAnnotation,
            openBrace,
            instructions.ToImmutable(),
            closeBrace
        );
    }

    private SeparatedSyntaxList<ParameterSyntax> ParseParameterList()
    {
        if (CurrentKind == SyntaxKind.CloseParenToken)
            return new SeparatedSyntaxList<ParameterSyntax>(ImmutableArray<SyntaxNode>.Empty);

        var items = ImmutableArray.CreateBuilder<SyntaxNode>();

        while (CurrentKind != SyntaxKind.EndOfInputToken)
        {
            var currentToken = CurrentToken;
            var arg = ParseParameter();
            items.Add(arg);

            if (CurrentKind == SyntaxKind.CloseParenToken)
                break;

            var comma = Accept(SyntaxKind.CommaToken);
            items.Add(comma);
            if (CurrentToken == currentToken)
            {
                NextToken();
            }
        }

        return new SeparatedSyntaxList<ParameterSyntax>(items.ToImmutable());
    }

    private ParameterSyntax ParseParameter()
    {
        var ident = Accept(SyntaxKind.IdentifierToken);
        var typeAnnotation = ParseTypeAnnotation();

        return new ParameterSyntax(_sourceFile, ident, typeAnnotation);
    }

    private TypeAnnotationSyntax ParseTypeAnnotation()
    {
        var colonToken = Accept(SyntaxKind.ColonToken);
        var type = ParseNameSyntax();

        return new TypeAnnotationSyntax(_sourceFile, colonToken, type);
    }

    private NameSyntax ParseNameSyntax() => ParseSimpleName();

    private SimpleNameSyntax ParseSimpleName()
    {
        var ident = Accept(SyntaxKind.IdentifierToken);

        return new IdentifierNameSyntax(_sourceFile, ident);
    }

    private InstructionSyntax ParseInstruction()
    {
        switch (CurrentKind)
        {
            case SyntaxKind.LabelKeyword:
            case SyntaxKind.BrKeyword:
            case SyntaxKind.BrfalseKeyword:
            case SyntaxKind.BrtrueKeyword:
            case SyntaxKind.LdsfldKeyword:
            case SyntaxKind.StsfldKeyword:
                return ParseLabelOperandInstruction();

            case SyntaxKind.LdargKeyword:
            case SyntaxKind.LdcKeyword:
            case SyntaxKind.LdfldKeyword:
            case SyntaxKind.LdlocKeyword:
            case SyntaxKind.NewKeyword:
            case SyntaxKind.StargKeyword:
            case SyntaxKind.StfldKeyword:
            case SyntaxKind.StlocKeyword:
                return ParseIntOperandInstruction();

            case SyntaxKind.LdstrKeyword:
                return ParseLoadStringInstruction();

            case SyntaxKind.CallKeyword:
                return ParseCallInstruction();

            case SyntaxKind.MethodKeyword:
                return ParseFunctionInstruction();

            case SyntaxKind.AddKeyword:
            case SyntaxKind.AndKeyword:
            case SyntaxKind.CeqKeyword:
            case SyntaxKind.CgtKeyword:
            case SyntaxKind.CltKeyword:
            case SyntaxKind.DivKeyword:
            case SyntaxKind.MulKeyword:
            case SyntaxKind.NegKeyword:
            case SyntaxKind.NopKeyword:
            case SyntaxKind.NotKeyword:
            case SyntaxKind.OrKeyword:
            case SyntaxKind.PopKeyword:
            case SyntaxKind.RetKeyword:
            case SyntaxKind.SubKeyword:
            case SyntaxKind.XorKeyword:
            default:
                return ParseNoOperandInstruction();
        }
    }

    private SyntaxToken AcceptOpCode()
    {
        var code = AssemblyFacts.GetOpCode(CurrentKind);
        if (code == null)
            Diagnostics.ReportUnexpectedOpCode(CurrentToken.Location, CurrentKind);

        return Accept();
    }

    private InstructionSyntax ParseFunctionInstruction()
    {
        var code = AcceptOpCode();
        var label = Accept(SyntaxKind.IdentifierToken);
        var operand = Accept(SyntaxKind.NumberToken);
        AssertStatementTerminator(operand);
        return new FunctionInstructionSyntax(_sourceFile, code, label, operand);
    }

    private InstructionSyntax ParseCallInstruction()
    {
        var code = AcceptOpCode();
        var label = Accept(SyntaxKind.IdentifierToken);
        var operand = Accept(SyntaxKind.NumberToken);
        AssertStatementTerminator(operand);
        return new CallInstructionSyntax(_sourceFile, code, label, operand);
    }

    private InstructionSyntax ParseLoadStringInstruction()
    {
        var code = AcceptOpCode();
        var operand = Accept(SyntaxKind.StringToken);
        AssertStatementTerminator(operand);

        return new LoadStringInstructionSyntax(_sourceFile, code, operand);
    }

    private InstructionSyntax ParseLabelOperandInstruction()
    {
        var code = AcceptOpCode();
        var label = Accept(SyntaxKind.IdentifierToken);
        AssertStatementTerminator(label);

        return new LabelOperandInstructionSyntax(_sourceFile, code, label);
    }

    private InstructionSyntax ParseIntOperandInstruction()
    {
        var code = AcceptOpCode();
        var number = Accept(SyntaxKind.NumberToken);
        AssertStatementTerminator(number);

        return new IntOperandInstructionSyntax(_sourceFile, code, number);
    }

    private InstructionSyntax ParseNoOperandInstruction()
    {
        var code = AcceptOpCode();
        AssertStatementTerminator(code);

        return new NoOperandInstructionSyntax(_sourceFile, code);
    }

    private void AssertStatementTerminator(SyntaxNode expr)
    {
        var terminator = GetStatementTerminator(expr);
        if (terminator == null)
        {
            Diagnostics.ReportExpectedEndOfLineTrivia(expr.Location);
        }
    }

    private static SyntaxKind? GetStatementTerminator(SyntaxNode expression)
    {
        var syntaxNode = expression
            .DescendantsAndSelf()
            .LastOrDefault(x => x.Kind != SyntaxKind.WhitespaceTrivia);

        return syntaxNode?.Kind switch
        {
            SyntaxKind.EndOfInputToken => SyntaxKind.EndOfInputToken,
            SyntaxKind.EndOfLineTrivia => SyntaxKind.EndOfLineTrivia,
            _ => null
        };
    }

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
