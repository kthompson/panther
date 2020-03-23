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
            }
        }

        [Property]
        public void UnaryExpressionHonorsPrecedences(UnaryOperatorSyntaxKind op1, BinaryOperatorSyntaxKind op2)
        {
            var unaryOperatorPrecedence = op1.Kind.GetUnaryOperatorPrecedence();
            var binaryOperatorPrecedence = op2.Kind.GetBinaryOperatorPrecedence();

            var unaryText = SyntaxFacts.GetText(op1.Kind);
            var binaryText = SyntaxFacts.GetText(op2.Kind);

            var text = $"{unaryText} a {binaryText} b";
            var expression = SyntaxTree.Parse(text).Root;

            Assert.True(unaryOperatorPrecedence >= binaryOperatorPrecedence);

            using var e = new AssertingEnumerator(expression);

            // └──BinaryExpression
            //     ├──UnaryExpression
            //     │   ├──MinusToken
            //     │   └──NameExpression
            //     │       └──IdentifierToken
            //     ├──PlusToken
            //     └──NameExpression
            //         └──IdentifierToken

            e.AssertNode(SyntaxKind.BinaryExpression);
            e.AssertNode(SyntaxKind.UnaryExpression);
            e.AssertToken(op1.Kind, unaryText);
            e.AssertNode(SyntaxKind.NameExpression);
            e.AssertToken(SyntaxKind.IdentifierToken, "a");
            e.AssertToken(op2.Kind, binaryText);
            e.AssertNode(SyntaxKind.NameExpression);
            e.AssertToken(SyntaxKind.IdentifierToken, "b");
        }
    }
}