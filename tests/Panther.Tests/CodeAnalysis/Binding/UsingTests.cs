using Xunit;
using static Panther.Tests.CodeAnalysis.TestHelpers;

namespace Panther.Tests.CodeAnalysis.Binding;

public class UsingTests
{
    [Fact]
    public void CanUseConsole()
    {
        var code = AnnotatedText.Parse(
            @"
                using System

                object Hello {
                    def print(value: any) = Console.Write(value)
                }"
        );
        var compilation = Compile(code.Text);

        Assert.Collection(
            compilation.RootSymbol.Types,
            symbol =>
                Assert.Collection(
                    symbol.Methods,
                    methodSymbol => Assert.Equal("unit", methodSymbol.ReturnType.Symbol.Name)
                )
        );
    }
}
