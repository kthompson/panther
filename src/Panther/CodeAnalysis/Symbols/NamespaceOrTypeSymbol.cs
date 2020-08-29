using System.Collections.Immutable;

namespace Panther.CodeAnalysis.Symbols
{
    public abstract class NamespaceOrTypeSymbol : Symbol
    {
        public abstract ImmutableArray<Symbol> GetMembers();
        public abstract ImmutableArray<Symbol> GetMembers(string name);
        public ImmutableArray<TypeSymbol> GetTypeMembers() =>
            GetMembers().OfType<TypeSymbol>().ToImmutableArray();
        public ImmutableArray<TypeSymbol> GetTypeMembers(string name) =>
            GetMembers(name).OfType<TypeSymbol>().ToImmutableArray();

        public abstract bool IsType { get; }
        public bool IsNamespace => !IsType;

        protected NamespaceOrTypeSymbol(string name)
            : base(name)
        {
        }
    }
}