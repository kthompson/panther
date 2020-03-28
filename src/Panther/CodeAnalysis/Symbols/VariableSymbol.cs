using System;

namespace Panther.CodeAnalysis.Symbols
{
    public sealed class VariableSymbol : Symbol
    {
        public bool IsReadOnly { get; }
        public TypeSymbol Type { get; }

        internal VariableSymbol(string name, bool isReadOnly, TypeSymbol type): base(name)
        {
            IsReadOnly = isReadOnly;
            Type = type;
        }

        public override string ToString()
        {
            var valOrVar = IsReadOnly ? "val" : "var";
            return $"{valOrVar} {Name}";
        }

        public override SymbolKind Kind => SymbolKind.Variable;
    }
}