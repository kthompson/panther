using System;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    internal abstract class BoundExpression : BoundNode
    {
        public abstract TypeSymbol Type { get; }
        public virtual BoundConstant? ConstantValue { get; }

        protected BoundExpression(SyntaxNode syntax)
            : base(syntax)
        {
        }
    }
}