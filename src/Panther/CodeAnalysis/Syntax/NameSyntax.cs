using System.Collections.Generic;

namespace Panther.CodeAnalysis.Syntax
{
    public abstract partial record NameSyntax
    {
        public abstract string ToText();
    }

    public sealed partial record QualifiedNameSyntax
    {
        public override string ToText() => $"{Left.ToText()}.{Right.ToText()}";
    }

    public sealed partial record IdentifierNameSyntax
    {
        public override string ToText() => Identifier.Text;
    }
}