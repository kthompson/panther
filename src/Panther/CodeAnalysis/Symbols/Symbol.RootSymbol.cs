using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Symbols;

public abstract partial class Symbol
{
    private sealed class RootSymbol : Symbol
    {
        public override Symbol Owner => this;
        public override bool IsRoot => true;

        public RootSymbol() : base(null, TextLocation.None,  "global::")
        {
            this.Type = Type.NoType;
        }
    }
}