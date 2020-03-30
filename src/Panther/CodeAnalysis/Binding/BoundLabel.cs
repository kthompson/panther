namespace Panther.CodeAnalysis.Binding
{
    public sealed class BoundLabel
    {
        public string Name { get; }

        internal BoundLabel(string name)
        {
            Name = name;
        }

        public override string ToString() => Name;
    }
}