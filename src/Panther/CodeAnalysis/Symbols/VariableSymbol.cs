using System;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Symbols
{
    [Obsolete]
    public abstract class VariableSymbol : Symbol
    {
        internal BoundConstant? ConstantValue { get; }

        internal VariableSymbol(string name, bool isReadOnly, Type type, BoundConstant? constantValue)
            : base(Symbol.None, TextLocation.None, name)
        {
            if(isReadOnly)
            {
                this.Flags |= SymbolFlags.Readonly;
            }

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