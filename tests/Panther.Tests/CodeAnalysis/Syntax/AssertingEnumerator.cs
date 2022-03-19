using System;
using System.Collections.Generic;
using System.Linq;
using Panther.CodeAnalysis.Syntax;
using Xunit;

namespace Panther.Tests.CodeAnalysis.Syntax;

internal sealed class AssertingEnumerator : IDisposable
{
    private readonly IEnumerator<SyntaxNode> _enumerator;
    private bool _hasErrors;

    public AssertingEnumerator(SyntaxNode node)
    {
        _enumerator = node.DescendantsAndSelf().GetEnumerator();
    }

    public SyntaxToken AssertToken(SyntaxKind kind, string text) =>
        AssertToken(token =>
        {
            Assert.Equal(kind, token.Kind);
            Assert.Equal(text, token.Text);
        });

    public SyntaxToken AssertToken(Action<SyntaxToken> action)
    {
        try
        {
            Assert.True(_enumerator.MoveNext());
            Assert.NotNull(_enumerator.Current);

            var token = Assert.IsType<SyntaxToken>(_enumerator.Current);
            action(token);
            return token;
        }
        catch
        {
            _hasErrors = true;
            throw;
        }
    }

    public SyntaxTrivia AssertTrivia(SyntaxKind kind, string text) =>
        AssertTrivia(token =>
        {
            Assert.Equal(kind, token.Kind);
            Assert.Equal(text, token.Text);
        });

    public SyntaxTrivia AssertTrivia(SyntaxKind kind) =>
        AssertTrivia(token => Assert.Equal(kind, token.Kind));

    public SyntaxTrivia AssertTrivia(Action<SyntaxTrivia> action)
    {
        try
        {
            Assert.True(_enumerator.MoveNext());
            Assert.NotNull(_enumerator.Current);

            var token = Assert.IsType<SyntaxTrivia>(_enumerator.Current);
            action(token);
            return token;
        }
        catch
        {
            _hasErrors = true;
            throw;
        }
    }

    public SyntaxNode AssertNode(SyntaxKind kind)
    {
        try
        {
            Assert.True(_enumerator.MoveNext());
            Assert.NotNull(_enumerator.Current);

            Assert.Equal(kind, _enumerator.Current.Kind);
            Assert.IsNotType<SyntaxToken>(_enumerator.Current);
            return _enumerator.Current;
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