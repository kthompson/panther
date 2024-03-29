﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Syntax;

internal delegate ExpressionSyntax PrefixParseFunction();

internal delegate ExpressionSyntax InfixParseFunction(ExpressionSyntax expression);

//internal enum OperatorPrecedence
//{
//    Lowest,
//    Equals, // ==
//    LessGreater, // > or <
//    Sum, // +
//    Product, // *
//    Prefix, // -X or !X
//    Call, // myFunction(X)
//    Index // myArray[x]
//}

internal class Parser
{
    private readonly SourceFile _sourceFile;
    public DiagnosticBag Diagnostics { get; } = new DiagnosticBag();

    private readonly Dictionary<SyntaxKind, PrefixParseFunction> _prefixParseFunctions =
        new Dictionary<SyntaxKind, PrefixParseFunction>();

    private readonly Dictionary<SyntaxKind, InfixParseFunction> _infixParseFunctions =
        new Dictionary<SyntaxKind, InfixParseFunction>();

    private ImmutableArray<SyntaxToken> _tokens;
    private int _tokenPosition = 0;

    public Parser(SourceFile sourceFile)
    {
        var lexer = new Lexer(sourceFile, SyntaxFacts.GetKeywordKind);
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

        // a
        _prefixParseFunctions[SyntaxKind.CharToken] = ParseLiteralExpression;
        _prefixParseFunctions[SyntaxKind.FalseKeyword] = ParseBooleanLiteral;
        _prefixParseFunctions[SyntaxKind.ForKeyword] = ParseForExpression;
        _prefixParseFunctions[SyntaxKind.IdentifierToken] = ParseIdentifierName;
        _prefixParseFunctions[SyntaxKind.ThisKeyword] = ParseThis;
        _prefixParseFunctions[SyntaxKind.IfKeyword] = ParseIfExpression;
        _prefixParseFunctions[SyntaxKind.NumberToken] = ParseLiteralExpression;
        _prefixParseFunctions[SyntaxKind.NewKeyword] = ParseNewExpression;
        _prefixParseFunctions[SyntaxKind.NullKeyword] = ParseNull;
        _prefixParseFunctions[SyntaxKind.OpenBraceToken] = ParseBlockExpression;
        _prefixParseFunctions[SyntaxKind.OpenParenToken] = ParseGroupOrUnitExpression;
        _prefixParseFunctions[SyntaxKind.StringToken] = ParseLiteralExpression;
        _prefixParseFunctions[SyntaxKind.TrueKeyword] = ParseBooleanLiteral;
        _prefixParseFunctions[SyntaxKind.WhileKeyword] = ParseWhileExpression;

        // -a
        _prefixParseFunctions[SyntaxKind.BangToken] = ParsePrefixExpression;
        _prefixParseFunctions[SyntaxKind.DashToken] = ParsePrefixExpression;
        _prefixParseFunctions[SyntaxKind.PlusToken] = ParsePrefixExpression;
        _prefixParseFunctions[SyntaxKind.TildeToken] = ParsePrefixExpression;

        // a + b
        _infixParseFunctions[SyntaxKind.AmpersandAmpersandToken] = ParseInfixExpression;
        _infixParseFunctions[SyntaxKind.AmpersandToken] = ParseInfixExpression;
        _infixParseFunctions[SyntaxKind.BangEqualsToken] = ParseInfixExpression;
        _infixParseFunctions[SyntaxKind.CaretToken] = ParseInfixExpression;
        _infixParseFunctions[SyntaxKind.DashToken] = ParseInfixExpression;
        _infixParseFunctions[SyntaxKind.DotToken] = ParseMemberAccessExpression;
        _infixParseFunctions[SyntaxKind.EqualsEqualsToken] = ParseInfixExpression;
        _infixParseFunctions[SyntaxKind.EqualsToken] = ParseAssignmentExpression;
        _infixParseFunctions[SyntaxKind.GreaterThanEqualsToken] = ParseInfixExpression;
        _infixParseFunctions[SyntaxKind.GreaterThanToken] = ParseInfixExpression;
        _infixParseFunctions[SyntaxKind.LessThanEqualsToken] = ParseInfixExpression;
        _infixParseFunctions[SyntaxKind.LessThanToken] = ParseInfixExpression;
        _infixParseFunctions[SyntaxKind.OpenBracketToken] = ParseIndexExpression;
        _infixParseFunctions[SyntaxKind.OpenParenToken] = ParseCallExpression;
        _infixParseFunctions[SyntaxKind.PipePipeToken] = ParseInfixExpression;
        _infixParseFunctions[SyntaxKind.PipeToken] = ParseInfixExpression;
        _infixParseFunctions[SyntaxKind.PlusToken] = ParseInfixExpression;
        _infixParseFunctions[SyntaxKind.SlashToken] = ParseInfixExpression;
        _infixParseFunctions[SyntaxKind.StarToken] = ParseInfixExpression;
    }

    private SyntaxToken CurrentToken => TokenFromPosition(_tokenPosition);
    private SyntaxKind CurrentKind => CurrentToken.Kind;

    private SyntaxToken TokenFromPosition(int pos) =>
        pos > _tokens.Length - 1 ? _tokens[^1] : _tokens[pos];

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

    private SyntaxToken Create(SyntaxKind kind, string text)
    {
        var currentToken = CurrentToken;

        return new SyntaxToken(_sourceFile, kind, currentToken.Position, text, null);
    }

    public CompilationUnitSyntax ParseCompilationUnit()
    {
        var namespaceDeclaration = ParseNamespaceDeclaration();
        var usingDirectives = ParseUsings();
        var namespaceSyntax = ParseMembers();

        var endToken = Accept(SyntaxKind.EndOfInputToken);

        return new CompilationUnitSyntax(
            _sourceFile,
            namespaceDeclaration,
            usingDirectives,
            namespaceSyntax,
            endToken
        );
    }

    private ImmutableArray<UsingDirectiveSyntax> ParseUsings()
    {
        var usingDirectives = ImmutableArray.CreateBuilder<UsingDirectiveSyntax>();
        while (CurrentKind == SyntaxKind.UsingKeyword)
        {
            usingDirectives.Add(ParseUsingDirective());
        }

        return usingDirectives.ToImmutable();
    }

    private ImmutableArray<MemberSyntax> ParseMembers()
    {
        var members = ImmutableArray.CreateBuilder<MemberSyntax>();
        while (
            CurrentKind != SyntaxKind.EndOfInputToken && CurrentKind != SyntaxKind.CloseBraceToken
        )
        {
            var startToken = CurrentToken;

            var member = ParseMember();
            members.Add(member);

            if (CurrentToken == startToken)
                NextToken();
        }

        return members.ToImmutable();
    }

    private MemberSyntax ParseMember() =>
        CurrentKind switch
        {
            SyntaxKind.DefKeyword => ParseFunctionDeclaration(),
            SyntaxKind.ClassKeyword => ParseClassDeclaration(),
            SyntaxKind.ObjectKeyword => ParseObjectDeclaration(),
            _ => ParseGlobalStatement(),
        };

    private NamespaceDeclarationSyntax? ParseNamespaceDeclaration()
    {
        if (CurrentKind != SyntaxKind.NamespaceKeyword)
            return null;

        var namespaceKeyword = Accept();
        var name = ParseNameSyntax();

        return new NamespaceDeclarationSyntax(_sourceFile, namespaceKeyword, name);
    }

    private TemplateSyntax ParseTemplate()
    {
        var openBraceToken = Accept(SyntaxKind.OpenBraceToken);
        var members = ParseMembers();
        var closeBraceToken = Accept(SyntaxKind.CloseBraceToken);

        return new TemplateSyntax(_sourceFile, openBraceToken, members, closeBraceToken);
    }

    private MemberSyntax ParseClassDeclaration()
    {
        var keyword = Accept();
        var identifier = Accept(SyntaxKind.IdentifierToken);
        var openParenToken = Accept(SyntaxKind.OpenParenToken);
        var parameters = ParseParameterList();
        var closeParenToken = Accept(SyntaxKind.CloseParenToken);

        var template = CurrentKind == SyntaxKind.OpenBraceToken ? ParseTemplate() : null;

        return new ClassDeclarationSyntax(
            _sourceFile,
            keyword,
            identifier,
            openParenToken,
            parameters,
            closeParenToken,
            template
        );
    }

    private MemberSyntax ParseObjectDeclaration()
    {
        var objectKeyword = Accept(SyntaxKind.ObjectKeyword);
        var identifier = Accept(SyntaxKind.IdentifierToken);
        var template = ParseTemplate();

        return new ObjectDeclarationSyntax(_sourceFile, objectKeyword, identifier, template);
    }

    private GlobalStatementSyntax ParseGlobalStatement()
    {
        var statement = ParseStatement();
        return new GlobalStatementSyntax(_sourceFile, statement);
    }

    private UsingDirectiveSyntax ParseUsingDirective()
    {
        var usingKeyword = Accept();
        var special =
            (CurrentKind == SyntaxKind.ImplicitKeyword || CurrentKind == SyntaxKind.StaticKeyword)
                ? Accept()
                : null;

        var name = ParseNameSyntax();

        return new UsingDirectiveSyntax(_sourceFile, usingKeyword, special, name);
    }

    private NameSyntax ParseNameSyntax()
    {
        NameSyntax name = ParseSimpleName();

        if (name is GenericNameSyntax)
            return name;

        while (CurrentKind == SyntaxKind.DotToken)
        {
            var dot = Accept();
            var right = ParseSimpleName();

            name = new QualifiedNameSyntax(_sourceFile, name, dot, right);

            if (right is GenericNameSyntax)
                return name;
        }

        return name;
    }

    private MemberSyntax ParseFunctionDeclaration()
    {
        var defKeyword = Accept();
        var identifier = Accept(SyntaxKind.IdentifierToken);

        var openParenToken = Accept(SyntaxKind.OpenParenToken);
        var parameters = ParseParameterList();
        var closeParenToken = Accept(SyntaxKind.CloseParenToken);
        var typeAnnotation = ParseOptionalTypeAnnotation();
        var body = ParseFunctionBody();

        return new FunctionDeclarationSyntax(
            _sourceFile,
            defKeyword,
            identifier,
            openParenToken,
            parameters,
            closeParenToken,
            typeAnnotation,
            body
        );
    }

    private FunctionBodySyntax? ParseFunctionBody()
    {
        if (CurrentKind == SyntaxKind.EqualsToken)
        {
            var equalsToken = Accept();
            var body = ParseExpression(OperatorPrecedence.Lowest);
            return new FunctionBodySyntax(_sourceFile, equalsToken, body);
        }

        return null;
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

    bool HasStatementTerminator(SyntaxNode expr) => GetStatementTerminator(expr) != null;

    private static SyntaxKind? GetStatementTerminator(SyntaxNode expression)
    {
        var syntaxNode = expression
            .DescendantsAndSelf()
            .LastOrDefault(x => x.Kind != SyntaxKind.WhitespaceTrivia);

        return syntaxNode?.Kind switch
        {
            SyntaxKind.EndOfInputToken => SyntaxKind.EndOfInputToken,
            SyntaxKind.EndOfLineTrivia => SyntaxKind.EndOfLineTrivia,
            SyntaxKind.CloseBraceToken => SyntaxKind.CloseBraceToken,
            _ => null
        };
    }

    private void AssertStatementTerminator(SyntaxNode expr)
    {
        var terminator = GetStatementTerminator(expr) ?? GetStatementTerminator(CurrentToken);
        if (terminator == null)
        {
            Diagnostics.ReportExpectedEndOfLineTrivia(expr.Location);
        }
    }

    private ExpressionSyntax ParseExpression(OperatorPrecedence precedence, bool inGroup = false)
    {
        bool isTerminatingLine(SyntaxNode node) => !inGroup && HasStatementTerminator(node);

        // advance until we get to the actual current token
        var currentToken = CurrentToken;
        var prefixFunction = _prefixParseFunctions.GetValueOrDefault(currentToken.Kind);
        if (prefixFunction == null)
        {
            // no prefix function
            // this results in an error from another location
            // so I don't think we need this here?
            // Diagnostics.ReportUnsupportedPrefixToken(currentToken);
            Diagnostics.ReportExpectedExpression(currentToken.Location, currentToken.Kind);
            return ParseLiteralExpression();
        }

        var left = prefixFunction();

        // investigate if we can use precedence to break early
        if (isTerminatingLine(left))
            return left;

        while (precedence < CurrentPrecedence() && !isTerminatingLine(left))
        {
            var infix = _infixParseFunctions.GetValueOrDefault(CurrentKind);
            if (infix == null)
                return left;

            left = infix(left);
        }

        return left;
    }

    private ExpressionSyntax ParsePrefixExpression()
    {
        // parse expressions like:
        // !b

        var unaryOperatorToken = Accept();

        var expression = ParseExpression(OperatorPrecedence.Prefix);

        return new UnaryExpressionSyntax(_sourceFile, unaryOperatorToken, expression);
    }

    private OperatorPrecedence CurrentPrecedence() =>
        CurrentKind.GetBinaryOperatorPrecedence() ?? OperatorPrecedence.Lowest;

    private ExpressionSyntax ParseMemberAccessExpression(ExpressionSyntax left)
    {
        // parse expressions like:
        // left.right
        var dotToken = Accept();
        var right = ParseIdentifierName();

        return new MemberAccessExpressionSyntax(_sourceFile, left, dotToken, right);
    }

    private ExpressionSyntax ParseInfixExpression(ExpressionSyntax left)
    {
        // parse expressions like:
        // left + right
        var precedence = CurrentPrecedence();
        var binaryOperatorToken = Accept();
        var right = ParseExpression(precedence);

        return new BinaryExpressionSyntax(_sourceFile, left, binaryOperatorToken, right);
    }

    private ExpressionSyntax ParseWhileExpression()
    {
        var whileKeyword = Accept();
        var openParenToken = Accept(SyntaxKind.OpenParenToken);
        var condition = ParseExpression(OperatorPrecedence.Lowest);
        var closeParenToken = Accept(SyntaxKind.CloseParenToken);

        var expr = ParseExpression(OperatorPrecedence.Lowest);

        return new WhileExpressionSyntax(
            _sourceFile,
            whileKeyword,
            openParenToken,
            condition,
            closeParenToken,
            expr
        );
    }

    private ExpressionSyntax ParseForExpression()
    {
        var forKeyword = Accept();
        var openParenToken = Accept(SyntaxKind.OpenParenToken);
        var variable = Accept(SyntaxKind.IdentifierToken);
        var leftArrow = Accept(SyntaxKind.LessThanDashToken);

        var fromExpression = ParseExpression(OperatorPrecedence.Lowest);
        var toKeyword = Accept(SyntaxKind.ToKeyword);
        var toExpression = ParseExpression(OperatorPrecedence.Lowest);
        var closeParenToken = Accept(SyntaxKind.CloseParenToken);

        var expr = ParseExpression(OperatorPrecedence.Lowest);

        return new ForExpressionSyntax(
            _sourceFile,
            forKeyword,
            openParenToken,
            variable,
            leftArrow,
            fromExpression,
            toKeyword,
            toExpression,
            closeParenToken,
            expr
        );
    }

    private ExpressionSyntax ParseIndexExpression(ExpressionSyntax left)
    {
        var openBracketToken = Accept();
        var expr = ParseExpression(OperatorPrecedence.Lowest);
        var closeBracketToken = Accept(SyntaxKind.CloseBracketToken);

        return new IndexExpressionSyntax(
            _sourceFile,
            left,
            openBracketToken,
            expr,
            closeBracketToken
        );
    }

    private ExpressionSyntax ParseCallExpression(ExpressionSyntax name)
    {
        var openParenToken = Accept(SyntaxKind.OpenParenToken);
        var arguments = ParseArguments();
        var closeParenToken = Accept(SyntaxKind.CloseParenToken);

        return new CallExpressionSyntax(
            _sourceFile,
            name,
            openParenToken,
            arguments,
            closeParenToken
        );
    }

    private SeparatedSyntaxList<ExpressionSyntax> ParseArguments() =>
        ParseExpressionList(SyntaxKind.CloseParenToken);

    private ArrayInitializerExpressionSyntax ParseArrayInitializers()
    {
        var openBrace = Accept();
        var expressions = ParseExpressionList(SyntaxKind.CloseBraceToken);
        var closeBrace = Accept(SyntaxKind.CloseBraceToken);

        return new ArrayInitializerExpressionSyntax(
            _sourceFile,
            openBrace,
            expressions,
            closeBrace
        );
    }

    private SeparatedSyntaxList<ExpressionSyntax> ParseExpressionList(SyntaxKind terminator)
    {
        var items = new List<SyntaxNode>();

        if (CurrentKind == terminator)
            return new SeparatedSyntaxList<ExpressionSyntax>(ImmutableArray<SyntaxNode>.Empty);

        while (true)
        {
            var arg = ParseExpression(OperatorPrecedence.Lowest);
            items.Add(arg);

            var currentToken = CurrentToken;
            if (currentToken.Kind == terminator || currentToken.Kind == SyntaxKind.EndOfInputToken)
                break;

            var comma = Accept(SyntaxKind.CommaToken);
            items.Add(comma);
        }

        return new SeparatedSyntaxList<ExpressionSyntax>(items.ToImmutableArray());
    }

    private ExpressionSyntax ParseIfExpression()
    {
        var ifKeyword = Accept();
        var openParenToken = Accept(SyntaxKind.OpenParenToken);
        var condition = ParseExpression(OperatorPrecedence.Lowest);
        var closeParenToken = Accept(SyntaxKind.CloseParenToken);
        var thenExpr = ParseExpression(OperatorPrecedence.Lowest);
        var elseKeyword = Accept(SyntaxKind.ElseKeyword);
        var elseExpr = ParseExpression(OperatorPrecedence.Lowest);

        return new IfExpressionSyntax(
            _sourceFile,
            ifKeyword,
            openParenToken,
            condition,
            closeParenToken,
            thenExpr,
            elseKeyword,
            elseExpr
        );
    }

    private ExpressionSyntax ParseGroupOrUnitExpression()
    {
        var open = Accept();
        if (CurrentKind == SyntaxKind.CloseParenToken)
        {
            // unit expression
            var close = Accept(SyntaxKind.CloseParenToken);
            return new UnitExpressionSyntax(_sourceFile, open, close);
        }
        else
        {
            var expr = ParseExpression(OperatorPrecedence.Lowest, true);
            var close = Accept(SyntaxKind.CloseParenToken);

            return new GroupExpressionSyntax(_sourceFile, open, expr, close);
        }
    }

    private ExpressionSyntax ParseNewExpression()
    {
        var newToken = Accept();
        var type = ParseNameSyntax();

        // new Point[ 5 ] or new Point[ ]
        if (CurrentKind == SyntaxKind.OpenBracketToken)
        {
            var openBracket = Accept();
            var rank =
                CurrentKind == SyntaxKind.CloseBracketToken
                    ? null
                    : ParseExpression(OperatorPrecedence.Lowest);
            var closeBracket = Accept(SyntaxKind.CloseBracketToken);

            var initializer =
                CurrentKind == SyntaxKind.OpenBraceToken ? ParseArrayInitializers() : null;

            return new ArrayCreationExpressionSyntax(
                _sourceFile,
                newToken,
                type,
                openBracket,
                rank,
                closeBracket,
                initializer
            );
        }

        // new Point(0, 1)
        var openToken = Accept(SyntaxKind.OpenParenToken);
        var arguments = ParseArguments();
        var closeToken = Accept(SyntaxKind.CloseParenToken);

        return new NewExpressionSyntax(
            _sourceFile,
            newToken,
            type,
            openToken,
            arguments,
            closeToken
        );
    }

    private LiteralExpressionSyntax ParseLiteralExpression()
    {
        var numberToken = Accept();
        return new LiteralExpressionSyntax(_sourceFile, numberToken);
    }

    // private ExpressionSyntax ParseMemberAccessOrAssignmentExpression()
    // {
    //     ExpressionSyntax name = ParseIdentifierName();
    //
    //     // while (CurrentKind == SyntaxKind.DotToken)
    //     // {
    //     //     var dot = Accept();
    //     //     var right = ParseIdentifierName();
    //     //
    //     //     name = new MemberAccessExpressionSyntax(_sourceFile, name, dot, right);
    //     // }
    //
    //     // if (CurrentKind == SyntaxKind.EqualsToken)
    //     //     return ParseAssignmentExpression(name);
    //     //
    //     // if (CurrentKind == SyntaxKind.OpenParenToken)
    //     //     return ParseCallExpression(name);
    //
    //     return name;
    // }


    private SimpleNameSyntax ParseSimpleName()
    {
        var ident = Accept(SyntaxKind.IdentifierToken);

        if (CurrentKind != SyntaxKind.LessThanToken)
            return new IdentifierNameSyntax(_sourceFile, ident);

        var typeArgumentList = ParseTypeArgumentList();

        return new GenericNameSyntax(_sourceFile, ident, typeArgumentList);
    }

    private TypeArgumentList ParseTypeArgumentList()
    {
        var items = ImmutableArray.CreateBuilder<SyntaxNode>();

        var lt = Accept(SyntaxKind.LessThanToken);

        while (
            CurrentKind != SyntaxKind.EndOfInputToken && CurrentKind != SyntaxKind.GreaterThanToken
        )
        {
            var currentToken = CurrentToken;
            var arg = ParseNameSyntax();
            items.Add(arg);

            if (CurrentKind == SyntaxKind.GreaterThanToken)
                break;

            var comma = Accept(SyntaxKind.CommaToken);
            items.Add(comma);
            if (CurrentToken == currentToken)
            {
                NextToken();
            }
        }

        var gt = Accept(SyntaxKind.GreaterThanToken);

        return new TypeArgumentList(
            _sourceFile,
            lt,
            new SeparatedSyntaxList<NameSyntax>(items.ToImmutable()),
            gt
        );
    }

    private IdentifierNameSyntax ParseIdentifierName()
    {
        var ident = Accept(SyntaxKind.IdentifierToken);
        return new IdentifierNameSyntax(_sourceFile, ident);
    }

    private ThisExpressionSyntax ParseThis()
    {
        var ident = Accept();
        return new ThisExpressionSyntax(_sourceFile, ident);
    }

    private ExpressionSyntax ParseNull()
    {
        var token = Accept();
        return new NullExpressionSyntax(_sourceFile, token);
    }

    private LiteralExpressionSyntax ParseBooleanLiteral()
    {
        var value = CurrentKind == SyntaxKind.TrueKeyword;
        return new LiteralExpressionSyntax(_sourceFile, Accept(), value);
    }

    private ExpressionSyntax ParseBlockExpression()
    {
        var statements = new List<StatementSyntax>();
        var openBraceToken = Accept(SyntaxKind.OpenBraceToken);
        while (true)
        {
            var currentToken = CurrentToken;
            if (
                currentToken.Kind == SyntaxKind.EndOfInputToken
                || currentToken.Kind == SyntaxKind.CloseBraceToken
            )
                break;

            statements.Add(ParseStatement());

            // prevent getting stuck in a loop as ParseStatement() does not always consume tokens
            if (currentToken == CurrentToken)
                NextToken();
        }

        var expr = (statements.LastOrDefault() as ExpressionStatementSyntax)?.Expression;
        var stmts = expr == null ? statements : statements.Take(statements.Count - 1);

        if (expr == null)
        {
            var openParenToken = Create(SyntaxKind.OpenParenToken, "(");
            var closeParenToken = Create(SyntaxKind.CloseParenToken, ")");
            expr = new UnitExpressionSyntax(_sourceFile, openParenToken, closeParenToken);
        }

        var closeBraceToken = Accept(SyntaxKind.CloseBraceToken);

        return new BlockExpressionSyntax(
            _sourceFile,
            openBraceToken,
            stmts.ToImmutableArray(),
            expr,
            closeBraceToken
        );
    }

    private StatementSyntax ParseStatement() =>
        CurrentKind switch
        {
            SyntaxKind.ValKeyword => ParseVariableDeclarationStatement(),
            SyntaxKind.VarKeyword => ParseVariableDeclarationStatement(),
            SyntaxKind.BreakKeyword => ParseBreakStatement(),
            SyntaxKind.ContinueKeyword => ParseContinueStatement(),
            _ => ParseExpressionStatement()
        };

    private bool IsEndOfInput => CurrentKind == SyntaxKind.EndOfInputToken;

    private StatementSyntax ParseContinueStatement()
    {
        var keyword = Accept();
        return new ContinueStatementSyntax(_sourceFile, keyword);
    }

    private StatementSyntax ParseBreakStatement()
    {
        var keyword = Accept();
        return new BreakStatementSyntax(_sourceFile, keyword);
    }

    private StatementSyntax ParseExpressionStatement()
    {
        var expr = ParseExpression(OperatorPrecedence.Lowest);
        AssertStatementTerminator(expr);
        return new ExpressionStatementSyntax(_sourceFile, expr);
    }

    private StatementSyntax ParseVariableDeclarationStatement()
    {
        var valToken = Accept();
        var identToken = Accept(SyntaxKind.IdentifierToken);
        var typeAnnotationSyntax = ParseOptionalTypeAnnotation();
        var initializer = ParseOptionalInitializer();

        var endOfStatement = (SyntaxNode?)initializer ?? typeAnnotationSyntax;
        if (endOfStatement != null)
        {
            AssertStatementTerminator(endOfStatement);
        }
        else
        {
            Diagnostics.ReportUndefinedTypeOrInitializer(identToken.Location);
        }

        return new VariableDeclarationStatementSyntax(
            _sourceFile,
            valToken,
            identToken,
            typeAnnotationSyntax,
            initializer
        );
    }

    private InitializerSyntax? ParseOptionalInitializer()
    {
        if (CurrentKind != SyntaxKind.EqualsToken)
            return null;

        var equals = Accept();
        var expression = ParseExpression(OperatorPrecedence.Lowest);

        return new InitializerSyntax(_sourceFile, equals, expression);
    }

    private ExpressionSyntax ParseAssignmentExpression(ExpressionSyntax name)
    {
        var equalsToken = Accept(SyntaxKind.EqualsToken);
        var expr = ParseExpression(OperatorPrecedence.Lowest);

        return new AssignmentExpressionSyntax(_sourceFile, name, equalsToken, expr);
    }

    private TypeAnnotationSyntax? ParseOptionalTypeAnnotation() =>
        CurrentKind == SyntaxKind.ColonToken ? ParseTypeAnnotation() : null;

    private TypeAnnotationSyntax ParseTypeAnnotation()
    {
        var colonToken = Accept(SyntaxKind.ColonToken);
        var type = ParseNameSyntax();

        return new TypeAnnotationSyntax(_sourceFile, colonToken, type);
    }
}
