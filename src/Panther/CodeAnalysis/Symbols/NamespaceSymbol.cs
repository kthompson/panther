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

        public override SymbolKind Kind => SymbolKind.Namespace;

        public override ImmutableArray<Symbol> GetMembers()
        {
            throw new NotImplementedException();
        }

        public override ImmutableArray<Symbol> GetMembers(string name)
        {
            throw new NotImplementedException();
        }

        public override bool IsType => false;
    }
}