using System.Collections.Immutable;

namespace Panther.CodeAnalysis.Symbols
{
    public abstract class TypeOrNamespaceSymbol : Symbol
    {
        public abstract ImmutableArray<Symbol> GetMembers();
        public abstract ImmutableArray<Symbol> GetMembers(string name);
        public abstract ImmutableArray<TypeSymbol> GetTypeMembers();
        public abstract ImmutableArray<TypeSymbol> GetTypeMembers(string name);

        public abstract bool IsType { get; }
        public bool IsNamespace => !IsType;

        protected TypeOrNamespaceSymbol(string name)
            : base(name)
        {
        }
    }
}