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

            var op1Text = SyntaxFacts.GetText(op1.Kind);
            var op2Text = SyntaxFacts.GetText(op2.Kind);

            var text = $"a {op1Text} b {op2Text} c";
            var expression = SyntaxTree.Parse(text).Root.Statement;

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
            var expression = SyntaxTree.Parse(text).Root.Statement;

            using var e = new AssertingEnumerator(expression);

            // └──BinaryExpression
            //     ├──UnaryExpression
            //     │   ├──MinusToken
            //     │   └──NameExpression
            //     │       └──IdentifierToken
            //     ├──PlusToken
            //     └──NameExpression
            //         └──IdentifierToken

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
            var expression = SyntaxTree.Parse(text).Root.Statement;

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

            var expression = tree.Root.Statement;

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
            e.AssertToken(SyntaxKind.NewLineToken, Environment.NewLine);
            e.AssertNode(SyntaxKind.LiteralExpression);
            e.AssertToken(SyntaxKind.NumberToken, "5");
            e.AssertToken(SyntaxKind.CloseBraceToken, "}");
            e.AssertToken(SyntaxKind.CloseBraceToken, "}");
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

            var expression = tree.Root.Statement;

            using var e = new AssertingEnumerator(expression);

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