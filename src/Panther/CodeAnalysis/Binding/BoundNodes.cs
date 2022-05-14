using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Binding;

internal abstract partial record BoundExpression
{
    public abstract Type Type { get; init; }
}

internal partial record BoundAssignmentExpression
{
    public override Type Type { get; init; } = Type.Unit;
}

internal partial record BoundBinaryExpression
{
    public override Type Type { get; init; } = Operator.Type;
}

internal partial record BoundBlockExpression
{
    public override Type Type { get; init; } = Expression.Type;
}

internal partial record BoundCallExpression
{
    public override Type Type { get; init; } = Method.ReturnType;
}

internal partial record BoundFieldExpression
{
    public override Type Type { get; init; } = Field.Type;
}

internal partial record BoundIndexExpression
{
    public override Type Type { get; init; } = new IndexType(Expression.Type, Index.Type);
}

internal partial record BoundNamespaceExpression
{
    public override Type Type { get; init; } = Namespace.Type;
}

internal partial record BoundForExpression
{
    public override Type Type { get; init; } = Type.Unit;
}

internal partial record BoundGroupExpression
{
    public override Type Type { get; init; } = Expression.Type;
}

internal partial record BoundIfExpression
{
    public override Type Type { get; init; } = Then.Type;
}

internal partial record BoundNewExpression
{
    public override Type Type { get; init; } = Constructor.Owner.Type;
}

internal partial record BoundWhileExpression
{
    public override Type Type { get; init; } = Type.Unit;
}

internal partial record BoundUnaryExpression
{
    public override Type Type { get; init; } = Operator.Type;
}

internal partial record BoundUnitExpression
{
    public override Type Type { get; init; } = Type.Unit;
}

internal partial record BoundVariableExpression
{
    public override Type Type { get; init; } = Variable.Type;
}