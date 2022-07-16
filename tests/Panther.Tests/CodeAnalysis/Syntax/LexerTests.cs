using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck.Xunit;
using Panther.CodeAnalysis.Syntax;
using Xunit;

namespace Panther.Tests.CodeAnalysis.Syntax;

[Properties(Arbitrary = new[] { typeof(TokenGenerators) }, MaxTest = 20)]
public class LexerTests
{
    [Property]
    public void LexerLexesToken(NonSeparatorTokenTestData test)
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

        Assert.Collection(
            tokens,
            token1 =>
            {
                Assert.NotNull(token1);
                Assert.Equal(testData.Token1.Kind, token1.Kind);
                Assert.Equal(testData.Token1.Text, token1.Text);
            },
            token2 =>
            {
                Assert.NotNull(token2);
                Assert.Equal(testData.Token2.Kind, token2.Kind);
                Assert.Equal(testData.Token2.Text, token2.Text);
            }
        );
    }

    [Property]
    public void LexerLexesTwoTokensWithSeparator(
        NonSeparatorTokenTestData expectedToken1,
        SeparatorTokenTestData expectedSepToken,
        NonSeparatorTokenTestData expectedToken2
    )
    {
        var tokens = SyntaxTree.ParseTokens(
            expectedToken1.Text + expectedSepToken.Text + expectedToken2.Text
        );

        Assert.Collection(
            tokens,
            token1 =>
            {
                Assert.NotNull(token1);
                Assert.Equal(expectedToken1.Kind, token1.Kind);
                Assert.Equal(expectedToken1.Text, token1.Text);

                Assert.Collection(
                    token1.TrailingTrivia,
                    sepToken =>
                    {
                        Assert.NotNull(sepToken);
                        Assert.Equal(expectedSepToken.Kind, sepToken.Kind);
                        Assert.Equal(expectedSepToken.Text, sepToken.Text);
                    }
                );
            },
            token2 =>
            {
                Assert.NotNull(token2);
                Assert.Equal(expectedToken2.Kind, token2.Kind);
                Assert.Equal(expectedToken2.Text, token2.Text);
            }
        );
    }

    [Property]
    public void CanLexComment(LineCommentTriviaData lineCommentTrivia)
    {
        var text = AnnotatedText
            .Parse(
                $@"
            {lineCommentTrivia.Text}
            1
"
            )
            .Text;
        var tokens = SyntaxTree.ParseTokens(text);

        Assert.Collection(
            tokens,
            token1 =>
            {
                Assert.NotNull(token1);
                Assert.Collection(
                    token1.LeadingTrivia,
                    trivia =>
                    {
                        Assert.Equal(SyntaxKind.LineCommentTrivia, trivia.Kind);
                        Assert.Equal(lineCommentTrivia.Text, trivia.Text);
                    },
                    trivia => { }
                );
                Assert.Equal(SyntaxKind.NumberToken, token1.Kind);
                Assert.Equal("1", token1.Text);
            }
        );
    }
}
