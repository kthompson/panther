namespace Panther.CodeAnalysis.Binding
{
    internal sealed class BoundConstant
    {
        public object Value { get; }

        public BoundConstant(object value)
        {
            Value = value;
        }
    }
}