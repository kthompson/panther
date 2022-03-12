using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Symbols
{
    public abstract class Symbol
    {
        public virtual SymbolFlags Flags { get; set; }
        public string Name { get; }

        public virtual bool IsRoot => false;

        public bool IsType => IsClass || IsObject;
        public bool IsNamespace => this.Flags.HasFlag(SymbolFlags.Namespace);
        public bool IsClass => this.Flags.HasFlag(SymbolFlags.Class);
        public bool IsObject => this.Flags.HasFlag(SymbolFlags.Object);
        public bool IsMember => (this.Flags & SymbolFlags.Member) != 0;
        public bool IsMethod => this.Flags.HasFlag(SymbolFlags.Method);
        public bool IsConstructor => this.Name is ".ctor" or ".cctor";
        public bool IsField => this.Flags.HasFlag(SymbolFlags.Field);
        public bool IsParameter => this.Flags.HasFlag(SymbolFlags.Parameter);
        public bool IsLocal => this.Flags.HasFlag(SymbolFlags.Local);
        public bool IsValue => IsLocal || IsParameter || IsField;

        public bool IsStatic => this.Flags.HasFlag(SymbolFlags.Static);
        public bool IsReadOnly => this.Flags.HasFlag(SymbolFlags.Readonly);

        public virtual Symbol Owner { get; }
        public int Index { get; set; } = 0;
        public virtual Type Type { get; set; } = Type.Unresolved;
        public virtual TextLocation Location { get; }

        public string FullName
        {
            get
            {
                List<Symbol> AncestorsAndSelf(List<Symbol> list, Symbol symbol)
                {
                    while (true)
                    {
                        if (symbol.IsRoot || symbol == None)
                        {
                            return list;
                        }

                        list.Insert(0, symbol);

                        symbol = symbol.Owner;
                    }
                }

                var ancestors = AncestorsAndSelf(new List<Symbol>(), this);

                return string.Join(".", ancestors.Select(ancestor => ancestor.Name));
            }
        }

        protected Symbol(Symbol? owner, TextLocation location, string name)
        {
            Owner = owner ?? this;
            Name = name;
            Location = location;
            _symbolMap = new Dictionary<string, ImmutableArray<Symbol>>();
            _symbols = new List<Symbol>();
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

        public Symbol NewTerm(TextLocation location, string name, SymbolFlags flags) =>
            new TermSymbol(this, location, name)
                .WithFlags(flags);

        public Symbol NewNamespace(TextLocation location, string name) =>
            new TermSymbol(this, location, name)
                .WithFlags(SymbolFlags.Namespace);

        public Symbol NewClass(TextLocation location, string name)
        {
            var symbol = NewTerm(location, name, SymbolFlags.Class);
            return symbol.WithType(new ClassType(symbol));
        }

        public Symbol NewObject(TextLocation location, string name) => new TermSymbol(this, location, name)
            .WithFlags(SymbolFlags.Object);
        public Symbol NewField(TextLocation location, string name, bool isReadOnly) =>
            new TermSymbol(this, location, name)
                .WithFlags(SymbolFlags.Field| (isReadOnly ? SymbolFlags.Readonly : SymbolFlags.None));

        public Symbol NewMethod(TextLocation location, string name) =>
            new TermSymbol(this, location, name).WithFlags(SymbolFlags.Method);

        public Symbol NewParameter(TextLocation location, string name, int index) =>
            new TermSymbol(this, location, name) { Index = index }
                .WithFlags(SymbolFlags.Parameter | SymbolFlags.Readonly);

        public Symbol NewLocal(TextLocation location, string name, bool isReadOnly) =>
            new TermSymbol(this, location, name)
                .WithFlags(SymbolFlags.Local | (isReadOnly ? SymbolFlags.Readonly : SymbolFlags.None));

        private readonly Dictionary<string, ImmutableArray<Symbol>> _symbolMap;
        private readonly List<Symbol> _symbols;

        public Symbol Declare()
        {
            Owner.DefineSymbol(this);
            return this;
        }

        public void Delete() => Owner.Delete(this);

        public void Delete(Symbol child)
        {
            if (_symbols.Remove(child))
            {
                _symbolMap[child.Name] = _symbolMap[child.Name].Remove(child);
            }
        }

        public bool DefineSymbol(Symbol symbol)
        {
            // only one field symbol per name
            if ((symbol.Flags & SymbolFlags.Field) != 0)
            {
                if (_symbolMap.ContainsKey(symbol.Name))
                    return false;

                _symbolMap.Add(symbol.Name, ImmutableArray.Create(symbol));
                _symbols.Add(symbol);
                return true;
            }

            if (_symbolMap.TryGetValue(symbol.Name, out var symbols))
            {
                _symbolMap[symbol.Name] = symbols.Add(symbol);
                _symbols.Add(symbol);
                return true;
            }

            _symbolMap.Add(symbol.Name, ImmutableArray.Create(symbol));
            _symbols.Add(symbol);
            return true;
        }

        public ImmutableArray<Symbol> Locals  =>
            Members.Where(m => m.IsLocal).ToImmutableArray();

        public ImmutableArray<Symbol> Parameters =>
            Members.Where(m => m.IsParameter).ToImmutableArray();

        public ImmutableArray<Symbol> Constructors =>
            Members.Where(m => m.IsConstructor).ToImmutableArray();

        public ImmutableArray<Symbol> Methods =>
            Members.Where(sym => sym.IsMethod).ToImmutableArray();

        public ImmutableArray<Symbol> Fields =>
            Members.Where(sym => sym.IsField).ToImmutableArray();

        public ImmutableArray<Symbol> Types =>
            Members.Where(m => m.IsType).ToImmutableArray();

        public ImmutableArray<Symbol> Namespaces =>
            Members.Where(m => m.IsNamespace).ToImmutableArray();

        public virtual ImmutableArray<Symbol> Members => _symbols.ToImmutableArray();

        public Type ReturnType
        {
            get
            {
                if (Type is MethodType m)
                {
                    return m.ResultType;
                }

                return Type;
            }
        }

        public virtual ImmutableArray<Symbol> LookupMembers(string name) =>
            _symbolMap.TryGetValue(name, out var symbols)
                ? symbols
                : ImmutableArray<Symbol>.Empty;

        public Symbol? LookupSingle(string name, Predicate<Symbol> predicate) =>
            LookupMembers(name).SingleOrDefault(m => predicate(m));

        public Symbol? LookupNamespace(string name) =>
            LookupSingle(name, m => m.IsNamespace);

        public Symbol? LookupParameter(string name) =>
            LookupSingle(name, m => m.IsParameter);

        public Symbol? LookupVariable(string name) =>
            LookupSingle(name, m => m.IsLocal);

        public Symbol? LookupType(string name) =>
            LookupSingle(name, m => m.IsType);

        public ImmutableArray<Symbol> LookupMethod(string name) =>
            LookupMembers(name).Where(m => m.IsMethod).ToImmutableArray();

        private sealed class RootSymbol : Symbol
        {
            public override Symbol Owner => this;
            public override bool IsRoot => true;

            public RootSymbol() : base(null, TextLocation.None,  "global::")
            {
                this.Type = Type.NoType;
            }
        }

        private sealed class NoSymbol : Symbol
        {
            public NoSymbol() : base(null, TextLocation.None, "<none>")
            {
                this.Type = Type.NoType;
            }
        }

        private sealed class TermSymbol : Symbol
        {
            public TermSymbol(Symbol owner, TextLocation location, string name)
                : base(owner, location, name)
            {
            }
        }

        /// <summary>
        /// An alias symbol is one like `string` that really represents the System.String
        ///
        /// Also allows us to rename something such as:
        /// type u64 = System.UInt64
        /// </summary>
        private sealed class AliasSymbol : Symbol
        {
            public Symbol Target { get; }

            public override SymbolFlags Flags
            {
                get => Target.Flags;
                set { }
            }

            public override Type Type
            {
                get => Target.Type;
                set { }
            }

            public AliasSymbol(Symbol owner, TextLocation location, string name, Symbol target)
                : base(owner, location, name)
            {
                Target = target;
            }
        }
    }
}