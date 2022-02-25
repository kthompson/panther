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
                Members
                    .GroupBy(x => x.Name,
                        (key, symbols) =>
                            new KeyValuePair<string, ImmutableArray<Symbol>>(key, symbols.ToImmutableArray()))
                    .ToImmutableDictionary());
        }

        public override ImmutableArray<Symbol> Members => _members.Value;

        public override ImmutableArray<Symbol> LookupMembers(string name) =>
            _membersByName.Value.TryGetValue(name, out var symbols)
                ? symbols
                : ImmutableArray<Symbol>.Empty;

        private Symbol? ImportMethodDefinition(MethodDefinition methodDefinition)
        {
            var method = this
                .NewMethod(TextLocation.None, methodDefinition.Name)
                .Declare();

            for (var i = 0; i < methodDefinition.Parameters.Count; i++)
            {
                var p = methodDefinition.Parameters[i];
                // TODO: update handling for failure
                var pType = LookupTypeByMetadataName(p.ParameterType.FullName);
                if (pType == null)
                    return null;

                method
                    .NewParameter(TextLocation.None, p.Name, i)
                    .WithType(pType)
                    .Declare();
            }

            var returnType = LookupTypeByMetadataName(methodDefinition.ReturnType.FullName);
            if (returnType != null)
            {
                method.Type = returnType;
            }

            return method;
        }

        private static Type? LookupTypeByMetadataName(string name)
        {
            // TODO: we need to look up types other than these
            if (name == typeof(object).FullName)
                return Type.Any;

            if (name == typeof(int).FullName)
                return Type.Int;

            if (name == typeof(bool).FullName)
                return Type.Bool;

            if (name == typeof(string).FullName)
                return Type.String;

            if (name == typeof(void).FullName)
                return Type.Unit;

            return null;
        }
    }
}