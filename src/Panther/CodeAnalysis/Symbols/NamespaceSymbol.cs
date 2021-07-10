using System;
using System.Collections.Immutable;

namespace Panther.CodeAnalysis.Symbols
{
    public sealed class NamespaceSymbol : NamespaceOrTypeSymbol
    {
        public NamespaceSymbol(string name)
            : base(name)
        {
        }

        public override bool IsType => false;
    }
}