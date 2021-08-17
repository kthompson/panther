using FsCheck.Xunit;
using Xunit;
using static Panther.Tests.CodeAnalysis.TestHelpers;

namespace Panther.Tests.CodeAnalysis
{
    [Properties(MaxTest = 10)]
    public class ObjectAndClassTests
    {
        [Fact]
        public void EvaluatesObjectMethodCallExpression()
        {
            using var scriptHost = BuildScriptHost();
            string code = @"
                SomeObject.method()
                
                object SomeObject {
                    def method() = ""taco""
                }
            ";

            AssertEvaluation(code, "taco", scriptHost);
        }

        [Fact(Skip = "TODO")]
        public void EvaluatesObjectFieldExpression()
        {
            using var scriptHost = BuildScriptHost();
            string code = @"
                SomeObject.field
                
                object SomeObject {
                    val field = ""taco""
                }
            ";

            AssertEvaluation(code, "taco", scriptHost);
        }


        [Property]
        public void EvaluatesClassMethodCallExpression(int x, int y)
        {
            using var scriptHost = BuildScriptHost();
            string code = $@"
                new Point({x}, {y}).distance()
                
                class Point(X: int, Y: int)
                {{
                    def distance(): int = X * Y
                }}
            ";

            AssertEvaluation(code, x * y, scriptHost);
        }


        [Property]
        public void EvaluatesClassFieldExpression(int x, int y)
        {
            using var scriptHost = BuildScriptHost();
            string code = $@"
                new Point({x}, {y}).X
                
                class Point(X: int, Y: int)
            ";

            AssertEvaluation(code, x, scriptHost);
        }

        [Property]
        public void EvaluatesClassAssignment(int x, int y)
        {
            using var scriptHost = BuildScriptHost();
            string code = $@"
                val p = new Point({x}, {y})
                p.X
                
                class Point(X: int, Y: int)
            ";

            AssertEvaluation(code, x, scriptHost);
        }

        [Fact]
        public void EvaluatesClassAssignment2()
        {
            using var scriptHost = BuildScriptHost();
            string code = $@"
                val p = new Point(10, 20)
                p.Y
                
                class Point(X: int, Y: int)
            ";

            AssertEvaluation(code, 20, scriptHost);
        }

        [Fact]
        public void EvaluatesClassAssignmentWithTypeAnnotation()
        {
            using var scriptHost = BuildScriptHost();
            string code = $@"
                val p: Point = new Point(10, 20)
                p.Y
                
                class Point(X: int, Y: int)
            ";

            AssertEvaluation(code, 20, scriptHost);
        }
    }
}