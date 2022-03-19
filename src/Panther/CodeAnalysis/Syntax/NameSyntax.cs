using System;
using System.Collections.Generic;
using System.Linq;

namespace Panther.CodeAnalysis.Syntax;

public abstract partial record NameSyntax
{
    public abstract string ToText();

    public abstract IEnumerable<IdentifierNameSyntax> ToIdentifierNames();
}

public sealed partial record QualifiedNameSyntax
{
    public override string ToText() => $"{Left.ToText()}.{Right.ToText()}";

    public override IEnumerable<IdentifierNameSyntax> ToIdentifierNames() => this.Left.ToIdentifierNames().Append(Right);
}

public sealed partial record IdentifierNameSyntax
{
    public override string ToText() => Identifier.Text;

    public override IEnumerable<IdentifierNameSyntax> ToIdentifierNames()
    {
        yield return this;
    }
}