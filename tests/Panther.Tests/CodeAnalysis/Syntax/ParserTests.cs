using System;
using FsCheck.Xunit;
using Panther.CodeAnalysis.Syntax;
using Xunit;

namespace Panther.Tests.CodeAnalysis.Syntax
{
    [Properties(Arbitrary = new[] { typeof(TokenGenerators) })]
    public class ParserTests
    {
        [Property]
        public void BinaryExpressionHonorsPrecedences(BinaryOperatorSyntaxKind op1, BinaryOperatorSyntaxKind op2)
        {
            var op1Precedence = op1.Kind.GetBinaryOperatorPrecedence();
            var op2Precedence = op2.Kind.GetBinaryOperatorPrecedence();

            var op1Text = SyntaxFacts.GetText(op1.Kind) ?? throw new Exception("Invalid operator");
            var op2Text = SyntaxFacts.GetText(op2.Kind) ?? throw new Exception("Invalid operator");

            var text = $"a {op1Text} b {op2Text} c";
            var expression = SyntaxTree.Parse(text).Root;

            if (op1Precedence >= op2Precedence)
            {
                using var e = new AssertingEnumerator(expression);
                // └──BinaryExpression
                //    ├──BinaryExpression
                //    │   ├──NameExpression
                //    │   │   └──IdentifierToken
                //    │   ├──PlusToken
                //    │   └──NameExpression
                //    │       └──IdentifierToken
                //    ├──PlusToken
                //    └──NameExpression
                //        └──IdentifierToken
                e.AssertNode(SyntaxKind.CompilationUnit);
                e.AssertNode(SyntaxKind.GlobalStatement);
                e.AssertNode(SyntaxKind.ExpressionStatement);
                e.AssertNode(SyntaxKind.BinaryExpression);
                e.AssertNode(SyntaxKind.BinaryExpression);
                e.AssertNode(SyntaxKind.NameExpression);
                e.AssertToken(SyntaxKind.IdentifierToken, "a");
                e.AssertToken(op1.Kind, op1Text);
                e.AssertNode(SyntaxKind.NameExpression);
                e.AssertToken(SyntaxKind.IdentifierToken, "b");
                e.AssertToken(op2.Kind, op2Text);
                e.AssertNode(SyntaxKind.NameExpression);
                e.AssertToken(SyntaxKind.IdentifierToken, "c");
                e.AssertToken(SyntaxKind.EndOfInputToken, "");
            }
            else
            {
                using var e = new AssertingEnumerator(expression);
                // └──BinaryExpression
                //    ├──NameExpression
                //    │   └──IdentifierToken
                //    ├──PlusToken
                //    └──BinaryExpression
                //       ├──NameExpression
                //       │   └──IdentifierToken
                //       ├──StarToken
                //       └──NameExpression
                //          └──IdentifierToken

                e.AssertNode(SyntaxKind.CompilationUnit);
                e.AssertNode(SyntaxKind.GlobalStatement);
                e.AssertNode(SyntaxKind.ExpressionStatement);
                e.AssertNode(SyntaxKind.BinaryExpression);
                e.AssertNode(SyntaxKind.NameExpression);
                e.AssertToken(SyntaxKind.IdentifierToken, "a");
                e.AssertToken(op1.Kind, op1Text);
                e.AssertNode(SyntaxKind.BinaryExpression);
                e.AssertNode(SyntaxKind.NameExpression);
                e.AssertToken(SyntaxKind.IdentifierToken, "b");
                e.AssertToken(op2.Kind, op2Text);
                e.AssertNode(SyntaxKind.NameExpression);
                e.AssertToken(SyntaxKind.IdentifierToken, "c");
                e.AssertToken(SyntaxKind.EndOfInputToken, "");
            }
        }

        [Property]
        public void UnaryExpressionHonorsPrecedences(UnaryOperatorSyntaxKind op1, BinaryOperatorSyntaxKind op2)
        {
            var unaryText = SyntaxFacts.GetText(op1.Kind);
            var binaryText = SyntaxFacts.GetText(op2.Kind);

            var text = $"{unaryText} a {binaryText} b";
            var expression = SyntaxTree.Parse(text).Root;

            using var e = new AssertingEnumerator(expression);

            // └──BinaryExpression
            //     ├──UnaryExpression
            //     │   ├──MinusToken
            //     │   └──NameExpression
            //     │       └──IdentifierToken
            //     ├──PlusToken
            //     └──NameExpression
            //         └──IdentifierToken

            e.AssertNode(SyntaxKind.CompilationUnit);
            e.AssertNode(SyntaxKind.GlobalStatement);
            e.AssertNode(SyntaxKind.ExpressionStatement);
            e.AssertNode(SyntaxKind.BinaryExpression);
            e.AssertNode(SyntaxKind.UnaryExpression);
            e.AssertToken(op1.Kind, unaryText);
            e.AssertNode(SyntaxKind.NameExpression);
            e.AssertToken(SyntaxKind.IdentifierToken, "a");
            e.AssertToken(op2.Kind, binaryText);
            e.AssertNode(SyntaxKind.NameExpression);
            e.AssertToken(SyntaxKind.IdentifierToken, "b");
            e.AssertToken(SyntaxKind.EndOfInputToken, "");
        }

        [Fact]
        public void ParseNestedBlockExpression()
        {
            var text = "{{}}";
            var expression = SyntaxTree.Parse(text).Root;

            using var e = new AssertingEnumerator(expression);

            //└──ExpressionStatement
            //    ├──BlockExpression
            //    │   ├──OpenBraceToken
            //    │   ├──BlockExpression
            //    │   │   ├──OpenBraceToken
            //    │   │   ├──UnitExpression
            //    │   │   │   ├──CloseBraceToken
            //    │   │   │   └──CloseBraceToken
            //    │   │   └──CloseBraceToken
            //    │   └──CloseBraceToken
            //    └──NewLineToken

            e.AssertNode(SyntaxKind.CompilationUnit);
            e.AssertNode(SyntaxKind.GlobalStatement);
            e.AssertNode(SyntaxKind.ExpressionStatement);
            e.AssertNode(SyntaxKind.BlockExpression);
            e.AssertToken(SyntaxKind.OpenBraceToken, "{");
            e.AssertNode(SyntaxKind.BlockExpression);
            e.AssertToken(SyntaxKind.OpenBraceToken, "{");
            e.AssertNode(SyntaxKind.UnitExpression);
            e.AssertToken(SyntaxKind.OpenParenToken, "(");
            e.AssertToken(SyntaxKind.CloseParenToken, ")");
            e.AssertToken(SyntaxKind.CloseBraceToken, "}");
            e.AssertToken(SyntaxKind.CloseBraceToken, "}");
            e.AssertToken(SyntaxKind.EndOfInputToken, "");
        }

        [Fact]
        public void ParseNestedNonUnitBlockExpression()
        {
            var text = @"{{
                            val x = 5
                            5
                         }}";
            var tree = SyntaxTree.Parse(text);
            Assert.Empty(tree.Diagnostics);

            var expression = tree.Root;

            using var e = new AssertingEnumerator(expression);

            //└──ExpressionStatement
            //    ├──BlockExpression
            //    │   ├──OpenBraceToken
            //    │   ├──BlockExpression
            //    │   │   ├──OpenBraceToken
            //    │   │   ├──UnitExpression
            //    │   │   │   ├──CloseBraceToken
            //    │   │   │   └──CloseBraceToken
            //    │   │   └──CloseBraceToken
            //    │   └──CloseBraceToken
            //    └──NewLineToken

            e.AssertNode(SyntaxKind.CompilationUnit);
            e.AssertNode(SyntaxKind.GlobalStatement);
            e.AssertNode(SyntaxKind.ExpressionStatement);
            e.AssertNode(SyntaxKind.BlockExpression);
            e.AssertToken(SyntaxKind.OpenBraceToken, "{");
            e.AssertNode(SyntaxKind.BlockExpression);
            e.AssertToken(SyntaxKind.OpenBraceToken, "{");
            e.AssertNode(SyntaxKind.VariableDeclarationStatement);
            e.AssertToken(SyntaxKind.ValKeyword, "val");
            e.AssertToken(SyntaxKind.IdentifierToken, "x");
            e.AssertToken(SyntaxKind.EqualsToken, "=");
            e.AssertNode(SyntaxKind.LiteralExpression);
            e.AssertToken(SyntaxKind.NumberToken, "5");
            e.AssertToken(token =>
            {
                Assert.Equal(SyntaxKind.NewLineToken, token.Kind);
                Assert.True(token.Text == "\r\n" || token.Text == "\n");
            });
            e.AssertNode(SyntaxKind.LiteralExpression);
            e.AssertToken(SyntaxKind.NumberToken, "5");
            e.AssertToken(SyntaxKind.CloseBraceToken, "}");
            e.AssertToken(SyntaxKind.CloseBraceToken, "}");
            e.AssertToken(SyntaxKind.EndOfInputToken, "");
        }

        [Fact]
        public void ParseForExpression()
        {
            var text = @"for (x <- 0 to 5) x";
            var tree = SyntaxTree.Parse(text);
            Assert.Empty(tree.Diagnostics);

            var expression = tree.Root;

            using var e = new AssertingEnumerator(expression);

            //└──CompilationUnit
            //    ├──ExpressionStatement
            //    │   ├──ForExpression
            //    │   │   ├──ForKeyword
            //    │   │   ├──OpenParenToken
            //    │   │   ├──NameExpression
            //    │   │   │   └──IdentifierToken
            //    │   │   ├──LessThanDashToken
            //    │   │   ├──LiteralExpression
            //    │   │   │   └──NumberToken 0
            //    │   │   ├──ToKeyword
            //    │   │   ├──LiteralExpression
            //    │   │   │   └──NumberToken 5
            //    │   │   ├──CloseParenToken
            //    │   │   └──NameExpression
            //    │   │       └──IdentifierToken
            //    │   └──NewLineToken
            //    └──EndOfInputToken
            e.AssertNode(SyntaxKind.CompilationUnit);
            e.AssertNode(SyntaxKind.GlobalStatement);
            e.AssertNode(SyntaxKind.ExpressionStatement);
            e.AssertNode(SyntaxKind.ForExpression);
            e.AssertToken(SyntaxKind.ForKeyword, "for");
            e.AssertToken(SyntaxKind.OpenParenToken, "(");
            e.AssertNode(SyntaxKind.NameExpression);
            e.AssertToken(SyntaxKind.IdentifierToken, "x");
            e.AssertToken(SyntaxKind.LessThanDashToken, "<-");
            e.AssertNode(SyntaxKind.LiteralExpression);
            e.AssertToken(SyntaxKind.NumberToken, "0");
            e.AssertToken(SyntaxKind.ToKeyword, "to");
            e.AssertNode(SyntaxKind.LiteralExpression);
            e.AssertToken(SyntaxKind.NumberToken, "5");
            e.AssertToken(SyntaxKind.CloseParenToken, ")");
            e.AssertNode(SyntaxKind.NameExpression);
            e.AssertToken(SyntaxKind.IdentifierToken, "x");
            e.AssertToken(SyntaxKind.EndOfInputToken, "");
        }


        [Fact]
        public void ParseForExpressionWithLineBreaks()
        {
            var text = AnnotatedText.Parse(@"
                for (x <- 0 to 5)
                    x
            ");
            var tree = SyntaxTree.Parse(text.Text);
            Assert.Empty(tree.Diagnostics);

            var expression = tree.Root;

            using var e = new AssertingEnumerator(expression);

            //└──CompilationUnit
            //    ├──ExpressionStatement
            //    │   ├──ForExpression
            //    │   │   ├──ForKeyword
            //    │   │   ├──OpenParenToken
            //    │   │   ├──NameExpression
            //    │   │   │   └──IdentifierToken
            //    │   │   ├──LessThanDashToken
            //    │   │   ├──LiteralExpression
            //    │   │   │   └──NumberToken 0
            //    │   │   ├──ToKeyword
            //    │   │   ├──LiteralExpression
            //    │   │   │   └──NumberToken 5
            //    │   │   ├──CloseParenToken
            //    │   │   └──NameExpression
            //    │   │       └──IdentifierToken
            //    │   └──NewLineToken
            //    └──EndOfInputToken
            e.AssertNode(SyntaxKind.CompilationUnit);
            e.AssertNode(SyntaxKind.GlobalStatement);
            e.AssertNode(SyntaxKind.ExpressionStatement);
            e.AssertNode(SyntaxKind.ForExpression);
            e.AssertToken(SyntaxKind.ForKeyword, "for");
            e.AssertToken(SyntaxKind.OpenParenToken, "(");
            e.AssertNode(SyntaxKind.NameExpression);
            e.AssertToken(SyntaxKind.IdentifierToken, "x");
            e.AssertToken(SyntaxKind.LessThanDashToken, "<-");
            e.AssertNode(SyntaxKind.LiteralExpression);
            e.AssertToken(SyntaxKind.NumberToken, "0");
            e.AssertToken(SyntaxKind.ToKeyword, "to");
            e.AssertNode(SyntaxKind.LiteralExpression);
            e.AssertToken(SyntaxKind.NumberToken, "5");
            e.AssertToken(SyntaxKind.CloseParenToken, ")");
            e.AssertNode(SyntaxKind.NameExpression);
            e.AssertToken(SyntaxKind.IdentifierToken, "x");
            e.AssertToken(SyntaxKind.EndOfInputToken, "");
        }

        [Fact]
        public void ParseWhileExpression()
        {
            var text = @"{
                            while ( true ) 1
                         }";
            var tree = SyntaxTree.Parse(text);
            Assert.Empty(tree.Diagnostics);

            var expression = tree.Root;

            using var e = new AssertingEnumerator(expression);

            e.AssertNode(SyntaxKind.CompilationUnit);
            e.AssertNode(SyntaxKind.GlobalStatement);
            e.AssertNode(SyntaxKind.ExpressionStatement);
            e.AssertNode(SyntaxKind.BlockExpression);
            e.AssertToken(SyntaxKind.OpenBraceToken, "{");
            e.AssertNode(SyntaxKind.WhileExpression);
            e.AssertToken(SyntaxKind.WhileKeyword, "while");
            e.AssertToken(SyntaxKind.OpenParenToken, "(");
            e.AssertNode(SyntaxKind.LiteralExpression);
            e.AssertToken(SyntaxKind.TrueKeyword, "true");
            e.AssertToken(SyntaxKind.CloseParenToken, ")");
            e.AssertNode(SyntaxKind.LiteralExpression);
            e.AssertToken(SyntaxKind.NumberToken, "1");
            e.AssertToken(SyntaxKind.CloseBraceToken, "}");
            e.AssertToken(SyntaxKind.EndOfInputToken, "");
        }
    }
}