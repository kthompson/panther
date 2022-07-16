using FsCheck.Xunit;
using Xunit;
using static Panther.Tests.CodeAnalysis.TestHelpers;

namespace Panther.Tests.CodeAnalysis;

[Properties(MaxTest = 10)]
public class ObjectAndClassTests
{
    [Fact]
    public void EvaluatesObjectMethodCallExpression()
    {
        using var scriptHost = BuildScriptHost();
        var code =
            @"
                SomeObject.method()
                
                object SomeObject {
                    def method() = ""taco""
                }
            ";

        AssertEvaluation(code, "taco", scriptHost);
    }

    [Fact]
    public void EvaluatesObjectFieldExpression()
    {
        using var scriptHost = BuildScriptHost();
        var code =
            @"
                SomeObject.field
                
                object SomeObject {
                    val field = ""taco""
                }
            ";

        AssertEvaluation(code, "taco", scriptHost);
    }

    [Fact(Skip = "TODO")]
    public void EvaluatesObjectNestedFieldExpression()
    {
        using var scriptHost = BuildScriptHost();
        var code =
            @"
                SomeObject.Nested.field
                
                object SomeObject {
                    object Nested {
                        val field = ""taco""
                    }
                }
            ";

        AssertEvaluation(code, "taco", scriptHost);
    }

    [Fact]
    public void EvaluatesObjectFieldAssignmentExpression()
    {
        using var scriptHost = BuildScriptHost();
        var code =
            @"
                SomeObject.field = SomeObject.field + "" bell""
                SomeObject.field
                
                object SomeObject {
                    var field = ""taco""
                }
            ";

        AssertEvaluation(code, "taco bell", scriptHost);
    }

    [Fact(Skip = "TODO")]
    public void EvaluatesNestedObjectFieldAssignmentExpression()
    {
        using var scriptHost = BuildScriptHost();
        var code =
            @"
                SomeObject.Nested.field = SomeObject.Nested.field + "" bell""
                SomeObject.Nested.field
                
                object SomeObject {
                    object Nested {   
                        var field = ""taco""
                    }
                }
            ";

        AssertEvaluation(code, "taco bell", scriptHost);
    }

    [Property]
    public void EvaluatesClassFieldExpression(int x, int y)
    {
        using var scriptHost = BuildScriptHost();
        var code =
            $@"
                new Point({x}, {y}).X
                
                class Point(X: int, Y: int)
            ";

        AssertEvaluation(code, x, scriptHost);
    }

    [Property]
    public void EvaluatesClassMethodCallExpression(int x, int y)
    {
        using var scriptHost = BuildScriptHost();
        var code =
            $@"
                new Point({x}, {y}).distance()
                
                class Point(X: int, Y: int)
                {{
                    def distance(): int = X * Y
                }}
            ";

        AssertEvaluation(code, x * y, scriptHost);
    }

    [Fact]
    public void EvaluatesClassFieldsFieldExpression()
    {
        using var scriptHost = BuildScriptHost();
        var code =
            $@"
                val x = new B(new A(1))
                x.A.X
                
                class A(X: int)
                class B(A: A)
            ";

        AssertEvaluation(code, 1, scriptHost);
    }

    [Property]
    public void EvaluatesClassFieldAccess(int x, int y)
    {
        using var scriptHost = BuildScriptHost();
        var code =
            $@"
                val p = new Point({x}, {y})
                p.X
                
                class Point(X: int, Y: int)
            ";

        AssertEvaluation(code, x, scriptHost);
    }

    [Fact]
    public void EvaluatesClassFieldAccess2()
    {
        using var scriptHost = BuildScriptHost();
        var code =
            $@"
                val p = new Point(10, 20)
                p.Y
                
                class Point(X: int, Y: int)
            ";

        AssertEvaluation(code, 20, scriptHost);
    }

    [Property]
    public void EvaluatesClassFieldAssignment(int x, int y)
    {
        using var scriptHost = BuildScriptHost();
        var code =
            $@"
                val p = new A({x})
                p.X = {y}
                p.X
                
                class A(X: int)
            ";

        AssertEvaluation(code, y, scriptHost);
    }

    [Property]
    public void EvaluatesClassFieldExpressions(int x, int y)
    {
        using var scriptHost = BuildScriptHost();
        var code =
            $@"
                val p = new A({x})
                p.X = p.X + {y}
                p.X
                
                class A(X: int)
            ";

        AssertEvaluation(code, x + y, scriptHost);
    }

    [Fact]
    public void EvaluatesClassAssignmentWithTypeAnnotation()
    {
        using var scriptHost = BuildScriptHost();
        var code =
            $@"
                val p: Point = new Point(10, 20)
                p.Y
                
                class Point(X: int, Y: int)
            ";

        AssertEvaluation(code, 20, scriptHost);
    }
}
