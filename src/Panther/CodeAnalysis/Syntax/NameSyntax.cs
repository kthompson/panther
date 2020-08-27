using System.Collections.Generic;

namespace Panther.CodeAnalysis.Syntax
{
    public abstract partial class NameSyntax : ExpressionSyntax
    {
        protected NameSyntax(SyntaxTree syntaxTree) : base(syntaxTree)
        {
        }

        public abstract string ToText();
    }
}