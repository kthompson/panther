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
                e.AssertTrivia(SyntaxKind.WhitespaceTrivia, " ");
                e.AssertToken(op1.Kind, op1Text);
                e.AssertTrivia(SyntaxKind.WhitespaceTrivia, " ");
                e.AssertNode(SyntaxKind.NameExpression);
                e.AssertToken(SyntaxKind.IdentifierToken, "b");
                e.AssertTrivia(SyntaxKind.WhitespaceTrivia, " ");
                e.AssertToken(op2.Kind, op2Text);
                e.AssertTrivia(SyntaxKind.WhitespaceTrivia, " ");
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
                e.AssertTrivia(SyntaxKind.WhitespaceTrivia, " ");
                e.AssertToken(op1.Kind, op1Text);
                e.AssertTrivia(SyntaxKind.WhitespaceTrivia, " ");
                e.AssertNode(SyntaxKind.BinaryExpression);
                e.AssertNode(SyntaxKind.NameExpression);
                e.AssertToken(SyntaxKind.IdentifierToken, "b");
                e.AssertTrivia(SyntaxKind.WhitespaceTrivia, " ");
                e.AssertToken(op2.Kind, op2Text);
                e.AssertTrivia(SyntaxKind.WhitespaceTrivia, " ");
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
            e.AssertTrivia(SyntaxKind.WhitespaceTrivia, " ");
            e.AssertNode(SyntaxKind.NameExpression);
            e.AssertToken(SyntaxKind.IdentifierToken, "a");
            e.AssertTrivia(SyntaxKind.WhitespaceTrivia, " ");
            e.AssertToken(op2.Kind, binaryText);
            e.AssertTrivia(SyntaxKind.WhitespaceTrivia, " ");
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
            var text = AnnotatedText.Parse(@"
                         {
                            val x = 5
                            5
                         }");
            var tree = SyntaxTree.Parse(text.Text);
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
            e.AssertTrivia(SyntaxKind.EndOfLineTrivia);
            e.AssertNode(SyntaxKind.VariableDeclarationStatement);
            e.AssertTrivia(SyntaxKind.WhitespaceTrivia, "   ");
            e.AssertToken(SyntaxKind.ValKeyword, "val");
            e.AssertTrivia(SyntaxKind.WhitespaceTrivia, " ");
            e.AssertToken(SyntaxKind.IdentifierToken, "x");
            e.AssertTrivia(SyntaxKind.WhitespaceTrivia, " ");
            e.AssertToken(SyntaxKind.EqualsToken, "=");
            e.AssertTrivia(SyntaxKind.WhitespaceTrivia, " ");
            e.AssertNode(SyntaxKind.LiteralExpression);
            e.AssertToken(SyntaxKind.NumberToken, "5");
            e.AssertTrivia(SyntaxKind.EndOfLineTrivia);

            e.AssertNode(SyntaxKind.LiteralExpression);
            e.AssertTrivia(SyntaxKind.WhitespaceTrivia, "   ");
            e.AssertToken(SyntaxKind.NumberToken, "5");
            e.AssertTrivia(SyntaxKind.EndOfLineTrivia);
            e.AssertToken(SyntaxKind.CloseBraceToken, "}");
            e.AssertToken(SyntaxKind.EndOfInputToken, "");
        }



        [Fact]
        public void ParseLineCommentOnly()
        {
            var text = @"// taco";
            var tree = SyntaxTree.Parse(text);
            Assert.Empty(tree.Diagnostics);

            var expression = tree.Root;

            using var e = new AssertingEnumerator(expression);

            e.AssertNode(SyntaxKind.CompilationUnit);
            e.AssertTrivia(SyntaxKind.LineCommentTrivia, "// taco");
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
            e.AssertTrivia(SyntaxKind.WhitespaceTrivia, " ");
            e.AssertToken(SyntaxKind.OpenParenToken, "(");
            e.AssertNode(SyntaxKind.NameExpression);
            e.AssertToken(SyntaxKind.IdentifierToken, "x");
            e.AssertTrivia(SyntaxKind.WhitespaceTrivia, " ");
            e.AssertToken(SyntaxKind.LessThanDashToken, "<-");
            e.AssertTrivia(SyntaxKind.WhitespaceTrivia, " ");
            e.AssertNode(SyntaxKind.LiteralExpression);
            e.AssertToken(SyntaxKind.NumberToken, "0");
            e.AssertTrivia(SyntaxKind.WhitespaceTrivia, " ");
            e.AssertToken(SyntaxKind.ToKeyword, "to");
            e.AssertTrivia(SyntaxKind.WhitespaceTrivia, " ");
            e.AssertNode(SyntaxKind.LiteralExpression);
            e.AssertToken(SyntaxKind.NumberToken, "5");
            e.AssertToken(SyntaxKind.CloseParenToken, ")");
            e.AssertTrivia(SyntaxKind.WhitespaceTrivia, " ");
            e.AssertNode(SyntaxKind.NameExpression);
            e.AssertToken(SyntaxKind.IdentifierToken, "x");
            e.AssertToken(SyntaxKind.EndOfInputToken, "");
        }


        [Fact]
        public void ParseUnaryExpressionAfterLineBreak()
        {
            var text = AnnotatedText.Parse(@"
                {
                    val x = 3
                    -x
                }
            ");
            var tree = SyntaxTree.Parse(text.Text);
            Assert.Empty(tree.Diagnostics);

            var expression = tree.Root;

            using var e = new AssertingEnumerator(expression);

            e.AssertNode(SyntaxKind.CompilationUnit);
            e.AssertNode(SyntaxKind.GlobalStatement);
            e.AssertNode(SyntaxKind.ExpressionStatement);
            e.AssertNode(SyntaxKind.BlockExpression);
            e.AssertToken(SyntaxKind.OpenBraceToken, "{");
            e.AssertTrivia(SyntaxKind.EndOfLineTrivia);
            e.AssertNode(SyntaxKind.VariableDeclarationStatement);
            e.AssertTrivia(SyntaxKind.WhitespaceTrivia, "    ");
            e.AssertToken(SyntaxKind.ValKeyword, "val");
            e.AssertTrivia(SyntaxKind.WhitespaceTrivia, " ");
            e.AssertToken(SyntaxKind.IdentifierToken, "x");
            e.AssertTrivia(SyntaxKind.WhitespaceTrivia, " ");
            e.AssertToken(SyntaxKind.EqualsToken, "=");
            e.AssertTrivia(SyntaxKind.WhitespaceTrivia, " ");
            e.AssertNode(SyntaxKind.LiteralExpression);
            e.AssertToken(SyntaxKind.NumberToken, "3");
            e.AssertTrivia(SyntaxKind.EndOfLineTrivia);

            e.AssertNode(SyntaxKind.UnaryExpression);
            e.AssertTrivia(SyntaxKind.WhitespaceTrivia, "    ");
            e.AssertToken(SyntaxKind.DashToken, "-");
            e.AssertNode(SyntaxKind.NameExpression);
            e.AssertToken(SyntaxKind.IdentifierToken, "x");
            e.AssertTrivia(SyntaxKind.EndOfLineTrivia);
            e.AssertToken(SyntaxKind.CloseBraceToken, "}");
            e.AssertToken(SyntaxKind.EndOfInputToken, "");
        }


        [Fact]
        public void ParseBinaryExpressionWithLineBreakInsideGroup()
        {
            var text = AnnotatedText.Parse(@"
                (false
                    || (false
                           || true))
            ");
            var tree = SyntaxTree.Parse(text.Text);
            Assert.Empty(tree.Diagnostics);

            var expression = tree.Root;

            using var e = new AssertingEnumerator(expression);

            e.AssertNode(SyntaxKind.CompilationUnit);
            e.AssertNode(SyntaxKind.GlobalStatement);
            e.AssertNode(SyntaxKind.ExpressionStatement);
            e.AssertNode(SyntaxKind.GroupExpression);
            e.AssertToken(SyntaxKind.OpenParenToken, "(");
            e.AssertNode(SyntaxKind.BinaryExpression);
            e.AssertNode(SyntaxKind.LiteralExpression);
            e.AssertToken(SyntaxKind.FalseKeyword, "false");
            e.AssertTrivia(SyntaxKind.EndOfLineTrivia);
            e.AssertTrivia(SyntaxKind.WhitespaceTrivia, "    ");
            e.AssertToken(SyntaxKind.PipePipeToken, "||");
            e.AssertTrivia(SyntaxKind.WhitespaceTrivia, " ");
            e.AssertNode(SyntaxKind.GroupExpression);
            e.AssertToken(SyntaxKind.OpenParenToken, "(");
            e.AssertNode(SyntaxKind.BinaryExpression);
            e.AssertNode(SyntaxKind.LiteralExpression);
            e.AssertToken(SyntaxKind.FalseKeyword, "false");
            e.AssertTrivia(SyntaxKind.EndOfLineTrivia);
            e.AssertTrivia(SyntaxKind.WhitespaceTrivia, "           ");
            e.AssertToken(SyntaxKind.PipePipeToken, "||");
            e.AssertTrivia(SyntaxKind.WhitespaceTrivia, " ");
            e.AssertNode(SyntaxKind.LiteralExpression);
            e.AssertToken(SyntaxKind.TrueKeyword, "true");
            e.AssertToken(SyntaxKind.CloseParenToken, ")");
            e.AssertToken(SyntaxKind.CloseParenToken, ")");
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
            e.AssertTrivia(SyntaxKind.WhitespaceTrivia, " ");
            e.AssertToken(SyntaxKind.OpenParenToken, "(");
            e.AssertNode(SyntaxKind.NameExpression);
            e.AssertToken(SyntaxKind.IdentifierToken, "x");
            e.AssertTrivia(SyntaxKind.WhitespaceTrivia, " ");
            e.AssertToken(SyntaxKind.LessThanDashToken, "<-");
            e.AssertTrivia(SyntaxKind.WhitespaceTrivia, " ");
            e.AssertNode(SyntaxKind.LiteralExpression);
            e.AssertToken(SyntaxKind.NumberToken, "0");
            e.AssertTrivia(SyntaxKind.WhitespaceTrivia, " ");
            e.AssertToken(SyntaxKind.ToKeyword, "to");
            e.AssertTrivia(SyntaxKind.WhitespaceTrivia, " ");
            e.AssertNode(SyntaxKind.LiteralExpression);
            e.AssertToken(SyntaxKind.NumberToken, "5");
            e.AssertToken(SyntaxKind.CloseParenToken, ")");
            e.AssertTrivia(SyntaxKind.EndOfLineTrivia);
            e.AssertNode(SyntaxKind.NameExpression);
            e.AssertTrivia(SyntaxKind.WhitespaceTrivia, "    ");
            e.AssertToken(SyntaxKind.IdentifierToken, "x");
            e.AssertToken(SyntaxKind.EndOfInputToken, "");
        }

        [Fact]
        public void ParseWhileExpression()
        {
            var text = AnnotatedText.Parse(@"
                          {
                            while ( true ) 1
                          }");
            var tree = SyntaxTree.Parse(text.Text);
            Assert.Empty(tree.Diagnostics);

            var expression = tree.Root;

            using var e = new AssertingEnumerator(expression);

            e.AssertNode(SyntaxKind.CompilationUnit);
            e.AssertNode(SyntaxKind.GlobalStatement);
            e.AssertNode(SyntaxKind.ExpressionStatement);
            e.AssertNode(SyntaxKind.BlockExpression);
            e.AssertToken(SyntaxKind.OpenBraceToken, "{");
            e.AssertTrivia(SyntaxKind.EndOfLineTrivia);
            e.AssertNode(SyntaxKind.WhileExpression);
            e.AssertTrivia(SyntaxKind.WhitespaceTrivia, "  ");
            e.AssertToken(SyntaxKind.WhileKeyword, "while");
            e.AssertTrivia(SyntaxKind.WhitespaceTrivia, " ");
            e.AssertToken(SyntaxKind.OpenParenToken, "(");
            e.AssertTrivia(SyntaxKind.WhitespaceTrivia, " ");
            e.AssertNode(SyntaxKind.LiteralExpression);
            e.AssertToken(SyntaxKind.TrueKeyword, "true");
            e.AssertTrivia(SyntaxKind.WhitespaceTrivia, " ");
            e.AssertToken(SyntaxKind.CloseParenToken, ")");
            e.AssertTrivia(SyntaxKind.WhitespaceTrivia, " ");
            e.AssertNode(SyntaxKind.LiteralExpression);
            e.AssertToken(SyntaxKind.NumberToken, "1");
            e.AssertTrivia(SyntaxKind.EndOfLineTrivia);
            e.AssertToken(SyntaxKind.CloseBraceToken, "}");
            e.AssertToken(SyntaxKind.EndOfInputToken, "");
        }
    }
}