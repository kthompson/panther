using Panther.CodeAnalysis.Symbols;
using Xunit;
using static Panther.Tests.CodeAnalysis.TestHelpers;

namespace Panther.Tests.CodeAnalysis.Binding;

public class ObjectTests
{

    [Fact]
    public void ObjectNameIsCorrect()
    {
        var code = AnnotatedText.Parse(@"
                object Hello {
                    def main() = println(""Hello World"")
                }"
        );
        var compilation = Compile(code.Text);

        Assert.Collection(compilation.Types, symbol =>
        {
            Assert.Equal("Hello", symbol.Name);
        });
    }


    [Fact]
    public void ObjectsCanBeNested()
    {
        var code = AnnotatedText.Parse(@"
                object Hello {
                    object World {
                        def Invoke() = println(""Hello World!"")
                    }
                }"
        );
        var compilation = Compile(code.Text);

        Assert.Collection(compilation.Types, hello =>
        {
            Assert.Equal("Hello", hello.Name);

            Assert.Collection(hello.Types, world =>
            {
                Assert.Equal("World", world.Name);
            });
        });
    }

    [Fact]
    public void ObjectHasMethod()
    {
        var code = AnnotatedText.Parse(@"
                object Hello {
                    def world() = println(""Hello World"")
                }"
        );
        var compilation = Compile(code.Text);

        Assert.Collection(compilation.Types,
            symbol => Assert.Collection(symbol.Methods,
                methodSymbol => Assert.Equal("world", methodSymbol.Name)));
    }

    [Fact]
    public void MethodHasReturnType()
    {
        var code = AnnotatedText.Parse(@"
                object Hello {
                    def world() = println(""Hello World"")
                }"
        );
        var compilation = Compile(code.Text);

        Assert.Collection(compilation.Types,
            symbol => Assert.Collection(symbol.Methods,
                methodSymbol => Assert.Equal("unit", methodSymbol.ReturnType.Symbol.Name)));
    }
}