using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Symbols;

public abstract partial class Symbol
{
    private sealed class TermSymbol : Symbol
    {
        public TermSymbol(Symbol owner, TextLocation location, string name)
            : base(owner, location, name) { }

        public override string ToString()
        {
            return $"Symbol: {FullName}";
        }
    }
}
