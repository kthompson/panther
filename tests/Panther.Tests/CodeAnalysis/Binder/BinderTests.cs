using System.Collections.Immutable;
using Panther.CodeAnalysis.Binder;
using Panther.CodeAnalysis.Syntax;
using Xunit;

namespace Panther.Tests.CodeAnalysis.Binder;

public class BinderTests
{
    [Fact]
    public void EnumerateSymbolsInRoot()
    {
        var code = """
                object SomeObject {
                    def method() = "taco"
                }
            """;

        using var enumerator = EnumerateCodeSymbols(code);
        enumerator.AssertSymbol(SymbolFlags.Object, "SomeObject");
        enumerator.AssertSymbol(SymbolFlags.Method, "method");
    }

    [Fact]
    public void EnumerateSymbolsInNestedObject()
    {
        var code = """
                object SomeObject {
                    object Nested {
                        val field = "taco"
                    }
                }
            """;

        using var enumerator = EnumerateCodeSymbols(code);
        enumerator.AssertSymbol(SymbolFlags.Object, "SomeObject");
        enumerator.AssertSymbol(SymbolFlags.Object, "Nested");
        enumerator.AssertSymbol(SymbolFlags.Field, "field");
    }

    [Fact]
    public void EnumerateSymbolsInNestedObjectMethod()
    {
        var code = """
                object SomeObject {
                    object Nested {
                        def method() = "taco"
                    }
                }
            """;

        using var enumerator = EnumerateCodeSymbols(code);
        enumerator.AssertSymbol(SymbolFlags.Object, "SomeObject");
        enumerator.AssertSymbol(SymbolFlags.Object, "Nested");
        enumerator.AssertSymbol(SymbolFlags.Method, "method");
    }

    [Fact]
    public void EnumerateSymbolsInNestedObjectMethodWithParameter()
    {
        var code = """
                object SomeObject {
                    object Nested {
                        def method(x: Int) = "taco"
                    }
                }
            """;

        using var enumerator = EnumerateCodeSymbols(code);
        enumerator.AssertSymbol(SymbolFlags.Object, "SomeObject");
        enumerator.AssertSymbol(SymbolFlags.Object, "Nested");
        enumerator.AssertSymbol(SymbolFlags.Method, "method");
        enumerator.AssertSymbol(SymbolFlags.Parameter, "x");
    }

    [Fact]
    public void EnumerateSymbolsInNestedObjectMethodWithMultipleParameters()
    {
        var code = """
                object SomeObject {
                    object Nested {
                        def method(x: Int, y: String) = "taco"
                    }
                }
            """;

        using var enumerator = EnumerateCodeSymbols(code);
        enumerator.AssertSymbol(SymbolFlags.Object, "SomeObject");
        enumerator.AssertSymbol(SymbolFlags.Object, "Nested");
        enumerator.AssertSymbol(SymbolFlags.Method, "method");
        enumerator.AssertSymbol(SymbolFlags.Parameter, "x");
        enumerator.AssertSymbol(SymbolFlags.Parameter, "y");
    }

    [Fact]
    public void EnumerateSymbolsInNestedObjectMethodWithReturnType()
    {
        var code = """
                object SomeObject {
                    object Nested {
                        def method(): String = "taco"
                    }
                }
            """;

        using var enumerator = EnumerateCodeSymbols(code);
        enumerator.AssertSymbol(SymbolFlags.Object, "SomeObject");
        enumerator.AssertSymbol(SymbolFlags.Object, "Nested");
        enumerator.AssertSymbol(SymbolFlags.Method, "method");
    }

    [Fact]
    public void EnumerateSymbolsInClasses()
    {
        var code = """
                class Point(X: int, Y: int)

                class Extent(xmin: int, xmax: int, ymin: int, ymax: int)
                {
                    def width(): int = xmax - xmin
                    def height(): int = ymax - ymin
                }
            """;

        using var enumerator = EnumerateCodeSymbols(code);
        enumerator.AssertSymbol(SymbolFlags.Class, "Point");
        enumerator.AssertSymbol(SymbolFlags.Field, "X");
        enumerator.AssertSymbol(SymbolFlags.Field, "Y");
        enumerator.AssertSymbol(SymbolFlags.Class, "Extent");
        enumerator.AssertSymbol(SymbolFlags.Field, "xmin");
        enumerator.AssertSymbol(SymbolFlags.Field, "xmax");
        enumerator.AssertSymbol(SymbolFlags.Field, "ymin");
        enumerator.AssertSymbol(SymbolFlags.Field, "ymax");
        enumerator.AssertSymbol(SymbolFlags.Method, "width");
        enumerator.AssertSymbol(SymbolFlags.Method, "height");
    }

    [Fact]
    public void EnumerateSymbolsInNestedClasses()
    {
        var code = """
                class Point(X: int, Y: int)
                {
                    class Nested() {
                        val field = "taco"
                    }
                }
            """;

        using var enumerator = EnumerateCodeSymbols(code);
        enumerator.AssertSymbol(SymbolFlags.Class, "Point");
        enumerator.AssertSymbol(SymbolFlags.Field, "X");
        enumerator.AssertSymbol(SymbolFlags.Field, "Y");
        enumerator.AssertSymbol(SymbolFlags.Class, "Nested");
        enumerator.AssertSymbol(SymbolFlags.Field, "field");
    }

    [Fact]
    public void EnumerateSymbolsInNestedClassesWithMethods()
    {
        var code = """
                class Point(X: int, Y: int)
                {
                    class Nested() {
                        def method() = "taco"
                    }
                }
            """;

        using var enumerator = EnumerateCodeSymbols(code);
        enumerator.AssertSymbol(SymbolFlags.Class, "Point");
        enumerator.AssertSymbol(SymbolFlags.Field, "X");
        enumerator.AssertSymbol(SymbolFlags.Field, "Y");
        enumerator.AssertSymbol(SymbolFlags.Class, "Nested");
        enumerator.AssertSymbol(SymbolFlags.Method, "method");
    }

    [Fact]
    public void EnumerateSymbolsInNamespace()
    {
        var code = """
                namespace HelloNamespace

                object Hello {
                    def main() = println("Hello World")
                }
            """;

        using var enumerator = EnumerateCodeSymbols(code);
        enumerator.AssertSymbol(SymbolFlags.Namespace, "HelloNamespace");
        enumerator.AssertSymbol(SymbolFlags.Object, "Hello");
        enumerator.AssertSymbol(SymbolFlags.Method, "main");
    }

    private static SymbolEnumerator EnumerateCodeSymbols(string code)
    {
        var global = Symbol.NewRoot();
        var tree = SyntaxTree.Parse(code);
        Assert.Empty(tree.Diagnostics);
        var (binderDiags, _) = Panther.CodeAnalysis.Binder.Binder.Bind(
            global,
            new[] { tree }.ToImmutableArray()
        );
        Assert.Empty(binderDiags);

        return new SymbolEnumerator(global);
    }
}
