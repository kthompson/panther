using System;
using System.Collections.Generic;
using Panther.CodeAnalysis.Binder;
using Xunit;

namespace Panther.Tests.CodeAnalysis.Binder;

class SymbolEnumerator(Symbol root) : IDisposable
{
    private readonly IEnumerator<Symbol> _enumerator = EnumerateSymbols(root, false)
        .GetEnumerator();

    private bool _hasErrors;

    public Symbol AssertSymbol(SymbolFlags flags, string name)
    {
        try
        {
            Assert.True(_enumerator.MoveNext());
            var current = _enumerator.Current;
            Assert.NotNull(current);
            Assert.Equal(flags, current.Flags);
            Assert.Equal(name, current.Name);
            return current;
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
            Assert.False(
                _enumerator.MoveNext(),
                $"Additional symbols remain: {_enumerator.Current.Name}"
            );
        }
        _enumerator.Dispose();
    }

    private static IEnumerable<Symbol> EnumerateSymbols(Symbol symbol, bool includeRoot = true)
    {
        if (includeRoot)
            yield return symbol;

        foreach (var child in symbol)
        foreach (var s in EnumerateSymbols(child))
        {
            yield return s;
        }
    }
}
