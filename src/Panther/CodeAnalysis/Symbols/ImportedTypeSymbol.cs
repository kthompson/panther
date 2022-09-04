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

    private readonly Lazy<ImmutableArray<Symbol>> _members;
    private readonly Lazy<ImmutableDictionary<string, ImmutableArray<Symbol>>> _membersByName;

    public ImportedTypeSymbol(Symbol parent, string name, TypeDefinition typeDefinition)
        : base(parent, TextLocation.None, name)
    {
        if (typeDefinition.IsClass)
            this.Flags |= SymbolFlags.Class;

        this.Flags |= SymbolFlags.Import;

        this.Type = new ClassType(this);

        _typeDefinition = typeDefinition;
        _members = new Lazy<ImmutableArray<Symbol>>(() =>
        {
            var builder = ImmutableArray.CreateBuilder<Symbol>();

            foreach (var method in _typeDefinition.Methods)
            {
                if (!method.IsPublic || !method.IsStatic)
                    continue;

                var imported = ImportMethodDefinition(method);
                if (imported == null)
                    continue;

                builder.Add(imported);
            }

            return builder.ToImmutable();
        });

        _membersByName = new Lazy<ImmutableDictionary<string, ImmutableArray<Symbol>>>(
            () =>
                Members
                    .GroupBy(
                        x => x.Name,
                        (key, symbols) =>
                            new KeyValuePair<string, ImmutableArray<Symbol>>(
                                key,
                                symbols.ToImmutableArray()
                            )
                    )
                    .ToImmutableDictionary()
        );
    }

    public override ImmutableArray<Symbol> Members => _members.Value;

    public override ImmutableArray<Symbol> LookupMembers(string name) =>
        _membersByName.Value.TryGetValue(name, out var symbols)
            ? symbols
            : ImmutableArray<Symbol>.Empty;

    private Symbol? ImportMethodDefinition(MethodDefinition methodDefinition)
    {
        var method = this.NewMethod(TextLocation.None, methodDefinition.Name).Declare();

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
            var elementName = name.Substring(0, name.Length - 2);
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
