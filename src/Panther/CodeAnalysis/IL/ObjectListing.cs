using System.Collections.Immutable;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.IL;

/// <summary>
/// Equivalent to an Object File
/// </summary>
/// <param name="EntryPoint"></param>
/// <param name="Instructions"></param>
/// <param name="SymbolMap">
/// Each Symbol points to the offset within the Instructions of a given function symbol
/// </param>
/// <param name="Data"></param>
public record ObjectListing(
    int? EntryPoint,
    ImmutableArray<Instruction> Instructions,
    ImmutableDictionary<Symbol, int> SymbolMap,
    ImmutableArray<Data> Data
);