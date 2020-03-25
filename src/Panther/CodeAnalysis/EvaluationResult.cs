using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Panther.CodeAnalysis
{
    public sealed class EvaluationResult
    {
        public object? Value { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }

        public EvaluationResult(ImmutableArray<Diagnostic> diagnostics, object? value)
        {
            Value = value;
            Diagnostics = diagnostics;
        }
    }
}