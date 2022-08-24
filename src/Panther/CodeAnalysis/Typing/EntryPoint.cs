using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Typing;

internal sealed record EntryPoint(bool IsScript, Symbol Symbol, TypedBlockExpression? Body);
