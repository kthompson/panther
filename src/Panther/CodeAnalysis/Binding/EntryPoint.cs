using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class EntryPoint
    {
        public bool IsScript { get; }
        public MethodSymbol Symbol { get; }

        public EntryPoint(bool isScript, MethodSymbol symbol)
        {
            IsScript = isScript;
            Symbol = symbol;
        }
    }
}