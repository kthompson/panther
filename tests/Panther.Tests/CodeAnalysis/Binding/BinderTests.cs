using System.Collections.Immutable;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;
using Xunit;
using static Panther.Tests.CodeAnalysis.TestHelpers;

namespace Panther.Tests.CodeAnalysis.Binding
{
    public class BinderTests
    {
        [Fact]
        public void ClassesHaveSymbols()
        {
            var code = AnnotatedText.Parse(@"
                class Hello() {
                    def main() = println(""Hello World"")
                }"
            );
            var symbols = Bind(code.Text);

            Assert.Collection(symbols, symbol =>
            {
                Assert.True(symbol.IsClass);
                Assert.Equal("Hello", symbol.Name);
            });
        }

        [Fact]
        public void ObjectNameIsCorrect()
        {
            var code = AnnotatedText.Parse(@"
                object Hello {
                    def main() = println(""Hello World"")
                }"
            );
            var symbols = Bind(code.Text);

            Assert.Collection(symbols, symbol =>
            {
                Assert.True(symbol.IsObject);
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
            var symbols = Bind(code.Text);

            Assert.Collection(symbols, hello =>
            {
                Assert.True(hello.IsObject);
                Assert.Equal("Hello", hello.Name);

                Assert.Collection(hello.Types, world =>
                {
                    Assert.True(world.IsObject);
                    Assert.Equal("World", world.Name);
                    Assert.Collection(world.Methods, invoke =>
                    {
                        Assert.True(invoke.IsMethod);
                        Assert.Equal("Invoke", invoke.Name);
                    });
                });
            });
        }


        [Fact]
        public void NamespacesShouldMerge()
        {
            var code = AnnotatedText.Parse(@"
                namespace Root

                object Hello {
                }"
            );
            var symbols = Bind(code.Text);

            Assert.Collection(symbols, root =>
            {
                Assert.True(root.IsNamespace);
                Assert.Equal("Root", root.Name);

                Assert.Collection(root.Types, hello =>
                {
                    Assert.True(hello.IsObject);
                    Assert.Equal("Hello", hello.Name);
                });
            });
        }

        [Fact]
        public void NamespacesCanBeDotted()
        {
            var code = AnnotatedText.Parse(@"
                namespace Root.Nested

                object Hello {
                }"
            );
            var symbols = Bind(code.Text);

            Assert.Collection(symbols, root =>
            {
                Assert.True(root.IsNamespace);
                Assert.Equal("Root", root.Name);

                Assert.Collection(root.Namespaces, nested =>
                {
                    Assert.True(nested.IsNamespace);
                    Assert.Equal("Nested", nested.Name);

                    Assert.Collection(nested.Types, hello =>
                    {
                        Assert.True(hello.IsObject);
                        Assert.Equal("Hello", hello.Name);
                    });
                });
            });
        }


        [Fact]
        public void NamespacesCanBeNested()
        {
            var code = AnnotatedText.Parse(@"
                namespace Root
                namespace Nested

                object Hello {
                }"
            );
            var symbols = Bind(code.Text);

            Assert.Collection(symbols, root =>
            {
                Assert.True(root.IsNamespace);
                Assert.Equal("Root", root.Name);

                Assert.Collection(root.Namespaces, nested =>
                {
                    Assert.True(nested.IsNamespace);
                    Assert.Equal("Nested", nested.Name);

                    Assert.Collection(nested.Types, hello =>
                    {
                        Assert.True(hello.IsObject);
                        Assert.Equal("Hello", hello.Name);
                    });
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
            var symbols = Bind(code.Text);

            Assert.Collection(symbols,
                symbol => Assert.Collection(symbol.Methods,
                    methodSymbol => Assert.Equal("world", methodSymbol.Name)));
        }

        [Fact]
        public void ObjectAndClassCoexist()
        {
            var code = AnnotatedText.Parse(@"
                object Hello {}
                class Hello(a: int)
            ");
            var symbols = Bind(code.Text);

            Assert.Collection(symbols,
                symbol => Assert.Empty(symbol.Methods),
                symbol => Assert.Collection(symbol.Parameters, param => Assert.Equal("a", param.Name))
            );
        }

        [Fact]
        public void ReportDuplicateObjectDefinition()
        {
            var code = @"
                        object Hello {}
                        object [Hello] {}
                        ";

            var diagnostic = @"
                Duplicate definition 'Hello'
            ";

            BindDiagnostics(code, diagnostic);
        }

        [Fact]
        public void ReportDuplicateClassDefinition()
        {
            var code = @"
                        class Hello() {}
                        class [Hello]() {}
                        ";

            var diagnostic = @"
                Duplicate definition 'Hello'
            ";

            BindDiagnostics(code, diagnostic);
        }

        [Fact]
        public void ReportParameterAlreadyDeclared()
        {
            var code = @"def Main(a: int, [a]: bool) = ()";

            var diagnostic = @"
                Duplicate parameter 'a'
            ";

            BindDiagnostics(code, diagnostic);
        }

        [Fact]
        public void ReportVariableAlreadyDeclaredFromParameter()
        {
            var code = @"def Main(a: int) = {
                            val [a] = 12
                        }";

            var diagnostic = @"
                Variable 'a' is already defined in the current scope
            ";

            BindDiagnostics(code, diagnostic);
        }

        [Fact]
        public void ReportVariableAlreadyDeclaredFromLocal()
        {
            var code = @"{
                            val a = 12
                            val [a] = true
                        }";

            var diagnostic = @"
                Variable 'a' is already defined in the current scope
            ";

            BindDiagnostics(code, diagnostic);
        }

        private void BindDiagnostics(string code, string diagnosticsText)
        {
            var annotated = AnnotatedText.Parse(code);
            var tree = SyntaxTree.Parse(annotated.Text);
            Assert.Empty(tree.Diagnostics);
            var binding = SymbolBinder.Bind(tree);

            AssertDiagnostics(diagnosticsText, annotated, binding.Diagnostics);
        }

        private ImmutableArray<Symbol> Bind(string code)
        {
            var tree = SyntaxTree.Parse(code);
            Assert.Empty(tree.Diagnostics);

            return SymbolBinder.Bind(tree).RootSymbol.Members;
        }
    }
}