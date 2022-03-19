using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Binding;

internal sealed record EntryPoint(bool IsScript, Symbol Symbol, BoundBlockExpression? Body);