using System;
using System.Linq;
using FsCheck.Xunit;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Lowering;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;
using Xunit;
using static Panther.Tests.CodeAnalysis.TestHelpers;

namespace Panther.Tests.CodeAnalysis.Lowering;

[Properties(Arbitrary = new[] { typeof(BindingGenerators) }, MaxTest = 1000)]
public class LoweringTests
{
    [Property]
    private void LoweringATypedStatementShouldNotContainBlocks(TypedStatement statement)
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
            AnnotatedText
                .Parse(
                    $@"{{
                                        println(""{i}"")
                                        ""V{i}""
                                      }}"
                )
                .Text;

        var source = AnnotatedText
            .Parse(
                $@"
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
                                        "
            )
            .Text;

        var tree = SyntaxTree.Parse(source);
        Assert.Empty(tree.Diagnostics);
        using var scriptHost = BuildScriptHostTestLib();

        scriptHost.Execute(tree);

        var expectedOutput = BuildExpectedOutput("first", "0", "1", "2", "V0V1V2", "last");

        AssertEvaluation("getOutput()", expectedOutput, scriptHost);
    }

    private bool ContainsBlock(TypedBlockExpression block) =>
        block.Statements.Any(ContainsBlock) || ContainsBlock(block.Expression);

    private bool ContainsBlock(params TypedExpression[] expression) =>
        expression.Any(ContainsBlock);

    private bool ContainsBlock(TypedExpression expression)
    {
        switch (expression)
        {
            case TypedBlockExpression:
                return true;
            case TypedErrorExpression:
            case TypedLiteralExpression:
            case TypedUnitExpression:
            case TypedVariableExpression:
                return false;
            case TypedAssignmentExpression assignmentExpression:
                return ContainsBlock(assignmentExpression.Right);
            case TypedBinaryExpression binaryExpression:
                return ContainsBlock(binaryExpression.Left, binaryExpression.Right);
            case TypedCallExpression boundCallExpression:
                return ContainsBlock(boundCallExpression.Arguments.ToArray());
            case TypedConversionExpression conversionExpression:
                return ContainsBlock(conversionExpression.Expression);
            case TypedForExpression boundForExpression:
                return ContainsBlock(
                    boundForExpression.Body,
                    boundForExpression.LowerTyped,
                    boundForExpression.UpperTyped
                );
            case TypedIfExpression boundIfExpression:
                return ContainsBlock(
                    boundIfExpression.Condition,
                    boundIfExpression.Else,
                    boundIfExpression.Then
                );
            case TypedWhileExpression boundWhileExpression:
                return ContainsBlock(boundWhileExpression.Body, boundWhileExpression.Condition);
            case TypedUnaryExpression boundUnaryExpression:
                return ContainsBlock(boundUnaryExpression.Operand);
            default:
                throw new ArgumentOutOfRangeException(nameof(expression));
        }
    }

    private bool ContainsBlock(TypedStatement arg) =>
        arg switch
        {
            TypedGotoStatement _ => false,
            TypedLabelStatement _ => false,
            TypedNopStatement _ => false,
            TypedAssignmentStatement statement => ContainsBlock(statement.Right),
            TypedConditionalGotoStatement statement => ContainsBlock(statement.Condition),
            TypedExpressionStatement statement => ContainsBlock(statement.Expression),
            TypedVariableDeclarationStatement(_, _, var expression)
                => expression != null && ContainsBlock(expression),
            _ => throw new ArgumentOutOfRangeException(nameof(arg))
        };
}
