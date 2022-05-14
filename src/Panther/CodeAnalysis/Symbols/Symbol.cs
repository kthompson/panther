using System.Collections.Generic;
using System.IO;
using System.Linq;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Symbols;

public abstract partial class Symbol : SymbolContainer
{
    public virtual SymbolFlags Flags { get; set; }
    public override string Name { get; }

    public virtual bool IsRoot => false;

    public bool IsType => IsClass || IsObject;
    public bool IsNamespace => this.Flags.HasFlag(SymbolFlags.Namespace);
    public bool IsClass => this.Flags.HasFlag(SymbolFlags.Class);
    public bool IsObject => this.Flags.HasFlag(SymbolFlags.Object);
    public bool IsMember => (this.Flags & SymbolFlags.Member) != 0;
    public bool IsMethod => this.Flags.HasFlag(SymbolFlags.Method);
    public bool IsConstructor => this.Name is ".ctor" or ".cctor";
    public bool IsField => this.Flags.HasFlag(SymbolFlags.Field);
    public bool IsImport => this.Flags.HasFlag(SymbolFlags.Import);
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

    public Symbol NewNamespace(TextLocation location, string name)
    {
        var symbol = NewTerm(location, name, SymbolFlags.Namespace);
        return symbol.WithType(new NamespaceType(symbol));
    }

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


    public Symbol Declare()
    {
        Owner.DefineSymbol(this);
        return this;
    }

    public void Delete() => Owner.Delete(this);


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
}