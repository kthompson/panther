using System;
using System.Collections.Immutable;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Symbols;

public abstract partial class Symbol
{
    private sealed class RootSymbol : Symbol, INamespaceOrTypeSymbol
    {
        public override Symbol Owner => this;
        public override bool IsRoot => true;

        public RootSymbol() : base(null, TextLocation.None, "global::")
        {
            this.Type = Type.NoType;
        }

        public ImmutableArray<ISymbol> GetMembers() => Members.As<ISymbol>();

        public ImmutableArray<ISymbol> GetMembers(string name) => LookupMembers(name).As<ISymbol>();

        public ImmutableArray<ITypeSymbol> GetTypeMembers()
        {
            throw new NotImplementedException();
        }

        public ImmutableArray<ITypeSymbol> GetTypeMembers(string name)
        {
            throw new NotImplementedException();
        }
    }
}
