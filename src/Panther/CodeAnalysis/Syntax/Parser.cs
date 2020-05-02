using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Syntax
{
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
        private readonly SyntaxTree _syntaxTree;
        public DiagnosticBag Diagnostics { get; } = new DiagnosticBag();

        private readonly Dictionary<SyntaxKind, PrefixParseFunction> _prefixParseFunctions = new Dictionary<SyntaxKind, PrefixParseFunction>();

        private readonly Dictionary<SyntaxKind, InfixParseFunction> _infixParseFunctions =
            new Dictionary<SyntaxKind, InfixParseFunction>();

        private ImmutableArray<SyntaxToken> _tokens;
        private int _tokenPosition = 0;

        public Parser(SyntaxTree syntaxTree)
        {
            var lexer = new Lexer(syntaxTree);
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
            _syntaxTree = syntaxTree;

            this.Diagnostics.AddRange(lexer.Diagnostics);

            // -a
            _prefixParseFunctions[SyntaxKind.BangToken] = ParsePrefixExpression;
            _prefixParseFunctions[SyntaxKind.DashToken] = ParsePrefixExpression;
            _prefixParseFunctions[SyntaxKind.PlusToken] = ParsePrefixExpression;
            _prefixParseFunctions[SyntaxKind.TildeToken] = ParsePrefixExpression;

            _prefixParseFunctions[SyntaxKind.IdentifierToken] = ParseNameOrAssignmentExpression;
            _prefixParseFunctions[SyntaxKind.StringToken] = ParseLiteralExpression;
            _prefixParseFunctions[SyntaxKind.NumberToken] = ParseLiteralExpression;
            _prefixParseFunctions[SyntaxKind.TrueKeyword] = ParseBooleanLiteral;
            _prefixParseFunctions[SyntaxKind.FalseKeyword] = ParseBooleanLiteral;
            _prefixParseFunctions[SyntaxKind.OpenParenToken] = ParseGroupOrUnitExpression;
            _prefixParseFunctions[SyntaxKind.IfKeyword] = ParseIfExpression;
            _prefixParseFunctions[SyntaxKind.WhileKeyword] = ParseWhileExpression;
            _prefixParseFunctions[SyntaxKind.ForKeyword] = ParseForExpression;
            _prefixParseFunctions[SyntaxKind.OpenBraceToken] = ParseBlockExpression;
            _prefixParseFunctions[SyntaxKind.BreakKeyword] = ParseBreakExpression;
            _prefixParseFunctions[SyntaxKind.ContinueKeyword] = ParseContinueExpression;

            // a + b
            _infixParseFunctions[SyntaxKind.AmpersandAmpersandToken] = ParseInfixExpression;
            _infixParseFunctions[SyntaxKind.AmpersandToken] = ParseInfixExpression;
            _infixParseFunctions[SyntaxKind.BangEqualsToken] = ParseInfixExpression;
            _infixParseFunctions[SyntaxKind.CaretToken] = ParseInfixExpression;
            _infixParseFunctions[SyntaxKind.EqualsEqualsToken] = ParseInfixExpression;
            _infixParseFunctions[SyntaxKind.GreaterThanEqualsToken] = ParseInfixExpression;
            _infixParseFunctions[SyntaxKind.GreaterThanToken] = ParseInfixExpression;
            _infixParseFunctions[SyntaxKind.LessThanEqualsToken] = ParseInfixExpression;
            _infixParseFunctions[SyntaxKind.LessThanToken] = ParseInfixExpression;
            _infixParseFunctions[SyntaxKind.DashToken] = ParseInfixExpression;
            _infixParseFunctions[SyntaxKind.PipePipeToken] = ParseInfixExpression;
            _infixParseFunctions[SyntaxKind.PipeToken] = ParseInfixExpression;
            _infixParseFunctions[SyntaxKind.PlusToken] = ParseInfixExpression;
            _infixParseFunctions[SyntaxKind.SlashToken] = ParseInfixExpression;
            _infixParseFunctions[SyntaxKind.StarToken] = ParseInfixExpression;
            //InfixParseFunctions[SyntaxKind.LeftBracket] = ParseIndexExpression;
        }

        private SyntaxToken PeekToken
        {
            get
            {
                var pos = _tokenPosition;

                TokenFromPosition(ref pos);
                pos++;
                return TokenFromPosition(ref pos);
            }
        }

        private SyntaxToken CurrentToken
        {
            get
            {
                var pos = _tokenPosition;
                return TokenFromPosition(ref pos);
            }
        }

        private SyntaxToken TokenFromPosition(ref int pos)
        {
            while (true)
            {
                if (pos > _tokens.Length - 1)
                    return _tokens[^1];

                var token = _tokens[pos];

                if (token.Kind == SyntaxKind.WhitespaceTrivia || token.Kind == SyntaxKind.InvalidToken)
                {
                    pos++;
                    continue;
                }

                return token;
            }
        }

        private void NextToken()
        {
            // get position of current token
            var pos = _tokenPosition;
            TokenFromPosition(ref pos);

            // set next position to pos + 1
            _tokenPosition = pos + 1;
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

            return new SyntaxToken(_syntaxTree, kind, currentToken.Position);
        }

        private SyntaxToken Create(SyntaxKind kind, string text)
        {
            var currentToken = CurrentToken;

            return new SyntaxToken(_syntaxTree, kind, currentToken.Position, text, null);
        }

        public CompilationUnitSyntax ParseCompilationUnit()
        {
            var members = ParseMembers();

            var endToken = Accept(SyntaxKind.EndOfInputToken);

            return new CompilationUnitSyntax(_syntaxTree, members, endToken);
        }

        private ImmutableArray<MemberSyntax> ParseMembers()
        {
            var members = ImmutableArray.CreateBuilder<MemberSyntax>();
            while (CurrentToken.Kind != SyntaxKind.EndOfInputToken)
            {
                var startToken = CurrentToken;

                var member = ParseMember();
                members.Add(member);

                if (CurrentToken == startToken)
                    NextToken();
            }

            return members.ToImmutable();
        }

        private MemberSyntax ParseMember()
        {
            if (CurrentToken.Kind == SyntaxKind.DefKeyword)
            {
                return ParseFunctionDeclaration();
            }

            return ParseGlobalStatement();
        }

        private MemberSyntax ParseGlobalStatement()
        {
            var statement = ParseStatement();
            return new GlobalStatementSyntax(_syntaxTree, statement);
        }

        private MemberSyntax ParseFunctionDeclaration()
        {
            var defKeyword = Accept();
            var identifier = Accept(SyntaxKind.IdentifierToken);

            var openParenToken = Accept(SyntaxKind.OpenParenToken);
            var parameters = ParseParameterList();
            var closeParenToken = Accept(SyntaxKind.CloseParenToken);
            var typeAnnotation = ParseOptionalTypeAnnotation();
            var equalsToken = Accept(SyntaxKind.EqualsToken);
            var body = ParseExpression(OperatorPrecedence.Lowest);

            return new FunctionDeclarationSyntax(_syntaxTree, defKeyword, identifier, openParenToken, parameters, closeParenToken, typeAnnotation, equalsToken, body);
        }

        private SeparatedSyntaxList<ParameterSyntax> ParseParameterList()
        {
            if (CurrentToken.Kind == SyntaxKind.CloseParenToken)
                return new SeparatedSyntaxList<ParameterSyntax>(ImmutableArray<SyntaxNode>.Empty);

            var items = ImmutableArray.CreateBuilder<SyntaxNode>();

            while (CurrentToken.Kind != SyntaxKind.EndOfInputToken)
            {
                var currentToken = CurrentToken;
                var arg = ParseParameter();
                items.Add(arg);

                if (CurrentToken.Kind == SyntaxKind.CloseParenToken)
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

            return new ParameterSyntax(_syntaxTree, ident, typeAnnotation);
        }

        private ExpressionSyntax ParseExpression(OperatorPrecedence precedence, bool inGroup = false)
        {
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
            var isTerminatingLine = !inGroup && left.Descendants().Select(x => x.Kind == SyntaxKind.EndOfLineTrivia).LastOrDefault();
            if (left.Kind == SyntaxKind.ContinueExpression || left.Kind == SyntaxKind.BreakExpression || isTerminatingLine)
                return left;

            while (precedence < CurrentPrecedence())
            {
                var infix = _infixParseFunctions.GetValueOrDefault(CurrentToken.Kind);
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

            return new UnaryExpressionSyntax(_syntaxTree, unaryOperatorToken, expression);
        }

        private OperatorPrecedence CurrentPrecedence() =>
            CurrentToken.Kind.GetBinaryOperatorPrecedence() ?? OperatorPrecedence.Lowest;

        private ExpressionSyntax ParseInfixExpression(ExpressionSyntax left)
        {
            // parse expressions like:
            // left + right
            var precedence = CurrentPrecedence();
            var binaryOperatorToken = Accept();
            var right = ParseExpression(precedence);

            return new BinaryExpressionSyntax(_syntaxTree, left, binaryOperatorToken, right);
        }

        private ExpressionSyntax ParseWhileExpression()
        {
            var whileKeyword = Accept();
            var openParenToken = Accept(SyntaxKind.OpenParenToken);
            var condition = ParseExpression(OperatorPrecedence.Lowest);
            var closeParenToken = Accept(SyntaxKind.CloseParenToken);

            var expr = ParseExpression(OperatorPrecedence.Lowest);

            return new WhileExpressionSyntax(_syntaxTree, whileKeyword, openParenToken, condition, closeParenToken, expr);
        }

        private ExpressionSyntax ParseForExpression()
        {
            var forKeyword = Accept();
            var openParenToken = Accept(SyntaxKind.OpenParenToken);
            var variable = ParseNameExpression(true);
            var leftArrow = Accept(SyntaxKind.LessThanDashToken);

            var fromExpression = ParseExpression(OperatorPrecedence.Lowest);
            var toKeyword = Accept(SyntaxKind.ToKeyword);
            var toExpression = ParseExpression(OperatorPrecedence.Lowest);
            var closeParenToken = Accept(SyntaxKind.CloseParenToken);

            var expr = ParseExpression(OperatorPrecedence.Lowest);

            return new ForExpressionSyntax(_syntaxTree, forKeyword, openParenToken, variable, leftArrow, fromExpression, toKeyword, toExpression, closeParenToken, expr);
        }

        private ExpressionSyntax ParseCallExpression()
        {
            var ident = Accept();
            var openParenToken = Accept(SyntaxKind.OpenParenToken);
            var arguments = ParseArguments();
            var closeParenToken = Accept(SyntaxKind.CloseParenToken);

            return new CallExpressionSyntax(_syntaxTree, ident, openParenToken, arguments, closeParenToken);
        }

        private SeparatedSyntaxList<ExpressionSyntax> ParseArguments()
        {
            var items = new List<SyntaxNode>();

            if (CurrentToken.Kind == SyntaxKind.CloseParenToken)
                return new SeparatedSyntaxList<ExpressionSyntax>(ImmutableArray<SyntaxNode>.Empty);

            while (true)
            {
                var arg = ParseExpression(OperatorPrecedence.Lowest);
                items.Add(arg);

                var currentToken = CurrentToken;
                if (currentToken.Kind == SyntaxKind.CloseParenToken || currentToken.Kind == SyntaxKind.EndOfInputToken)
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

            return new IfExpressionSyntax(_syntaxTree, ifKeyword, openParenToken, condition, closeParenToken, thenExpr, elseKeyword, elseExpr);
        }

        private ExpressionSyntax ParseGroupOrUnitExpression()
        {
            var open = Accept();
            if (CurrentToken.Kind == SyntaxKind.CloseParenToken)
            {
                // unit expression
                var close = Accept(SyntaxKind.CloseParenToken);
                return new UnitExpressionSyntax(_syntaxTree, open, close);
            }
            else
            {
                var expr = ParseExpression(OperatorPrecedence.Lowest, true);
                var close = Accept(SyntaxKind.CloseParenToken);

                return new GroupExpressionSyntax(_syntaxTree, open, expr, close);
            }
        }

        private LiteralExpressionSyntax ParseLiteralExpression()
        {
            var numberToken = Accept();
            return new LiteralExpressionSyntax(_syntaxTree, numberToken);
        }

        private ExpressionSyntax ParseNameOrAssignmentExpression()
        {
            if (PeekTokenExact.Kind == SyntaxKind.EqualsToken)
                return ParseAssignmentExpression();

            if (PeekTokenExact.Kind == SyntaxKind.OpenParenToken)
                return ParseCallExpression();

            return ParseNameExpression(false);
        }

        private SyntaxToken PeekTokenExact => PeekToken;

        private NameExpressionSyntax ParseNameExpression(bool skipNewLines)
        {
            var token = Accept();

            return new NameExpressionSyntax(_syntaxTree, token);
        }

        private LiteralExpressionSyntax ParseBooleanLiteral()
        {
            var value = CurrentToken.Kind == SyntaxKind.TrueKeyword;
            return new LiteralExpressionSyntax(_syntaxTree, Accept(), value);
        }

        private ExpressionSyntax ParseBlockExpression()
        {
            var statements = new List<StatementSyntax>();
            var openBraceToken = Accept(SyntaxKind.OpenBraceToken);
            while (true)
            {
                var currentToken = CurrentToken;
                if (currentToken.Kind == SyntaxKind.EndOfInputToken || currentToken.Kind == SyntaxKind.CloseBraceToken)
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
                expr = new UnitExpressionSyntax(_syntaxTree, openParenToken, closeParenToken);
            }

            var closeBraceToken = Accept(SyntaxKind.CloseBraceToken);

            return new BlockExpressionSyntax(_syntaxTree, openBraceToken, stmts.ToImmutableArray(), expr, closeBraceToken);
        }

        private StatementSyntax ParseStatement()
        {
            switch (CurrentToken.Kind)
            {
                case SyntaxKind.ValKeyword:
                case SyntaxKind.VarKeyword:
                    return ParseVariableDeclarationStatement();

                default:
                    return ParseExpressionStatement();
            }
        }

        private ExpressionSyntax ParseContinueExpression()
        {
            var keyword = Accept();
            return new ContinueExpressionSyntax(_syntaxTree, keyword);
        }

        private ExpressionSyntax ParseBreakExpression()
        {
            var keyword = Accept();
            return new BreakExpressionSyntax(_syntaxTree, keyword);
        }

        private StatementSyntax ParseExpressionStatement()
        {
            var expr = ParseExpression(OperatorPrecedence.Lowest);
            AssertStatementTerminator(expr);
            return new ExpressionStatementSyntax(_syntaxTree, expr);
        }

        private void AssertStatementTerminator(ExpressionSyntax expr)
        {
            var lastKind = expr.Descendants().Last(token => token.Kind != SyntaxKind.WhitespaceTrivia).Kind;

            if (lastKind != SyntaxKind.EndOfLineTrivia && lastKind != SyntaxKind.CloseBraceToken && CurrentToken.Kind != SyntaxKind.EndOfInputToken)
            {
                throw new Exception("expected end of line trivia");
            }
        }

        private StatementSyntax ParseVariableDeclarationStatement()
        {
            var valToken = Accept();
            var identToken = Accept(SyntaxKind.IdentifierToken);
            var typeAnnotationSyntax = ParseOptionalTypeAnnotation();

            var equalsToken = Accept(SyntaxKind.EqualsToken);
            var expr = ParseExpression(OperatorPrecedence.Lowest);

            AssertStatementTerminator(expr);

            return new VariableDeclarationStatementSyntax(_syntaxTree, valToken, identToken, typeAnnotationSyntax, equalsToken, expr);
        }

        private ExpressionSyntax ParseAssignmentExpression()
        {
            var identToken = Accept(SyntaxKind.IdentifierToken);
            var equalsToken = Accept(SyntaxKind.EqualsToken);
            var expr = ParseExpression(OperatorPrecedence.Lowest);

            return new AssignmentExpressionSyntax(_syntaxTree, identToken, equalsToken, expr);
        }

        private TypeAnnotationSyntax? ParseOptionalTypeAnnotation() =>
            CurrentToken.Kind == SyntaxKind.ColonToken ? ParseTypeAnnotation() : null;

        private TypeAnnotationSyntax ParseTypeAnnotation()
        {
            var colonToken = Accept(SyntaxKind.ColonToken);
            var identToken = Accept(SyntaxKind.IdentifierToken);

            return new TypeAnnotationSyntax(_syntaxTree, colonToken, identToken);
        }
    }
}