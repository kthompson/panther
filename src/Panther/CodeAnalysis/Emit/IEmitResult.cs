using System.Collections.Immutable;

namespace Panther.CodeAnalysis.Emit;

public interface IEmitResult
{
    ImmutableArray<Diagnostic> Diagnostics { get; }
}