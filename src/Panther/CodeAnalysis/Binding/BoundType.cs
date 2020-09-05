using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class BoundType : TypeSymbol
    {
        private readonly Dictionary<MethodSymbol, BoundBlockExpression> _methodDefinitions = new Dictionary<MethodSymbol, BoundBlockExpression>();

        public ImmutableDictionary<MethodSymbol, BoundBlockExpression> MethodDefinitions => _methodDefinitions.ToImmutableDictionary();

        public BoundType(string name)
            : base(name)
        {
        }

        public void DefineFunctionBody(MethodSymbol symbol, BoundBlockExpression loweredBody)
        {
            _methodDefinitions[symbol] = loweredBody;
        }
    }
}