using System.Collections.Immutable;
using System.Linq;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Text;
using Panther.CodeAnalysis.Typing;

namespace Panther.CodeAnalysis.Lowering;

sealed class IndexExpressions
{
    private readonly Symbol _method;

    private IndexExpressions(Symbol method)
    {
        _method = method;
    }

    public static TypedStatement Lower(Symbol method, TypedStatement statement) =>
        Getters.Lower(Setters.Lower(ArrayCreation.Lower(method, statement)));

    class ArrayCreation : TypedTreeRewriter
    {
        private readonly Symbol _method;

        public ArrayCreation(Symbol method)
        {
            _method = method;
        }

        private int _variableCount;

        private Symbol GenerateVariable(Type type)
        {
            _variableCount++;
            var name = $"variable$ArrayCreation${_variableCount}";

            return _method.NewLocal(TextLocation.None, name, false).WithType(type).Declare();
        }

        protected override TypedExpression RewriteArrayCreationExpression(
            TypedArrayCreationExpression node
        )
        {
            // if there are no initializers then ignore this Array expression
            if (node.Expressions.Length == 0)
                return base.RewriteArrayCreationExpression(node);

            // create a temp variable for the array
            var variable = GenerateVariable(node.Type);
            var varExpr = new TypedVariableExpression(node.Syntax, variable);

            // create index assignment expressions to assign each of the inner expressions
            TypedStatement ToAssignment(
                TypedExpression variable,
                TypedExpression expression,
                int index
            ) =>
                new TypedAssignmentStatement(
                    expression.Syntax,
                    new TypedIndexExpression(
                        node.Syntax,
                        variable,
                        new TypedLiteralExpression(node.Syntax, index),
                        null,
                        null
                    ),
                    expression
                );

            var statements = ImmutableArray.CreateBuilder<TypedStatement>();
            statements.Add(
                new TypedVariableDeclarationStatement(
                    node.Syntax,
                    variable,
                    new TypedArrayCreationExpression(
                        node.Syntax,
                        node.ElementType,
                        node.ArraySize,
                        ImmutableArray<TypedExpression>.Empty
                    )
                )
            );

            for (var index = 0; index < node.Expressions.Length; index++)
            {
                var expression = node.Expressions[index];
                statements.Add(ToAssignment(varExpr, expression, index));
            }

            return RewriteExpression(
                new TypedBlockExpression(
                    node.Syntax,
                    statements.ToImmutable(),
                    new TypedVariableExpression(node.Syntax, variable)
                )
            );
        }

        public static TypedStatement Lower(Symbol method, TypedStatement statement) =>
            new ArrayCreation(method).RewriteStatement(statement);
    }

    class Setters : TypedTreeRewriter
    {
        protected override TypedExpression RewriteAssignmentExpression(
            TypedAssignmentExpression node
        )
        {
            if (node.Left is TypedIndexExpression { Setter: { } } indexExpression)
            {
                return new TypedCallExpression(
                    node.Syntax,
                    indexExpression.Setter,
                    RewriteExpression(indexExpression.Expression),
                    ImmutableArray.Create(
                        RewriteExpression(indexExpression.Index),
                        RewriteExpression(node.Right)
                    )
                );
            }

            return node;
        }

        public static TypedStatement Lower(TypedStatement statement) =>
            new Setters().RewriteStatement(statement);
    }

    class Getters : TypedTreeRewriter
    {
        protected override TypedExpression RewriteIndexExpression(
            TypedIndexExpression indexExpression
        )
        {
            if (indexExpression.Getter == null)
                return indexExpression;

            return new TypedCallExpression(
                indexExpression.Syntax,
                indexExpression.Getter,
                RewriteExpression(indexExpression.Expression),
                ImmutableArray.Create(RewriteExpression(indexExpression.Index))
            );
        }

        public static TypedStatement Lower(TypedStatement statement) =>
            new Getters().RewriteStatement(statement);
    }
}
