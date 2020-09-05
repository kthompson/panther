using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class EntryPoint
    {
        public bool IsScript { get; }
        public MethodSymbol Symbol { get; }
        public BoundBlockExpression? Body { get; }

        public EntryPoint(bool isScript, MethodSymbol symbol, BoundBlockExpression? body)
        {
            IsScript = isScript;
            Symbol = symbol;
            Body = body;
        }
    }
}