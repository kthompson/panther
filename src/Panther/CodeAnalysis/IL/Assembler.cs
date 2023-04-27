using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Panther.CodeAnalysis.Syntax;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.IL;

public class Assembler
{
    public SourceFile File { get; }
    public ImmutableArray<Diagnostic> Diagnostics { get; }
    public AssemblyListing Listing { get; }

    private Assembler(SourceFile file)
    {
        File = file;
        var parser = new AssemblyParser(file);

        Listing = parser.ParseListing();
        Diagnostics = parser.Diagnostics.ToImmutableArray();
    }

    public static Assembler ParseFile(string fileName)
    {
        var text = System.IO.File.ReadAllText(fileName);
        var sourceText = SourceFile.From(text, fileName);
        return Parse(sourceText);
    }

    public static Assembler Parse(SourceFile source) => new Assembler(source);

    public static Assembler ParseText(string source) => Parse(SourceFile.From(source));
}

class AssemblyParser
{
    private readonly SourceFile _sourceFile;
    public DiagnosticBag Diagnostics { get; } = new DiagnosticBag();
    private readonly ImmutableArray<SyntaxToken> _tokens;
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

        Diagnostics.AddRange(lexer.Diagnostics);
    }

    public AssemblyListing ParseListing()
    {
        var instructions = ImmutableArray.CreateBuilder<InstructionSyntax>();
        while (CurrentKind != SyntaxKind.EndOfInputToken)
        {
            var instruction = ParseInstruction();
            instructions.Add(instruction);
        }

        var endToken = Accept(SyntaxKind.EndOfInputToken);

        return new AssemblyListing(_sourceFile, instructions.ToImmutable(), endToken);
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

            case SyntaxKind.FunctionKeyword:
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
        var code = GetOpCode(CurrentKind);
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
        var terminator = GetStatementTerminator(expr) ?? GetStatementTerminator(CurrentToken);
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

    private readonly Dictionary<SyntaxKind, OpCode> _opCodeLookup = new Dictionary<
        SyntaxKind,
        OpCode
    >
    {
        [SyntaxKind.AddKeyword] = OpCode.Add,
        [SyntaxKind.AndKeyword] = OpCode.And,
        [SyntaxKind.BrKeyword] = OpCode.Br,
        [SyntaxKind.BrfalseKeyword] = OpCode.Brfalse,
        [SyntaxKind.BrtrueKeyword] = OpCode.Brtrue,
        [SyntaxKind.CallKeyword] = OpCode.Call,
        [SyntaxKind.CeqKeyword] = OpCode.Ceq,
        [SyntaxKind.CgtKeyword] = OpCode.Cgt,
        [SyntaxKind.CltKeyword] = OpCode.Clt,
        [SyntaxKind.DivKeyword] = OpCode.Div,
        [SyntaxKind.FunctionKeyword] = OpCode.Function,
        [SyntaxKind.LabelKeyword] = OpCode.Label,
        [SyntaxKind.LdargKeyword] = OpCode.Ldarg,
        [SyntaxKind.LdcKeyword] = OpCode.Ldc,
        [SyntaxKind.LdfldKeyword] = OpCode.Ldfld,
        [SyntaxKind.LdlocKeyword] = OpCode.Ldloc,
        [SyntaxKind.LdsfldKeyword] = OpCode.Ldsfld,
        [SyntaxKind.LdstrKeyword] = OpCode.Ldstr,
        [SyntaxKind.MulKeyword] = OpCode.Mul,
        [SyntaxKind.NegKeyword] = OpCode.Neg,
        [SyntaxKind.NewKeyword] = OpCode.New,
        [SyntaxKind.NopKeyword] = OpCode.Nop,
        [SyntaxKind.NotKeyword] = OpCode.Not,
        [SyntaxKind.OrKeyword] = OpCode.Or,
        [SyntaxKind.PopKeyword] = OpCode.Pop,
        [SyntaxKind.RetKeyword] = OpCode.Ret,
        [SyntaxKind.StargKeyword] = OpCode.Starg,
        [SyntaxKind.StfldKeyword] = OpCode.Stfld,
        [SyntaxKind.StlocKeyword] = OpCode.Stloc,
        [SyntaxKind.StsfldKeyword] = OpCode.Stsfld,
        [SyntaxKind.SubKeyword] = OpCode.Sub,
        [SyntaxKind.XorKeyword] = OpCode.Xor,
    };

    private OpCode? GetOpCode(SyntaxKind kind) =>
        _opCodeLookup.TryGetValue(kind, out var opCode) ? opCode : null;

    private readonly Dictionary<string, SyntaxKind> _kindLookup = new Dictionary<string, SyntaxKind>
    {
        ["add"] = SyntaxKind.AddKeyword,
        ["and"] = SyntaxKind.AndKeyword,
        ["br"] = SyntaxKind.BrKeyword,
        ["brfalse"] = SyntaxKind.BrfalseKeyword,
        ["brtrue"] = SyntaxKind.BrtrueKeyword,
        ["call"] = SyntaxKind.CallKeyword,
        ["ceq"] = SyntaxKind.CeqKeyword,
        ["cgt"] = SyntaxKind.CgtKeyword,
        ["clt"] = SyntaxKind.CltKeyword,
        ["div"] = SyntaxKind.DivKeyword,
        ["function"] = SyntaxKind.FunctionKeyword,
        ["label"] = SyntaxKind.LabelKeyword,
        ["ldarg"] = SyntaxKind.LdargKeyword,
        ["ldc"] = SyntaxKind.LdcKeyword,
        ["ldfld"] = SyntaxKind.LdfldKeyword,
        ["ldloc"] = SyntaxKind.LdlocKeyword,
        ["ldsfld"] = SyntaxKind.LdsfldKeyword,
        ["ldstr"] = SyntaxKind.LdstrKeyword,
        ["mul"] = SyntaxKind.MulKeyword,
        ["neg"] = SyntaxKind.NegKeyword,
        ["new"] = SyntaxKind.NewKeyword,
        ["nop"] = SyntaxKind.NopKeyword,
        ["not"] = SyntaxKind.NotKeyword,
        ["or"] = SyntaxKind.OrKeyword,
        ["pop"] = SyntaxKind.PopKeyword,
        ["ret"] = SyntaxKind.RetKeyword,
        ["starg"] = SyntaxKind.StargKeyword,
        ["stfld"] = SyntaxKind.StfldKeyword,
        ["stloc"] = SyntaxKind.StlocKeyword,
        ["stsfld"] = SyntaxKind.StsfldKeyword,
        ["sub"] = SyntaxKind.SubKeyword,
        ["xor"] = SyntaxKind.XorKeyword,
    };

    private SyntaxKind GetKeywordKind(string span) =>
        _kindLookup.TryGetValue(span, out var kind) ? kind : SyntaxKind.IdentifierToken;

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
