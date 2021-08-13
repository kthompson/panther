using System;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;
using Type = Panther.CodeAnalysis.Symbols.Type;

namespace Panther.CodeAnalysis.Binding
{
    internal abstract record BoundExpression : BoundNode
    {
        public abstract Type Type { get; init; }
        public virtual BoundConstant? ConstantValue { get; }

        protected BoundExpression(SyntaxNode syntax)
            : base(syntax)
        {
        }
    }
}