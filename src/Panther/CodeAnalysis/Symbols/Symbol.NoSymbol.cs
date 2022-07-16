using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Symbols;

public abstract partial class Symbol
{
    private sealed class NoSymbol : Symbol
    {
        public NoSymbol() : base(null, TextLocation.None, "<none>")
        {
            this.Type = Type.NoType;
        }
    }
}
