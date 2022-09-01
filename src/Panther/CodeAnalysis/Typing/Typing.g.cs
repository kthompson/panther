using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Immutable;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

#nullable enable

namespace Panther.CodeAnalysis.Typing;
internal abstract partial record TypedExpression(SyntaxNode Syntax)
    : TypedNode(Syntax);

internal abstract partial record TypedStatement(SyntaxNode Syntax)
    : TypedNode(Syntax);

internal abstract partial record TypedMember(SyntaxNode Syntax)
    : TypedNode(Syntax);

internal sealed partial record TypedAssignmentExpression(SyntaxNode Syntax, TypedExpression Left, TypedExpression Right)
    : TypedExpression(Syntax) {
    public override TypedNodeKind Kind => TypedNodeKind.AssignmentExpression;

    public override string ToString()
    {
        using var writer = new StringWriter();
        this.WriteTo(writer);
        return writer.ToString();
    }

    public override void Accept(TypedNodeVisitor visitor) => visitor.VisitAssignmentExpression(this);

    public override TResult Accept<TResult>(TypedNodeVisitor<TResult> visitor) => visitor.VisitAssignmentExpression(this);
}

internal sealed partial record TypedBinaryExpression(SyntaxNode Syntax, TypedExpression Left, TypedBinaryOperator Operator, TypedExpression Right)
    : TypedExpression(Syntax) {
    public override TypedNodeKind Kind => TypedNodeKind.BinaryExpression;

    public override string ToString()
    {
        using var writer = new StringWriter();
        this.WriteTo(writer);
        return writer.ToString();
    }

    public override void Accept(TypedNodeVisitor visitor) => visitor.VisitBinaryExpression(this);

    public override TResult Accept<TResult>(TypedNodeVisitor<TResult> visitor) => visitor.VisitBinaryExpression(this);
}

internal sealed partial record TypedBlockExpression(SyntaxNode Syntax, ImmutableArray<TypedStatement> Statements, TypedExpression Expression)
    : TypedExpression(Syntax) {
    public override TypedNodeKind Kind => TypedNodeKind.BlockExpression;

    public override string ToString()
    {
        using var writer = new StringWriter();
        this.WriteTo(writer);
        return writer.ToString();
    }

    public override void Accept(TypedNodeVisitor visitor) => visitor.VisitBlockExpression(this);

    public override TResult Accept<TResult>(TypedNodeVisitor<TResult> visitor) => visitor.VisitBlockExpression(this);
}

internal sealed partial record TypedArrayCreationExpression(SyntaxNode Syntax, Symbol ElementType, int ArraySize, ImmutableArray<TypedExpression> Expressions)
    : TypedExpression(Syntax) {
    public override TypedNodeKind Kind => TypedNodeKind.ArrayCreationExpression;

    public override string ToString()
    {
        using var writer = new StringWriter();
        this.WriteTo(writer);
        return writer.ToString();
    }

    public override void Accept(TypedNodeVisitor visitor) => visitor.VisitArrayCreationExpression(this);

    public override TResult Accept<TResult>(TypedNodeVisitor<TResult> visitor) => visitor.VisitArrayCreationExpression(this);
}

internal sealed partial record TypedBreakStatement(SyntaxNode Syntax)
    : TypedStatement(Syntax) {
    public override TypedNodeKind Kind => TypedNodeKind.BreakStatement;

    public override string ToString()
    {
        using var writer = new StringWriter();
        this.WriteTo(writer);
        return writer.ToString();
    }

    public override void Accept(TypedNodeVisitor visitor) => visitor.VisitBreakStatement(this);

    public override TResult Accept<TResult>(TypedNodeVisitor<TResult> visitor) => visitor.VisitBreakStatement(this);
}

internal sealed partial record TypedContinueStatement(SyntaxNode Syntax)
    : TypedStatement(Syntax) {
    public override TypedNodeKind Kind => TypedNodeKind.ContinueStatement;

    public override string ToString()
    {
        using var writer = new StringWriter();
        this.WriteTo(writer);
        return writer.ToString();
    }

    public override void Accept(TypedNodeVisitor visitor) => visitor.VisitContinueStatement(this);

    public override TResult Accept<TResult>(TypedNodeVisitor<TResult> visitor) => visitor.VisitContinueStatement(this);
}

internal sealed partial record TypedCallExpression(SyntaxNode Syntax, Symbol Method, TypedExpression? Expression, ImmutableArray<TypedExpression> Arguments)
    : TypedExpression(Syntax) {
    public override TypedNodeKind Kind => TypedNodeKind.CallExpression;

    public override string ToString()
    {
        using var writer = new StringWriter();
        this.WriteTo(writer);
        return writer.ToString();
    }

    public override void Accept(TypedNodeVisitor visitor) => visitor.VisitCallExpression(this);

    public override TResult Accept<TResult>(TypedNodeVisitor<TResult> visitor) => visitor.VisitCallExpression(this);
}

internal sealed partial record TypedFieldExpression(SyntaxNode Syntax, TypedExpression? Expression, Symbol Field)
    : TypedExpression(Syntax) {
    public override TypedNodeKind Kind => TypedNodeKind.FieldExpression;

    public override string ToString()
    {
        using var writer = new StringWriter();
        this.WriteTo(writer);
        return writer.ToString();
    }

    public override void Accept(TypedNodeVisitor visitor) => visitor.VisitFieldExpression(this);

    public override TResult Accept<TResult>(TypedNodeVisitor<TResult> visitor) => visitor.VisitFieldExpression(this);
}

internal sealed partial record TypedPropertyExpression(SyntaxNode Syntax, TypedExpression Expression, Symbol Property)
    : TypedExpression(Syntax) {
    public override TypedNodeKind Kind => TypedNodeKind.PropertyExpression;

    public override string ToString()
    {
        using var writer = new StringWriter();
        this.WriteTo(writer);
        return writer.ToString();
    }

    public override void Accept(TypedNodeVisitor visitor) => visitor.VisitPropertyExpression(this);

    public override TResult Accept<TResult>(TypedNodeVisitor<TResult> visitor) => visitor.VisitPropertyExpression(this);
}

internal sealed partial record TypedIndexExpression(SyntaxNode Syntax, TypedExpression Expression, TypedExpression Index, Symbol? Getter, Symbol? Setter)
    : TypedExpression(Syntax) {
    public override TypedNodeKind Kind => TypedNodeKind.IndexExpression;

    public override string ToString()
    {
        using var writer = new StringWriter();
        this.WriteTo(writer);
        return writer.ToString();
    }

    public override void Accept(TypedNodeVisitor visitor) => visitor.VisitIndexExpression(this);

    public override TResult Accept<TResult>(TypedNodeVisitor<TResult> visitor) => visitor.VisitIndexExpression(this);
}

internal sealed partial record TypedNamespaceExpression(SyntaxNode Syntax, Symbol Namespace)
    : TypedExpression(Syntax) {
    public override TypedNodeKind Kind => TypedNodeKind.NamespaceExpression;

    public override string ToString()
    {
        using var writer = new StringWriter();
        this.WriteTo(writer);
        return writer.ToString();
    }

    public override void Accept(TypedNodeVisitor visitor) => visitor.VisitNamespaceExpression(this);

    public override TResult Accept<TResult>(TypedNodeVisitor<TResult> visitor) => visitor.VisitNamespaceExpression(this);
}

internal sealed partial record TypedForExpression(SyntaxNode Syntax, Symbol Variable, TypedExpression LowerTyped, TypedExpression UpperTyped, TypedExpression Body, TypedLabel BreakLabel, TypedLabel ContinueLabel)
    : TypedExpression(Syntax) {
    public override TypedNodeKind Kind => TypedNodeKind.ForExpression;

    public override string ToString()
    {
        using var writer = new StringWriter();
        this.WriteTo(writer);
        return writer.ToString();
    }

    public override void Accept(TypedNodeVisitor visitor) => visitor.VisitForExpression(this);

    public override TResult Accept<TResult>(TypedNodeVisitor<TResult> visitor) => visitor.VisitForExpression(this);
}

internal sealed partial record TypedGroupExpression(SyntaxNode Syntax, TypedExpression Expression)
    : TypedExpression(Syntax) {
    public override TypedNodeKind Kind => TypedNodeKind.GroupExpression;

    public override string ToString()
    {
        using var writer = new StringWriter();
        this.WriteTo(writer);
        return writer.ToString();
    }

    public override void Accept(TypedNodeVisitor visitor) => visitor.VisitGroupExpression(this);

    public override TResult Accept<TResult>(TypedNodeVisitor<TResult> visitor) => visitor.VisitGroupExpression(this);
}

internal sealed partial record TypedIfExpression(SyntaxNode Syntax, TypedExpression Condition, TypedExpression Then, TypedExpression Else)
    : TypedExpression(Syntax) {
    public override TypedNodeKind Kind => TypedNodeKind.IfExpression;

    public override string ToString()
    {
        using var writer = new StringWriter();
        this.WriteTo(writer);
        return writer.ToString();
    }

    public override void Accept(TypedNodeVisitor visitor) => visitor.VisitIfExpression(this);

    public override TResult Accept<TResult>(TypedNodeVisitor<TResult> visitor) => visitor.VisitIfExpression(this);
}

internal sealed partial record TypedNewExpression(SyntaxNode Syntax, Symbol Constructor, ImmutableArray<TypedExpression> Arguments)
    : TypedExpression(Syntax) {
    public override TypedNodeKind Kind => TypedNodeKind.NewExpression;

    public override string ToString()
    {
        using var writer = new StringWriter();
        this.WriteTo(writer);
        return writer.ToString();
    }

    public override void Accept(TypedNodeVisitor visitor) => visitor.VisitNewExpression(this);

    public override TResult Accept<TResult>(TypedNodeVisitor<TResult> visitor) => visitor.VisitNewExpression(this);
}

internal sealed partial record TypedWhileExpression(SyntaxNode Syntax, TypedExpression Condition, TypedExpression Body, TypedLabel BreakLabel, TypedLabel ContinueLabel)
    : TypedExpression(Syntax) {
    public override TypedNodeKind Kind => TypedNodeKind.WhileExpression;

    public override string ToString()
    {
        using var writer = new StringWriter();
        this.WriteTo(writer);
        return writer.ToString();
    }

    public override void Accept(TypedNodeVisitor visitor) => visitor.VisitWhileExpression(this);

    public override TResult Accept<TResult>(TypedNodeVisitor<TResult> visitor) => visitor.VisitWhileExpression(this);
}

internal sealed partial record TypedUnaryExpression(SyntaxNode Syntax, TypedUnaryOperator Operator, TypedExpression Operand)
    : TypedExpression(Syntax) {
    public override TypedNodeKind Kind => TypedNodeKind.UnaryExpression;

    public override string ToString()
    {
        using var writer = new StringWriter();
        this.WriteTo(writer);
        return writer.ToString();
    }

    public override void Accept(TypedNodeVisitor visitor) => visitor.VisitUnaryExpression(this);

    public override TResult Accept<TResult>(TypedNodeVisitor<TResult> visitor) => visitor.VisitUnaryExpression(this);
}

internal sealed partial record TypedUnitExpression(SyntaxNode Syntax)
    : TypedExpression(Syntax) {
    public override TypedNodeKind Kind => TypedNodeKind.UnitExpression;

    public override string ToString()
    {
        using var writer = new StringWriter();
        this.WriteTo(writer);
        return writer.ToString();
    }

    public override void Accept(TypedNodeVisitor visitor) => visitor.VisitUnitExpression(this);

    public override TResult Accept<TResult>(TypedNodeVisitor<TResult> visitor) => visitor.VisitUnitExpression(this);
}

internal sealed partial record TypedVariableExpression(SyntaxNode Syntax, Symbol Variable)
    : TypedExpression(Syntax) {
    public override TypedNodeKind Kind => TypedNodeKind.VariableExpression;

    public override string ToString()
    {
        using var writer = new StringWriter();
        this.WriteTo(writer);
        return writer.ToString();
    }

    public override void Accept(TypedNodeVisitor visitor) => visitor.VisitVariableExpression(this);

    public override TResult Accept<TResult>(TypedNodeVisitor<TResult> visitor) => visitor.VisitVariableExpression(this);
}

internal sealed partial record TypedAssignmentStatement(SyntaxNode Syntax, TypedExpression Left, TypedExpression Right)
    : TypedStatement(Syntax) {
    public override TypedNodeKind Kind => TypedNodeKind.AssignmentStatement;

    public override string ToString()
    {
        using var writer = new StringWriter();
        this.WriteTo(writer);
        return writer.ToString();
    }

    public override void Accept(TypedNodeVisitor visitor) => visitor.VisitAssignmentStatement(this);

    public override TResult Accept<TResult>(TypedNodeVisitor<TResult> visitor) => visitor.VisitAssignmentStatement(this);
}

internal sealed partial record TypedConditionalGotoStatement(SyntaxNode Syntax, TypedLabel TypedLabel, TypedExpression Condition, bool JumpIfTrue)
    : TypedStatement(Syntax) {
    public override TypedNodeKind Kind => TypedNodeKind.ConditionalGotoStatement;

    public override string ToString()
    {
        using var writer = new StringWriter();
        this.WriteTo(writer);
        return writer.ToString();
    }

    public override void Accept(TypedNodeVisitor visitor) => visitor.VisitConditionalGotoStatement(this);

    public override TResult Accept<TResult>(TypedNodeVisitor<TResult> visitor) => visitor.VisitConditionalGotoStatement(this);
}

internal sealed partial record TypedExpressionStatement(SyntaxNode Syntax, TypedExpression Expression)
    : TypedStatement(Syntax) {
    public override TypedNodeKind Kind => TypedNodeKind.ExpressionStatement;

    public override string ToString()
    {
        using var writer = new StringWriter();
        this.WriteTo(writer);
        return writer.ToString();
    }

    public override void Accept(TypedNodeVisitor visitor) => visitor.VisitExpressionStatement(this);

    public override TResult Accept<TResult>(TypedNodeVisitor<TResult> visitor) => visitor.VisitExpressionStatement(this);
}

internal sealed partial record TypedVariableDeclarationStatement(SyntaxNode Syntax, Symbol Variable, TypedExpression? Expression)
    : TypedStatement(Syntax) {
    public override TypedNodeKind Kind => TypedNodeKind.VariableDeclarationStatement;

    public override string ToString()
    {
        using var writer = new StringWriter();
        this.WriteTo(writer);
        return writer.ToString();
    }

    public override void Accept(TypedNodeVisitor visitor) => visitor.VisitVariableDeclarationStatement(this);

    public override TResult Accept<TResult>(TypedNodeVisitor<TResult> visitor) => visitor.VisitVariableDeclarationStatement(this);
}

internal sealed partial record TypedGlobalStatement(SyntaxNode Syntax, TypedStatement Statement)
    : TypedMember(Syntax) {
    public override TypedNodeKind Kind => TypedNodeKind.GlobalStatement;

    public override string ToString()
    {
        using var writer = new StringWriter();
        this.WriteTo(writer);
        return writer.ToString();
    }

    public override void Accept(TypedNodeVisitor visitor) => visitor.VisitGlobalStatement(this);

    public override TResult Accept<TResult>(TypedNodeVisitor<TResult> visitor) => visitor.VisitGlobalStatement(this);
}

