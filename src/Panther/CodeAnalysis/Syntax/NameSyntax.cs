using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Panther.CodeAnalysis.Syntax;

public abstract partial record NameSyntax
{
    public abstract string ToText();

    public abstract IEnumerable<SimpleNameSyntax> ToIdentifierNames();
}

public sealed partial record QualifiedNameSyntax
{
    public override string ToText() => $"{Left.ToText()}.{Right.ToText()}";

    public override IEnumerable<SimpleNameSyntax> ToIdentifierNames() =>
        this.Left.ToIdentifierNames().Append(Right);
}

public sealed partial record IdentifierNameSyntax
{
    public override string ToText() => Identifier.Text;

    public override IEnumerable<SimpleNameSyntax> ToIdentifierNames()
    {
        yield return this;
    }
}

public sealed partial record GenericNameSyntax
{
    public override string ToText()
    {
        var sb = new StringBuilder();
        sb.Append(Identifier.Text).Append('<');

        using var iterator = TypeArgumentList.ArgumentList.GetEnumerator();
        if (iterator.MoveNext())
        {
            while (true)
            {
                sb.Append(iterator.Current.ToText());
                if (!iterator.MoveNext())
                    break;

                sb.Append(", ");
            }
        }

        sb.Append('>');
        return sb.ToString();
    }

    public override IEnumerable<SimpleNameSyntax> ToIdentifierNames()
    {
        yield return this;
    }
}
