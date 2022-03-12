using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Immutable;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

#nullable enable

namespace Panther.CodeAnalysis.Binding
{
    internal abstract partial record BoundExpression(SyntaxNode Syntax)
        : BoundNode(Syntax);

    internal abstract partial record BoundStatement(SyntaxNode Syntax)
        : BoundNode(Syntax);

    internal abstract partial record BoundMember(SyntaxNode Syntax)
        : BoundNode(Syntax);

    internal sealed partial record BoundAssignmentExpression(SyntaxNode Syntax, BoundExpression Left, BoundExpression Right)
        : BoundExpression(Syntax) {
        public override BoundNodeKind Kind => BoundNodeKind.AssignmentExpression;

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(BoundNodeVisitor visitor) => visitor.VisitAssignmentExpression(this);

        public override TResult Accept<TResult>(BoundNodeVisitor<TResult> visitor) => visitor.VisitAssignmentExpression(this);
    }

    internal sealed partial record BoundBinaryExpression(SyntaxNode Syntax, BoundExpression Left, BoundBinaryOperator Operator, BoundExpression Right)
        : BoundExpression(Syntax) {
        public override BoundNodeKind Kind => BoundNodeKind.BinaryExpression;

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(BoundNodeVisitor visitor) => visitor.VisitBinaryExpression(this);

        public override TResult Accept<TResult>(BoundNodeVisitor<TResult> visitor) => visitor.VisitBinaryExpression(this);
    }

    internal sealed partial record BoundBlockExpression(SyntaxNode Syntax, ImmutableArray<BoundStatement> Statements, BoundExpression Expression)
        : BoundExpression(Syntax) {
        public override BoundNodeKind Kind => BoundNodeKind.BlockExpression;

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(BoundNodeVisitor visitor) => visitor.VisitBlockExpression(this);

        public override TResult Accept<TResult>(BoundNodeVisitor<TResult> visitor) => visitor.VisitBlockExpression(this);
    }

    internal sealed partial record BoundBreakStatement(SyntaxNode Syntax)
        : BoundStatement(Syntax) {
        public override BoundNodeKind Kind => BoundNodeKind.BreakStatement;

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(BoundNodeVisitor visitor) => visitor.VisitBreakStatement(this);

        public override TResult Accept<TResult>(BoundNodeVisitor<TResult> visitor) => visitor.VisitBreakStatement(this);
    }

    internal sealed partial record BoundContinueStatement(SyntaxNode Syntax)
        : BoundStatement(Syntax) {
        public override BoundNodeKind Kind => BoundNodeKind.ContinueStatement;

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(BoundNodeVisitor visitor) => visitor.VisitContinueStatement(this);

        public override TResult Accept<TResult>(BoundNodeVisitor<TResult> visitor) => visitor.VisitContinueStatement(this);
    }

    internal sealed partial record BoundCallExpression(SyntaxNode Syntax, Symbol Method, BoundExpression? Expression, ImmutableArray<BoundExpression> Arguments)
        : BoundExpression(Syntax) {
        public override BoundNodeKind Kind => BoundNodeKind.CallExpression;

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(BoundNodeVisitor visitor) => visitor.VisitCallExpression(this);

        public override TResult Accept<TResult>(BoundNodeVisitor<TResult> visitor) => visitor.VisitCallExpression(this);
    }

    internal sealed partial record BoundFieldExpression(SyntaxNode Syntax, BoundExpression? Expression, Symbol Field)
        : BoundExpression(Syntax) {
        public override BoundNodeKind Kind => BoundNodeKind.FieldExpression;

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(BoundNodeVisitor visitor) => visitor.VisitFieldExpression(this);

        public override TResult Accept<TResult>(BoundNodeVisitor<TResult> visitor) => visitor.VisitFieldExpression(this);
    }

    internal sealed partial record BoundForExpression(SyntaxNode Syntax, Symbol Variable, BoundExpression LowerBound, BoundExpression UpperBound, BoundExpression Body, BoundLabel BreakLabel, BoundLabel ContinueLabel)
        : BoundExpression(Syntax) {
        public override BoundNodeKind Kind => BoundNodeKind.ForExpression;

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(BoundNodeVisitor visitor) => visitor.VisitForExpression(this);

        public override TResult Accept<TResult>(BoundNodeVisitor<TResult> visitor) => visitor.VisitForExpression(this);
    }

    internal sealed partial record BoundGroupExpression(SyntaxNode Syntax, BoundExpression Expression)
        : BoundExpression(Syntax) {
        public override BoundNodeKind Kind => BoundNodeKind.GroupExpression;

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(BoundNodeVisitor visitor) => visitor.VisitGroupExpression(this);

        public override TResult Accept<TResult>(BoundNodeVisitor<TResult> visitor) => visitor.VisitGroupExpression(this);
    }

    internal sealed partial record BoundIfExpression(SyntaxNode Syntax, BoundExpression Condition, BoundExpression Then, BoundExpression Else)
        : BoundExpression(Syntax) {
        public override BoundNodeKind Kind => BoundNodeKind.IfExpression;

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(BoundNodeVisitor visitor) => visitor.VisitIfExpression(this);

        public override TResult Accept<TResult>(BoundNodeVisitor<TResult> visitor) => visitor.VisitIfExpression(this);
    }

    internal sealed partial record BoundNewExpression(SyntaxNode Syntax, Symbol Constructor, ImmutableArray<BoundExpression> Arguments)
        : BoundExpression(Syntax) {
        public override BoundNodeKind Kind => BoundNodeKind.NewExpression;

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(BoundNodeVisitor visitor) => visitor.VisitNewExpression(this);

        public override TResult Accept<TResult>(BoundNodeVisitor<TResult> visitor) => visitor.VisitNewExpression(this);
    }

    internal sealed partial record BoundWhileExpression(SyntaxNode Syntax, BoundExpression Condition, BoundExpression Body, BoundLabel BreakLabel, BoundLabel ContinueLabel)
        : BoundExpression(Syntax) {
        public override BoundNodeKind Kind => BoundNodeKind.WhileExpression;

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(BoundNodeVisitor visitor) => visitor.VisitWhileExpression(this);

        public override TResult Accept<TResult>(BoundNodeVisitor<TResult> visitor) => visitor.VisitWhileExpression(this);
    }

    internal sealed partial record BoundUnaryExpression(SyntaxNode Syntax, BoundUnaryOperator Operator, BoundExpression Operand)
        : BoundExpression(Syntax) {
        public override BoundNodeKind Kind => BoundNodeKind.UnaryExpression;

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(BoundNodeVisitor visitor) => visitor.VisitUnaryExpression(this);

        public override TResult Accept<TResult>(BoundNodeVisitor<TResult> visitor) => visitor.VisitUnaryExpression(this);
    }

    internal sealed partial record BoundUnitExpression(SyntaxNode Syntax)
        : BoundExpression(Syntax) {
        public override BoundNodeKind Kind => BoundNodeKind.UnitExpression;

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(BoundNodeVisitor visitor) => visitor.VisitUnitExpression(this);

        public override TResult Accept<TResult>(BoundNodeVisitor<TResult> visitor) => visitor.VisitUnitExpression(this);
    }

    internal sealed partial record BoundVariableExpression(SyntaxNode Syntax, Symbol Variable)
        : BoundExpression(Syntax) {
        public override BoundNodeKind Kind => BoundNodeKind.VariableExpression;

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(BoundNodeVisitor visitor) => visitor.VisitVariableExpression(this);

        public override TResult Accept<TResult>(BoundNodeVisitor<TResult> visitor) => visitor.VisitVariableExpression(this);
    }

    internal sealed partial record BoundAssignmentStatement(SyntaxNode Syntax, BoundExpression Left, BoundExpression Right)
        : BoundStatement(Syntax) {
        public override BoundNodeKind Kind => BoundNodeKind.AssignmentStatement;

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(BoundNodeVisitor visitor) => visitor.VisitAssignmentStatement(this);

        public override TResult Accept<TResult>(BoundNodeVisitor<TResult> visitor) => visitor.VisitAssignmentStatement(this);
    }

    internal sealed partial record BoundConditionalGotoStatement(SyntaxNode Syntax, BoundLabel BoundLabel, BoundExpression Condition, bool JumpIfTrue)
        : BoundStatement(Syntax) {
        public override BoundNodeKind Kind => BoundNodeKind.ConditionalGotoStatement;

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(BoundNodeVisitor visitor) => visitor.VisitConditionalGotoStatement(this);

        public override TResult Accept<TResult>(BoundNodeVisitor<TResult> visitor) => visitor.VisitConditionalGotoStatement(this);
    }

    internal sealed partial record BoundExpressionStatement(SyntaxNode Syntax, BoundExpression Expression)
        : BoundStatement(Syntax) {
        public override BoundNodeKind Kind => BoundNodeKind.ExpressionStatement;

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(BoundNodeVisitor visitor) => visitor.VisitExpressionStatement(this);

        public override TResult Accept<TResult>(BoundNodeVisitor<TResult> visitor) => visitor.VisitExpressionStatement(this);
    }

    internal sealed partial record BoundVariableDeclarationStatement(SyntaxNode Syntax, Symbol Variable, BoundExpression? Expression)
        : BoundStatement(Syntax) {
        public override BoundNodeKind Kind => BoundNodeKind.VariableDeclarationStatement;

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(BoundNodeVisitor visitor) => visitor.VisitVariableDeclarationStatement(this);

        public override TResult Accept<TResult>(BoundNodeVisitor<TResult> visitor) => visitor.VisitVariableDeclarationStatement(this);
    }

    internal sealed partial record BoundGlobalStatement(SyntaxNode Syntax, BoundStatement Statement)
        : BoundMember(Syntax) {
        public override BoundNodeKind Kind => BoundNodeKind.GlobalStatement;

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(BoundNodeVisitor visitor) => visitor.VisitGlobalStatement(this);

        public override TResult Accept<TResult>(BoundNodeVisitor<TResult> visitor) => visitor.VisitGlobalStatement(this);
    }

}
