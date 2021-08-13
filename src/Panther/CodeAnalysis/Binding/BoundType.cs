using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Text;
using Type = Panther.CodeAnalysis.Symbols.Type;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class BoundType : TypeSymbol
    {
        public BoundType(Symbol owner, TextLocation location, string name)
            : base(owner, location, name)
        {
            this.Type = new ClassType(this);
        }
    }
}