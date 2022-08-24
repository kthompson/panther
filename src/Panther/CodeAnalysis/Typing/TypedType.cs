using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Typing;

internal sealed class TypedType : TypeSymbol
{
    public TypedType(Symbol owner, TextLocation location, string name) : base(owner, location, name)
    {
        Type = new ClassType(this);
    }
}
