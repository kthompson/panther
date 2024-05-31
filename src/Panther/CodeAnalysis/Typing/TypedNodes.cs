using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Typing;

internal abstract partial record TypedExpression
{
    public abstract Type Type { get; init; }
}

internal partial record TypedAssignmentExpression
{
    public override Type Type { get; init; } = Type.Unit;
}

internal partial record TypedArrayCreationExpression
{
    public override Type Type { get; init; } = Type.ArrayOf(ElementType);
}

internal partial record TypedBinaryExpression
{
    public override Type Type { get; init; } = Operator.Type;
}

internal partial record TypedBlockExpression
{
    public override Type Type { get; init; } = Expression.Type;
}

internal partial record TypedCallExpression
{
    public override Type Type { get; init; } = Method.ReturnType;
}

internal partial record TypedFieldExpression
{
    public override Type Type { get; init; } = Field.Type;
}

internal partial record TypedIndexExpression
{
    public override Type Type { get; init; } = new ApplyType(Expression.Type, Index.Type);
}

internal partial record TypedNamespaceExpression
{
    public override Type Type { get; init; } = Namespace.Type;
}

internal partial record TypedForExpression
{
    public override Type Type { get; init; } = Type.Unit;
}

internal partial record TypedGroupExpression
{
    public override Type Type { get; init; } = Expression.Type;
}

internal partial record TypedIfExpression
{
    public override Type Type { get; init; } = Then.Type;
}

internal partial record TypedNewExpression
{
    public override Type Type { get; init; } = Constructor.Owner.Type;
}

internal partial record TypedPropertyExpression
{
    public override Type Type { get; init; } = Property.Type;
}

internal partial record TypedWhileExpression
{
    public override Type Type { get; init; } = Type.Unit;
}

internal partial record TypedUnaryExpression
{
    public override Type Type { get; init; } = Operator.Type;
}

internal partial record TypedUnitExpression
{
    public override Type Type { get; init; } = Type.Unit;
}

internal partial record TypedVariableExpression
{
    public override Type Type { get; init; } = Variable.Type;
}

internal partial record TypedNullExpression
{
    public override Type Type { get; init; } = Type.Null;
}
