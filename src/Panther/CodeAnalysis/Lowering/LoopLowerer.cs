using System.Collections.Immutable;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Lowering;

internal sealed class LoopLowerer : TypedTreeRewriter
{
    private readonly Symbol _method;
    private int _labelCount;
    private int _variableCount;

    private enum LabelToken { }

    private LoopLowerer(Symbol method)
    {
        _method = method;
    }

    public static TypedStatement Lower(Symbol method, TypedStatement statement) =>
        new LoopLowerer(method).RewriteStatement(statement);

    private LabelToken GenerateLabelToken()
    {
        _labelCount++;
        return (LabelToken)_labelCount;
    }

    private TypedLabel GenerateLabel(string tag)
    {
        var token = GenerateLabelToken();
        return GenerateLabel(tag, token);
    }

    private TypedLabel GenerateLabel(string tag, LabelToken token) =>
        new TypedLabel($"{tag}Label{(int)token}");

    private Symbol GenerateVariable(Type type)
    {
        _variableCount++;
        var name = $"variable${_variableCount}";

        return _method.NewLocal(TextLocation.None, name, false).WithType(type).Declare();
    }

    protected override TypedStatement RewriteTypedConditionalGotoStatement(
        TypedConditionalGotoStatement node
    )
    {
        var constant = ConstantFolding.Fold(node.Condition);
        if (constant == null)
            return base.RewriteTypedConditionalGotoStatement(node);

        var condition = (bool)constant.Value;
        var condition2 = node.JumpIfTrue ? condition : !condition;
        if (condition2)
            return new TypedGotoStatement(node.Syntax, node.TypedLabel);

        return new TypedNopStatement(node.Syntax);
    }

    protected override TypedExpression RewriteBlockExpression(TypedBlockExpression node)
    {
        if (node.Statements.Length == 0)
            return RewriteExpression(node.Expression);

        return base.RewriteBlockExpression(node);
    }

    protected override TypedExpression RewriteWhileExpression(TypedWhileExpression node)
    {
        /*
         * while <condition>
         *     <body>
         *
         * <whileLabel>
         * gotoIfFalse <condition> <endLabel>
         *     <body>
         *     goto <whileLabel>
         * <endLabel>
         */

        var body = RewriteExpression(node.Body);
        var condition = RewriteExpression(node.Condition);
        return RewriteExpression(
            new TypedBlockExpression(
                node.Syntax,
                ImmutableArray.Create<TypedStatement>(
                    new TypedLabelStatement(node.Syntax, node.ContinueLabel),
                    new TypedConditionalGotoStatement(node.Syntax, node.BreakLabel, condition),
                    new TypedExpressionStatement(node.Syntax, body),
                    new TypedGotoStatement(node.Syntax, node.ContinueLabel),
                    new TypedLabelStatement(node.Syntax, node.BreakLabel)
                ),
                new TypedUnitExpression(node.Syntax)
            )
        );
    }

    protected override TypedExpression RewriteIfExpression(TypedIfExpression node)
    {
        /*
         * if (<condition>)
         *     <thenBody>
         * else
         *     <elseBody>
         *
         *
         * to
         *
         * gotoIfFalse <condition> <elseLabel>
         *    <thenBody>
         *    goto <endLabel>
         * <elseLabel>
         *     <elseBody>
         * <endLabel>
         *
         */

        var token = GenerateLabelToken();
        var elseLabel = GenerateLabel("IfElse", token);
        var endLabel = GenerateLabel("IfEnd", token);
        var variable = GenerateVariable(node.Type);

        var condition = RewriteExpression(node.Condition);
        if (condition is TypedLiteralExpression literal)
            return RewriteExpression((bool)literal.Value ? node.Then : node.Else);

        var then = RewriteExpression(node.Then);
        var @else = RewriteExpression(node.Else);
        var variableExpression = new TypedVariableExpression(node.Syntax, variable);
        var block = new TypedBlockExpression(
            node.Syntax,
            ImmutableArray.Create<TypedStatement>(
                new TypedVariableDeclarationStatement(node.Syntax, variable, null),
                new TypedConditionalGotoStatement(node.Syntax, elseLabel, condition),
                new TypedAssignmentStatement(node.Syntax, variableExpression, then),
                new TypedGotoStatement(node.Syntax, endLabel),
                new TypedLabelStatement(node.Syntax, elseLabel),
                new TypedAssignmentStatement(node.Syntax, variableExpression, @else),
                new TypedLabelStatement(node.Syntax, endLabel)
            ),
            variableExpression
        );

        return RewriteExpression(block);
    }

    private TypedExpression ValueExpression(SyntaxNode syntax, Symbol symbol)
    {
        if (symbol.IsField)
            return new TypedFieldExpression(syntax, null, symbol);

        return new TypedVariableExpression(syntax, symbol);
    }

    protected override TypedExpression RewriteForExpression(TypedForExpression node)
    {
        /*
         * convert from for to while
         *
         * for (x <- l to u) expr
         *
         * var x = l
         * while(x < u) {
         *     expr
         *     continue:
         *     x = x + 1
         * }
         */

        var lowerTyped = RewriteExpression(node.LowerTyped);
        var upperTyped = RewriteExpression(node.UpperTyped);
        var body = RewriteExpression(node.Body);

        var declareX = new TypedVariableDeclarationStatement(
            node.Syntax,
            node.Variable,
            lowerTyped
        );

        var variableExpression = ValueExpression(node.Syntax, node.Variable);
        var condition = new TypedBinaryExpression(
            node.Syntax,
            variableExpression,
            TypedBinaryOperator.BindOrThrow(SyntaxKind.LessThanToken, Type.Int, Type.Int),
            upperTyped
        );
        var continueLabelStatement = new TypedLabelStatement(node.Syntax, node.ContinueLabel);
        var incrementX = new TypedExpressionStatement(
            node.Syntax,
            new TypedAssignmentExpression(
                node.Syntax,
                variableExpression,
                new TypedBinaryExpression(
                    node.Syntax,
                    variableExpression,
                    TypedBinaryOperator.BindOrThrow(SyntaxKind.PlusToken, Type.Int, Type.Int),
                    new TypedLiteralExpression(node.Syntax, 1)
                )
            )
        );
        var whileBody = new TypedBlockExpression(
            node.Syntax,
            ImmutableArray.Create<TypedStatement>(
                new TypedExpressionStatement(body.Syntax, body),
                continueLabelStatement,
                incrementX
            ),
            new TypedUnitExpression(node.Syntax)
        );

        var newBlock = new TypedBlockExpression(
            node.Syntax,
            ImmutableArray.Create<TypedStatement>(declareX),
            new TypedWhileExpression(
                node.Syntax,
                condition,
                whileBody,
                node.BreakLabel,
                new TypedLabel("continue")
            )
        );
        return RewriteExpression(newBlock);
    }
}
