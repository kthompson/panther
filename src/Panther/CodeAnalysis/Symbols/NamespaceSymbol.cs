using System;
using System.Collections.Immutable;

namespace Panther.CodeAnalysis.Symbols
{
    public sealed class NamespaceSymbol : TypeOrNamespaceSymbol
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

        public override ImmutableArray<TypeSymbol> GetTypeMembers()
        {
            throw new NotImplementedException();
        }

        public override ImmutableArray<TypeSymbol> GetTypeMembers(string name)
        {
            throw new NotImplementedException();
        }

        public override bool IsType => false;
    }
}