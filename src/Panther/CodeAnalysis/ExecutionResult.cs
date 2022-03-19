using System.Collections.Immutable;

namespace Panther.CodeAnalysis;

public class ExecutionResult
{
    public ExecutionResult(ImmutableArray<Diagnostic> diagnostics, object? value)
    {
        Diagnostics = diagnostics;
        Value = value;
    }

    public object? Value { get; }

    public ImmutableArray<Diagnostic> Diagnostics { get; }
}