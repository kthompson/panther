using System.Collections.Immutable;

namespace Panther.CodeAnalysis.Emit
{
    public interface IEmitResult
    {
        string? OutputPath { get; }
        ImmutableArray<Diagnostic> Diagnostics { get; }
    }
}