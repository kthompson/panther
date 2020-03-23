using System.ComponentModel.DataAnnotations;
using FsCheck.Xunit;
using Panther.CodeAnalysis.Syntax;

namespace Panther.Tests.CodeAnalysis.Syntax
{
    [Properties(Arbitrary = new[] { typeof(TokenGenerators) })]
    public class ParserTests
    {
        [Property]
        public void Parser_BinaryExpressionHonorsPrecedences(BinaryOperatorSyntaxKind op1, BinaryOperatorSyntaxKind op2)
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
    }
}