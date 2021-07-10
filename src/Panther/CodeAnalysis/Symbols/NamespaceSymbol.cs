using System;
using System.Collections.Immutable;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Symbols
{
    public sealed class NamespaceSymbol : NamespaceOrTypeSymbol
    {
        public NamespaceSymbol(string name)
            : base(Symbol.None, TextLocation.None, name)
        {
        }

        public override bool IsNamespace => true;
    }
}