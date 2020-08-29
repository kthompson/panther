using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class BoundType: TypeSymbol
    {
        private readonly Dictionary<string, FieldSymbol> _fields = new Dictionary<string, FieldSymbol>();
        private readonly Dictionary<string, MethodSymbol> _methods = new Dictionary<string, MethodSymbol>();
        private readonly Dictionary<MethodSymbol, BoundBlockExpression> _methodDefinitions = new Dictionary<MethodSymbol, BoundBlockExpression>();

        public ImmutableDictionary<MethodSymbol, BoundBlockExpression> MethodDefinitions => _methodDefinitions.ToImmutableDictionary();

        public BoundType(string ns, string name)
            : base(ns, name)
        {
        }

        public bool TryDeclareFunction(MethodSymbol symbol)
        {
            if (_methods.ContainsKey(symbol.Name))
                return false;

            _methods[symbol.Name] = symbol;
            return true;
        }

        public override ImmutableArray<Symbol> GetMembers() =>
            _methods.Values.Cast<Symbol>().Concat(_fields.Values).ToImmutableArray();

        public override ImmutableArray<Symbol> GetMembers(string name) =>
            GetMembers().Where(x => x.Name == name).ToImmutableArray();

        public void DefineFunction(MethodSymbol symbol, BoundBlockExpression loweredBody)
        {
            _methodDefinitions[symbol] = loweredBody;
        }

        public void DefineField(FieldSymbol symbol)
        {
            _fields[symbol.Name] = symbol;
        }
    }
}