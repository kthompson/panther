using System.Collections.Generic;
using System.Collections.Immutable;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class BoundType : TypeSymbol
    {
        private readonly Dictionary<MethodSymbol, BoundBlockExpression> _methodDefinitions = new Dictionary<MethodSymbol, BoundBlockExpression>();

        public ImmutableDictionary<MethodSymbol, BoundBlockExpression> MethodDefinitions => _methodDefinitions.ToImmutableDictionary();

        public BoundType(Symbol owner, TextLocation location, string name)
            : base(owner, location, name)
        {
        }

        public void DefineFunctionBody(MethodSymbol symbol, BoundBlockExpression loweredBody)
        {
            _methodDefinitions[symbol] = loweredBody;
        }
    }
}