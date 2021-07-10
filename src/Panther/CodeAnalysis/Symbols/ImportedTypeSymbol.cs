using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Symbols
{
    internal sealed class ImportedTypeSymbol : TypeSymbol
    {
        private readonly TypeDefinition _typeDefinition;

        private readonly Lazy<ImmutableArray<Symbol>> _members;
        private readonly Lazy<ImmutableDictionary<string, ImmutableArray<Symbol>>> _membersByName;

        public ImportedTypeSymbol(string name, TypeDefinition typeDefinition)
            : base(Symbol.None, TextLocation.None, name)
        {
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

            _membersByName = new Lazy<ImmutableDictionary<string, ImmutableArray<Symbol>>>(() =>
                GetMembers()
                    .GroupBy(x => x.Name,
                        (key, symbols) =>
                            new KeyValuePair<string, ImmutableArray<Symbol>>(key, symbols.ToImmutableArray()))
                    .ToImmutableDictionary());
        }


        public override ImmutableArray<Symbol> GetMembers() => _members.Value;

        public override ImmutableArray<Symbol> GetMembers(string name) =>
            _membersByName.Value.TryGetValue(name, out var symbols)
                ? symbols
                : ImmutableArray<Symbol>.Empty;

        private MethodSymbol? ImportMethodDefinition(MethodDefinition methodDefinition)
        {
            var parameters = ImmutableArray.CreateBuilder<ParameterSymbol>();

            for (int i = 0; i < methodDefinition.Parameters.Count; i++)
            {
                var p = methodDefinition.Parameters[i];
                // TODO: update handling for failure
                var pType = LookupTypeByMetadataName(p.ParameterType.FullName);
                if (pType == null)
                    return null;

                parameters.Add(new ParameterSymbol(p.Name, pType, i));
            }

            var returnType = LookupTypeByMetadataName(methodDefinition.ReturnType.FullName);
            if (returnType == null)
                return null;

            return new ImportedMethodSymbol(methodDefinition.Name,
                parameters.ToImmutableArray(),
                returnType
            );
        }

        private static TypeSymbol? LookupTypeByMetadataName(string name)
        {
            // TODO: we need to look up types other than these
            if (name == typeof(object).FullName)
                return TypeSymbol.Any;

            if (name == typeof(int).FullName)
                return TypeSymbol.Int;

            if (name == typeof(bool).FullName)
                return TypeSymbol.Bool;

            if (name == typeof(string).FullName)
                return TypeSymbol.String;

            if (name == typeof(void).FullName)
                return TypeSymbol.Unit;

            return null;
        }
    }
}