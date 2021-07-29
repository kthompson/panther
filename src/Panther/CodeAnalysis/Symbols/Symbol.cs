using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Symbols
{
    [Flags]
    public enum SymbolFlags
    {
        Namespace = 1,
        Object = 1 << 1,
        Class = 1 << 2,
        Method = 1 << 3,
    }

    public abstract class Symbol
    {
        public SymbolFlags Flags { get; set; }
        public string Name { get; }

        public virtual bool IsRoot => false;
        public virtual bool IsType => false;
        public virtual bool IsNamespace => this.Flags.HasFlag(SymbolFlags.Namespace);
        public virtual bool IsClass => this.Flags.HasFlag(SymbolFlags.Class);

        public virtual Symbol Owner { get; }
        public virtual TextLocation Location { get; }
        // public abstract Type Type { get; }

        protected Symbol(Symbol? owner, TextLocation location, string name)
        {
            Owner = owner ?? this;
            Name = name;
            Location = location;
            _symbols = new Dictionary<string, ImmutableArray<Symbol>>();
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            WriteTo(writer);
            return writer.ToString();
        }

        public void WriteTo(TextWriter writer) =>
            SymbolPrinter.WriteTo(this, writer);

        /// <summary>
        /// The None symbol is a symbol to represent a value for when no valid symbol exists
        /// </summary>
        public static readonly Symbol None = new NoSymbol();

        public static Symbol NewRoot() => new RootSymbol();
        public Symbol NewAlias(TextLocation location, string name, Symbol target) => new AliasSymbol(this, location, name, target);
        public Symbol NewNamespace(TextLocation location, string name) => new TermSymbol(this, location, name).WithFlags(SymbolFlags.Namespace);
        public Symbol NewClass(TextLocation location, string name) => new TermSymbol(this, location, name).WithFlags(SymbolFlags.Class);
        public Symbol NewObject(TextLocation location, string name) => new TermSymbol(this, location, name).WithFlags(SymbolFlags.Object);

        private readonly Dictionary<string, ImmutableArray<Symbol>> _symbols;
        // public virtual ImmutableArray<Symbol> Parameters => GetMembers<ParameterSymbol>();

        public virtual bool DefineSymbol(Symbol symbol)
        {
            // only one field symbol per name
            if (symbol is FieldSymbol)
            {
                if (_symbols.ContainsKey(symbol.Name))
                    return false;

                _symbols.Add(symbol.Name, ImmutableArray.Create(symbol));
                return true;
            }

            if (_symbols.TryGetValue(symbol.Name, out var symbols))
            {
                _symbols[symbol.Name] = symbols.Add(symbol);
                return true;
            }

            _symbols.Add(symbol.Name, ImmutableArray.Create(symbol));
            return true;
        }

        public virtual ImmutableArray<TypeSymbol> GetTypeMembers() =>
            GetMembers().OfType<TypeSymbol>().ToImmutableArray();

        public virtual ImmutableArray<TypeSymbol> GetTypeMembers(string name) =>
            GetMembers(name).OfType<TypeSymbol>().ToImmutableArray();

        public virtual ImmutableArray<FieldSymbol> GetFieldMembers() =>
            GetMembers().OfType<FieldSymbol>().ToImmutableArray();

        ImmutableArray<Symbol> GetMembers<T>() where T : Symbol =>
            GetMembers().Where(symbol => symbol is T).ToImmutableArray();

        public virtual ImmutableArray<Symbol> GetMembers() =>
            (from symbolList in _symbols.Values
                from symbol in symbolList
                select symbol).ToImmutableArray();

        public virtual ImmutableArray<Symbol> GetMembers(string name) =>
            _symbols.TryGetValue(name, out var symbols)
                ? symbols
                : ImmutableArray<Symbol>.Empty;

        private sealed class RootSymbol : Symbol
        {
            public override Symbol Owner => this;
            // public override Type Type => Type.NoType;
            public override bool IsRoot => true;

            public RootSymbol() : base(null, TextLocation.None,  "global::")
            {
            }
        }

        private sealed class NoSymbol : Symbol
        {
            public NoSymbol() : base(null, TextLocation.None, "<none>")
            {
            }

            // public override Type Type => Type.NoType;
        }

        private sealed class TermSymbol : Symbol
        {
            public TermSymbol(Symbol owner, TextLocation location, string name)
                : base(owner, location, name)
            {
            }
            // public override Type Type => Type.NoType;
        }
    }

    public static class SymbolExtensions
    {
        public static T WithFlags<T>(this T symbol, SymbolFlags flags) where T : Symbol
        {
            symbol.Flags = flags;
            return symbol;
        }
    }

    /// <summary>
    /// An alias symbol is one like `string` that really represents the System.String
    /// </summary>
    public class AliasSymbol : Symbol
    {
        public Symbol Target { get; }
        public AliasSymbol(Symbol owner, TextLocation location, string name, Symbol target)
            : base(owner, location, name)
        {
            Target = target;
        }
    }

    internal class EmptySymbolScope : SymbolScope
    {
        public override void DefineSymbol<A>(A symbol)
        {
            throw new InvalidOperationException($"cannot define symbol on {nameof(EmptySymbolScope)}");
        }
    }

    internal class SymbolScope : IEnumerable<Symbol>
    {
        private readonly Dictionary<string, ImmutableArray<Symbol>> _symbols = new Dictionary<string, ImmutableArray<Symbol>>();

        public bool DefineSymbolUnique<A>(A symbol) where A : Symbol
        {
            if (Lookup(symbol.Name) != Symbol.None)
            {
                return false;
            }

            DefineSymbol(symbol);
            return true;
        }

        public virtual void DefineSymbol<A>(A symbol) where A : Symbol
        {
            if (_symbols.TryGetValue(symbol.Name, out var symbols))
            {
                _symbols[symbol.Name] = symbols.Add(symbol);
                return;
            }

            _symbols.Add(symbol.Name, ImmutableArray.Create<Symbol>(symbol));
        }

        public ImmutableArray<Symbol> LookupAll(string name) =>
            _symbols.TryGetValue(name, out var symbols)
                ? symbols
                : ImmutableArray<Symbol>.Empty;

        public Symbol Lookup(string name) =>
            (_symbols.TryGetValue(name, out var symbols))
                ? symbols.First()
                : Symbol.None;

        public IEnumerator<Symbol> GetEnumerator()
        {
            foreach (var symbols in _symbols)
            {
                foreach (var symbol in symbols.Value)
                {
                    yield return symbol;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}