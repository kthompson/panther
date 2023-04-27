using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Mono.Cecil;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Symbols;

internal sealed class ImportedTypeSymbol : Symbol
{
    private readonly TypeDefinition _typeDefinition;

    private bool _membersLoaded;
    private ImmutableArray<Symbol> _members;
    private ImmutableDictionary<string, ImmutableArray<Symbol>>? _membersByName;

    public ImportedTypeSymbol(Symbol parent, string name, TypeDefinition typeDefinition)
        : base(parent, TextLocation.None, name)
    {
        if (typeDefinition.IsClass)
            this.Flags |= SymbolFlags.Class;

        this.Flags |= SymbolFlags.Import;

        this.Type = new ClassType(this);

        _typeDefinition = typeDefinition;
    }

    public override ImmutableArray<Symbol> Members
    {
        get
        {
            EnsureInitialized();

            return _members;
        }
    }

    private void EnsureInitialized()
    {
        if (_membersLoaded)
            return;

        var builder = ImmutableArray.CreateBuilder<Symbol>();

        foreach (var method in _typeDefinition.Methods)
        {
            if (!method.IsPublic)
                continue;

            if (method.HasGenericParameters)
                continue;

            // if (method.IsStatic || method.IsConstructor)
            var imported = ImportMethodDefinition(method);
            if (imported == null)
                continue;

            builder.Add(imported);
        }

        _members = builder.ToImmutable();

        _membersByName = _members
            .GroupBy(
                x => x.Name,
                (key, symbols) =>
                    new KeyValuePair<string, ImmutableArray<Symbol>>(
                        key,
                        symbols.ToImmutableArray()
                    )
            )
            .ToImmutableDictionary();

        _membersLoaded = true;
    }

    public override ImmutableArray<Symbol> LookupMembers(string name)
    {
        EnsureInitialized();
        return _membersByName!.TryGetValue(name, out var symbols)
            ? symbols
            : ImmutableArray<Symbol>.Empty;
    }

    private Symbol? ImportMethodDefinition(MethodDefinition methodDefinition)
    {
        var method = this.NewMethod(TextLocation.None, methodDefinition.Name)
            .WithFlags(SymbolFlags.Static)
            .Declare();

        var parameters = ImmutableArray.CreateBuilder<Symbol>();

        for (var i = 0; i < methodDefinition.Parameters.Count; i++)
        {
            var p = methodDefinition.Parameters[i];
            // TODO: update handling for failure
            var pType = LookupTypeByMetadataName(p.ParameterType.FullName);
            if (pType == null)
                return null;

            parameters.Add(
                method.NewParameter(TextLocation.None, p.Name, i).WithType(pType).Declare()
            );
        }

        // TODO: this is probably wrong for the Type of a constructor
        var returnType = LookupTypeByMetadataName(methodDefinition.ReturnType.FullName);
        if (returnType != null)
        {
            method.Type = new MethodType(parameters.ToImmutable(), returnType);
        }

        return method;
    }

    private static Type? LookupTypeByMetadataName(string name)
    {
        if (name.EndsWith("[]"))
        {
            var elementName = name[..^2];
            var elementType = LookupTypeByMetadataName(elementName);
            if (elementType == null)
                return null;

            return Type.ArrayOf(elementType.Symbol);
        }

        // TODO: we need to look up types other than these
        if (name == typeof(object).FullName)
            return Type.Any;

        if (name == typeof(int).FullName)
            return Type.Int;

        if (name == typeof(bool).FullName)
            return Type.Bool;

        if (name == typeof(string).FullName)
            return Type.String;

        if (name == typeof(char).FullName)
            return Type.Char;

        if (name == typeof(void).FullName)
            return Type.Unit;

        return null;
    }
}
