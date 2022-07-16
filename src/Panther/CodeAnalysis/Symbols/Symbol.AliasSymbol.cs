using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Symbols;

public abstract partial class Symbol
{
    /// <summary>
    /// An alias symbol is one like `string` that really represents the System.String
    ///
    /// Also allows us to rename something such as:
    /// type u64 = System.UInt64
    /// </summary>
    private sealed class AliasSymbol : Symbol
    {
        public Symbol Target { get; }

        public override SymbolFlags Flags
        {
            get => Target.Flags;
            set { }
        }

        public override Type Type
        {
            get => Target.Type;
            set { }
        }

        public AliasSymbol(Symbol owner, TextLocation location, string name, Symbol target)
            : base(owner, location, name)
        {
            Target = target;
        }
    }
}
