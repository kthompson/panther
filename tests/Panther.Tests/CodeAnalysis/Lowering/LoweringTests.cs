using System;
using System.Linq;
using FsCheck.Xunit;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Lowering;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;
using Xunit;
using static Panther.Tests.CodeAnalysis.TestHelpers;

namespace Panther.Tests.CodeAnalysis.Lowering
{
    [Properties(Arbitrary = new[] { typeof(BindingGenerators) }, MaxTest = 1000)]
    public class LoweringTests
    {
        [Property]
        private void LoweringABoundStatementShouldNotContainBlocks(BoundStatement statement)
        {
            var root = Symbol.NewRoot();
            var block = LoweringPipeline.Lower(root, statement);

            var containsBlock = ContainsBlock(block);
            if (containsBlock)
            {
                Assert.False(true, block.ToString());
            }
        }

        [Fact]
        private void LoweringShouldPreserveSideEffectOrderInCallExpressions()
        {
            string SideEffectBlock(int i) =>
                AnnotatedText.Parse($@"{{
                                        println(""{i}"")
                                        ""V{i}""
                                      }}").Text;


            var source = AnnotatedText.Parse($@"
                                        {{
                                          println(""first"")
                                          println(concat({SideEffectBlock(0)}, sideEffect(""1""), {SideEffectBlock(2)})) 
                                          println(""last"")                                        
                                        }}

                                        def sideEffect(a: string): string = {{
                                            println(a)
                                            ""V"" + a
                                        }}

                                        def concat(a: string, b: string, c: string): string = a + b + c
                                        ").Text;

            var tree = SyntaxTree.Parse(source);
            Assert.Empty(tree.Diagnostics);
            using var scriptHost = BuildScriptHostTestLib();

            scriptHost.Execute(tree);

            var expectedOutput = BuildExpectedOutput(
                "first",
                "0",
                "1",
                "2",
                "V0V1V2",
                "last");

            AssertEvaluation("getOutput()", expectedOutput, scriptHost);
        }

        private bool ContainsBlock(BoundBlockExpression block) =>
            block.Statements.Any(ContainsBlock) || ContainsBlock(block.Expression);

        private bool ContainsBlock(params BoundExpression[] expression) => expression.Any(ContainsBlock);

        private bool ContainsBlock(BoundExpression expression)
        {
            switch (expression)
            {
                case BoundBlockExpression:
                    return true;
                case BoundErrorExpression:
                case BoundLiteralExpression:
                case BoundUnitExpression:
                case BoundVariableExpression:
                    return false;
                case BoundAssignmentExpression assignmentExpression:
                    return ContainsBlock(assignmentExpression.Expression);
                case BoundBinaryExpression binaryExpression:
                    return ContainsBlock(binaryExpression.Left, binaryExpression.Right);
                case BoundCallExpression boundCallExpression:
                    return ContainsBlock(boundCallExpression.Arguments.ToArray());
                case BoundConversionExpression conversionExpression:
                    return ContainsBlock(conversionExpression.Expression);
                case BoundForExpression boundForExpression:
                    return ContainsBlock(boundForExpression.Body, boundForExpression.LowerBound,
                        boundForExpression.UpperBound);
                case BoundIfExpression boundIfExpression:
                    return ContainsBlock(boundIfExpression.Condition, boundIfExpression.Else, boundIfExpression.Then);
                case BoundWhileExpression boundWhileExpression:
                    return ContainsBlock(boundWhileExpression.Body, boundWhileExpression.Condition);
                case BoundUnaryExpression boundUnaryExpression:
                    return ContainsBlock(boundUnaryExpression.Operand);
                default:
                    throw new ArgumentOutOfRangeException(nameof(expression));
            }
        }

        private bool ContainsBlock(BoundStatement arg) =>
            arg switch
            {
                BoundGotoStatement _ => false,
                BoundLabelStatement _ => false,
                BoundNopStatement _ => false,
                BoundAssignmentStatement statement => ContainsBlock(statement.Expression),
                BoundConditionalGotoStatement statement => ContainsBlock(statement.Condition),
                BoundExpressionStatement statement => ContainsBlock(statement.Expression),
                BoundVariableDeclarationStatement(_, _, var expression) => expression != null &&
                                                                           ContainsBlock(expression),
                _ => throw new ArgumentOutOfRangeException(nameof(arg))
            };
    }
}