using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Panther.CodeAnalysis.Syntax;

public abstract class SeparatedSyntaxList
{
    public abstract ImmutableArray<SyntaxNode> GetWithSeparators();
}

public class SeparatedSyntaxList<T> : SeparatedSyntaxList, IReadOnlyList<T> where T : SyntaxNode
{
    private readonly ImmutableArray<SyntaxNode> _nodesAndSeparators;

    public SeparatedSyntaxList(ImmutableArray<SyntaxNode> nodesAndSeparators)
    {
        _nodesAndSeparators = nodesAndSeparators;
    }

    public override ImmutableArray<SyntaxNode> GetWithSeparators()
    {
        return _nodesAndSeparators;
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (var i = 0; i < Count; i++)
            yield return this[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => (_nodesAndSeparators.Length + 1) / 2;

    public T this[int index] => (T)_nodesAndSeparators[index * 2];
}
