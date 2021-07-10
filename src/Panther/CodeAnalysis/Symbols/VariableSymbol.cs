using System;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Symbols
{
    public abstract class VariableSymbol : Symbol
    {
        public bool IsReadOnly { get; }
        public TypeSymbol Type { get; }
        internal BoundConstant? ConstantValue { get; }

        internal VariableSymbol(string name, bool isReadOnly, TypeSymbol type, BoundConstant? constantValue)
            : base(Symbol.None, TextLocation.None, name)
        {
            IsReadOnly = isReadOnly;
            Type = type;
            ConstantValue = isReadOnly ? constantValue : null;
        }

        public override string ToString()
        {
            var valOrVar = IsReadOnly ? "val" : "var";
            var name = string.IsNullOrWhiteSpace(Name) ? "?" : Name;
            return $"{valOrVar} {name}";
        }
    }
}