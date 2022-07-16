using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Panther.CodeAnalysis;

public static class ImmutableArrayExtensions
{
    public static (ImmutableArray<A> matches, ImmutableArray<A> nonMatches) Partition<A>(
        this IEnumerable<A> array,
        Predicate<A> predicate
    )
    {
        var lefts = ImmutableArray.CreateBuilder<A>();
        var rights = ImmutableArray.CreateBuilder<A>();

        foreach (var item in array)
        {
            var list = predicate(item) ? lefts : rights;
            list.Add(item);
        }

        return (lefts.ToImmutableArray(), rights.ToImmutableArray());
    }
}
