using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using FsCheck.Xunit;
using Mono.Cecil;
using Moq;
using Panther.CodeAnalysis;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Lowering;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;
using Panther.CodeAnalysis.Text;
using Panther.Tests.CodeAnalysis.Syntax;
using Xunit;
using static Panther.Tests.CodeAnalysis.TestHelpers;

namespace Panther.Tests.CodeAnalysis.Lowering
{
    [Properties(Arbitrary = new[] { typeof(BindingGenerators) })]
    public class LoweringTests
    {
        [Property]
        private void LoweringABoundStatementShouldNotContainBlocks(BoundStatement statement)
        {
            var block = Lowerer.Lower(statement);

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
                                        def sideEffect(a: string): string = {{
                                            println(a)
                                            ""V"" + a
                                        }}

                                        def concat(a: string, b: string, c: string): string = a + b + c

                                        {{
                                          println(""first"")
                                          println(concat({SideEffectBlock(0)}, sideEffect(""1""), {SideEffectBlock(2)})) 
                                          println(""last"")                                        
                                        }}").Text;

            var tree = SyntaxTree.Parse(source);
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
                case BoundBlockExpression _:
                    return true;
                case BoundErrorExpression _:
                case BoundLiteralExpression _:
                case BoundUnitExpression _:
                case BoundVariableExpression _:
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
                BoundAssignmentStatement assignmentStatement => ContainsBlock(assignmentStatement.Expression),
                BoundConditionalGotoStatement boundConditionalGotoStatement => ContainsBlock(
                    boundConditionalGotoStatement.Condition),
                BoundExpressionStatement boundExpressionStatement => ContainsBlock(boundExpressionStatement.Expression),
                BoundVariableDeclarationStatement boundVariableDeclarationStatement => ContainsBlock(
                    boundVariableDeclarationStatement.Expression),
                _ => throw new ArgumentOutOfRangeException(nameof(arg))
            };
    }
}