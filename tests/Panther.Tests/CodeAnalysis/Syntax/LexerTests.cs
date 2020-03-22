using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using Panther.CodeAnalysis.Syntax;
using Xunit;

namespace Panther.Tests.CodeAnalysis.Syntax
{
    public class TokenTestData
    {
        public SyntaxKind Kind { get; }
        public string Text { get; }

        public TokenTestData(SyntaxKind kind, string text)
        {
            Kind = kind;
            Text = text;
        }
    }

    public class NonSeparatorTokenTestData : TokenTestData
    {
        public NonSeparatorTokenTestData(SyntaxKind kind, string text)
            : base(kind, text)
        {
        }
    }

    public class SeparatorTokenTestData : TokenTestData
    {
        public SeparatorTokenTestData(SyntaxKind kind, string text)
            : base(kind, text)
        {
        }
    }

    public class TokenPairTestData
    {
        public TokenTestData Token1 { get; }
        public TokenTestData Token2 { get; }

        public TokenPairTestData(NonSeparatorTokenTestData token1, NonSeparatorTokenTestData token2)
        {
            Token1 = token1;
            Token2 = token2;
        }
    }

    public class LexerGenerators
    {
        public static Arbitrary<TokenTestData> TokenTestData() => Gen
            .OneOf(
                Arb.Generate<NonSeparatorTokenTestData>().Select(x => (TokenTestData)x),
                Arb.Generate<SeparatorTokenTestData>().Select(x => (TokenTestData)x)
            )
            .ToArbitrary();

        public static Arbitrary<NonSeparatorTokenTestData> NonSeparatorTokenTestData() =>
            Gen.Elements(
                new NonSeparatorTokenTestData(SyntaxKind.IdentifierToken, "a"),
                new NonSeparatorTokenTestData(SyntaxKind.IdentifierToken, "abc"),
                new NonSeparatorTokenTestData(SyntaxKind.NumberToken, "1"),
                new NonSeparatorTokenTestData(SyntaxKind.NumberToken, "123"),
                new NonSeparatorTokenTestData(SyntaxKind.NumberToken, "0"),
                new NonSeparatorTokenTestData(SyntaxKind.TrueKeyword, "true"),
                new NonSeparatorTokenTestData(SyntaxKind.FalseKeyword, "false"),
                new NonSeparatorTokenTestData(SyntaxKind.ValKeyword, "val"),
                new NonSeparatorTokenTestData(SyntaxKind.PlusToken, "+"),
                new NonSeparatorTokenTestData(SyntaxKind.MinusToken, "-"),
                new NonSeparatorTokenTestData(SyntaxKind.SlashToken, "/"),
                new NonSeparatorTokenTestData(SyntaxKind.StarToken, "*"),
                new NonSeparatorTokenTestData(SyntaxKind.BangToken, "!"),
                new NonSeparatorTokenTestData(SyntaxKind.AmpersandAmpersandToken, "&&"),
                new NonSeparatorTokenTestData(SyntaxKind.PipePipeToken, "||"),
                new NonSeparatorTokenTestData(SyntaxKind.BangEqualsToken, "!="),
                new NonSeparatorTokenTestData(SyntaxKind.EqualsToken, "="),
                new NonSeparatorTokenTestData(SyntaxKind.EqualsEqualsToken, "=="),
                new NonSeparatorTokenTestData(SyntaxKind.CloseParenToken, ")"),
                new NonSeparatorTokenTestData(SyntaxKind.OpenParenToken, "(")
            ).ToArbitrary();

        public static Arbitrary<TokenPairTestData> TokenPairTestData()
        {
            var gen = from t1 in Arb.Generate<NonSeparatorTokenTestData>()
                      from t2 in Arb.Generate<NonSeparatorTokenTestData>()
                      where !TokensCoalesce(t1.Kind, t2.Kind)
                      select new TokenPairTestData(t1, t2);

            return gen.ToArbitrary();
        }

        public static Arbitrary<SeparatorTokenTestData> SeparatorTokenTestData()
        {
            return Gen.Elements(
                new SeparatorTokenTestData(SyntaxKind.WhitespaceToken, " "),
                new SeparatorTokenTestData(SyntaxKind.WhitespaceToken, "\t"),
                new SeparatorTokenTestData(SyntaxKind.WhitespaceToken, "\n"),
                new SeparatorTokenTestData(SyntaxKind.WhitespaceToken, "\r"),
                new SeparatorTokenTestData(SyntaxKind.WhitespaceToken, "\r\n")
            ).ToArbitrary();
        }

        private static readonly SyntaxKind[] CoalescingKinds = new[]
        {
            SyntaxKind.NumberToken, SyntaxKind.EqualsToken, SyntaxKind.IdentifierToken
        };

        private static bool TokensCoalesce(SyntaxKind kind1, SyntaxKind kind2)
        {
            if (kind1 == kind2 && CoalescingKinds.Contains(kind1))
                return true;

            var kind1Keyword = kind1.ToString().EndsWith("Keyword");
            var kind2Keyword = kind2.ToString().EndsWith("Keyword");

            if (kind1Keyword && (kind2Keyword || kind2 == SyntaxKind.IdentifierToken))
                return true;

            if (kind1 == SyntaxKind.IdentifierToken && kind2Keyword)
                return true;

            // '!' + '==' => '!=' + '='
            if (kind1 == SyntaxKind.BangToken && kind2 == SyntaxKind.EqualsEqualsToken)
                return true;

            // '!' + '=' => '!='
            if (kind1 == SyntaxKind.BangToken && kind2 == SyntaxKind.EqualsToken)
                return true;

            // '=' + '==' => '==' + '='
            if (kind1 == SyntaxKind.EqualsToken && kind2 == SyntaxKind.EqualsEqualsToken)
                return true;

            return false;
        }
    }

    [Properties(Arbitrary = new[] { typeof(LexerGenerators) })]
    public class LexerTests
    {
        [Property]
        public void LexerLexesToken(TokenTestData test)
        {
            var tokens = SyntaxTree.ParseTokens(test.Text);

            var token = Assert.Single(tokens);

            Assert.NotNull(token);
            Assert.Equal(test.Kind, token.Kind);
            Assert.Equal(test.Text, token.Text);
        }

        [Property]
        public void LexerLexesTwoTokens(TokenPairTestData testData)
        {
            var tokens = SyntaxTree.ParseTokens(testData.Token1.Text + testData.Token2.Text);

            Assert.Collection(tokens, token1 =>
            {
                Assert.NotNull(token1);
                Assert.Equal(testData.Token1.Kind, token1.Kind);
                Assert.Equal(testData.Token1.Text, token1.Text);
            }, token2 =>
            {
                Assert.NotNull(token2);
                Assert.Equal(testData.Token2.Kind, token2.Kind);
                Assert.Equal(testData.Token2.Text, token2.Text);
            });
        }

        [Property]
        public void LexerLexesTwoTokensWithSeparator(NonSeparatorTokenTestData expectedToken1, SeparatorTokenTestData expectedSepToken, NonSeparatorTokenTestData expectedToken2)
        {
            var tokens = SyntaxTree.ParseTokens(expectedToken1.Text + expectedSepToken.Text + expectedToken2.Text);

            Assert.Collection(tokens, token1 =>
            {
                Assert.NotNull(token1);
                Assert.Equal(expectedToken1.Kind, token1.Kind);
                Assert.Equal(expectedToken1.Text, token1.Text);
            }, sepToken =>
            {
                Assert.NotNull(sepToken);
                Assert.Equal(expectedSepToken.Kind, sepToken.Kind);
                Assert.Equal(expectedSepToken.Text, sepToken.Text);
            }, token2 =>
            {
                Assert.NotNull(token2);
                Assert.Equal(expectedToken2.Kind, token2.Kind);
                Assert.Equal(expectedToken2.Text, token2.Text);
            });
        }
    }
}