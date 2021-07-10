using System.IO;

namespace Panther.CodeAnalysis.Symbols
{
    public abstract class Symbol
    {
        public string Name { get; }

        protected Symbol(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            WriteTo(writer);
            return writer.ToString();
        }

        public void WriteTo(TextWriter writer) =>
            SymbolPrinter.WriteTo(this, writer);
    }
}