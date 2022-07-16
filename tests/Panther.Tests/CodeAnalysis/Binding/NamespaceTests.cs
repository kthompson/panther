using Xunit;

namespace Panther.Tests.CodeAnalysis.Binding;

public class NamespaceTests
{
    [Fact]
    public void NamespaceSpecifiedForObject()
    {
        var code = AnnotatedText.Parse(
            @"
                namespace HelloNamespace

                object Hello {
                    def main() = println(""Hello World"")
                }"
        );
        var compilation = TestHelpers.Compile(code.Text);

        var HelloNamespace = compilation.RootSymbol.LookupNamespace("HelloNamespace");
        Assert.NotNull(HelloNamespace);

        Assert.Equal("HelloNamespace", HelloNamespace!.Name);

        Assert.Collection(HelloNamespace.Members, symbol => Assert.Equal("Hello", symbol.Name));
    }

    [Fact]
    public void DottedNamespaceSpecifiedForObject()
    {
        var code = AnnotatedText.Parse(
            @"
                namespace Hello.Namespace

                object Hello {
                    def main() = println(""Hello World"")
                }"
        );
        var compilation = TestHelpers.Compile(code.Text);

        var Hello = compilation.RootSymbol.LookupNamespace("Hello");
        Assert.NotNull(Hello);
        var Namespace = Hello!.LookupNamespace("Namespace");
        Assert.NotNull(Namespace);

        Assert.Collection(
            Namespace!.Types,
            symbol =>
            {
                Assert.Equal("Hello", symbol.Name);
                Assert.Collection(symbol.Methods, ns => Assert.Equal("main", ns.Name));
            }
        );
    }
}
