namespace Panther.CodeAnalysis.Binding;

internal sealed record TypedLabel(string Name)
{
    public bool Equals(TypedLabel? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return Name == other.Name;
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }

    public override string ToString() => Name;
}
