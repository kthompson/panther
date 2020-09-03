﻿using Panther.CodeAnalysis.Symbols;
using Xunit;

namespace Panther.Tests.CodeAnalysis.Binding
{
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
            var compilation = TestHelpers.Compile(code.Text);

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
            var compilation = TestHelpers.Compile(code.Text);

            Assert.Collection(compilation.Types, hello =>
            {
                Assert.Equal("Hello", hello.Name);

                Assert.Collection(hello.GetTypeMembers(), world =>
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
            var compilation = TestHelpers.Compile(code.Text);

            Assert.Collection(compilation.Types,
                symbol => Assert.Collection(symbol.GetMembers().OfType<MethodSymbol>(),
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
            var compilation = TestHelpers.Compile(code.Text);

            Assert.Collection(compilation.Types,
                symbol => Assert.Collection(symbol.GetMembers().OfType<MethodSymbol>(),
                    methodSymbol => Assert.Equal("unit", methodSymbol.ReturnType.Name)));
        }
    }
}