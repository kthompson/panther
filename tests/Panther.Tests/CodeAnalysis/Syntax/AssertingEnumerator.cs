using System;
using System.Collections.Generic;
using System.Linq;
using Panther.CodeAnalysis.Syntax;
using Xunit;

namespace Panther.Tests.CodeAnalysis.Syntax
{
    internal sealed class AssertingEnumerator : IDisposable
    {
        private readonly IEnumerator<SyntaxNode> _enumerator;
        private bool _hasErrors;

        public AssertingEnumerator(SyntaxNode node)
        {
            _enumerator = Flatten(node).GetEnumerator();
        }

        private static IEnumerable<SyntaxNode> Flatten(SyntaxNode node)
        {
            var stack = new Stack<SyntaxNode>();
            stack.Push(node);
            while (stack.Count > 0)
            {
                var n = stack.Pop();
                yield return n;
                foreach (var child in n.GetChildren().Reverse())
                {
                    stack.Push(child);
                }
            }
        }

        public void AssertToken(SyntaxKind kind, string text) =>
            AssertToken(token =>
            {
                Assert.Equal(kind, token.Kind);
                Assert.Equal(text, token.Text);
            });

        public void AssertToken(Action<SyntaxToken> action)
        {
            try
            {
                Assert.True(_enumerator.MoveNext());
                Assert.NotNull(_enumerator.Current);

                var token = Assert.IsType<SyntaxToken>(_enumerator.Current);
                action(token);
            }
            catch
            {
                _hasErrors = true;
                throw;
            }
        }

        public void AssertNode(SyntaxKind kind)
        {
            try
            {
                Assert.True(_enumerator.MoveNext());
                Assert.NotNull(_enumerator.Current);

                Assert.Equal(kind, _enumerator.Current.Kind);
                Assert.IsNotType<SyntaxToken>(_enumerator.Current);
            }
            catch
            {
                _hasErrors = true;
                throw;
            }
        }

        public void Dispose()
        {
            if (!_hasErrors)
            {
                var moveNextResult = _enumerator.MoveNext();
                Assert.False(moveNextResult, $"additional tokens remain: {_enumerator.Current.Kind}");
            }

            _enumerator?.Dispose();
        }
    }
}