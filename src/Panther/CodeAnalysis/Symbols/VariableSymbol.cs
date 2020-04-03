using System;

namespace Panther.CodeAnalysis.Symbols
{
    public class VariableSymbol : Symbol
    {
        public bool IsReadOnly { get; }
        public TypeSymbol Type { get; }

        protected internal VariableSymbol(string name, bool isReadOnly, TypeSymbol type)
            : base(name)
        {
            IsReadOnly = isReadOnly;
            Type = type;
        }

        public override string ToString()
        {
            var valOrVar = IsReadOnly ? "val" : "var";
            var name = string.IsNullOrWhiteSpace(Name) ? "?" : Name;
            return $"{valOrVar} {name}";
        }

        public override SymbolKind Kind => SymbolKind.Variable;
    }
}