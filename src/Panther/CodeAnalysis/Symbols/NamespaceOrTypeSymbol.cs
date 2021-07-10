using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Symbols
{
    public abstract class NamespaceOrTypeSymbol : Symbol
    {
        protected NamespaceOrTypeSymbol(Symbol owner, TextLocation location, string name)
            : base(owner, location, name)
        {
        }
    }
}