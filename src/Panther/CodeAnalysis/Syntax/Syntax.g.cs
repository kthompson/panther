using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Immutable;
using Panther.CodeAnalysis.Text;

#nullable enable

namespace Panther.CodeAnalysis.Syntax
{
    public abstract partial record InstructionSyntax(SourceFile SourceFile)
        : SyntaxNode(SourceFile);

    public abstract partial record ExpressionSyntax(SourceFile SourceFile)
        : SyntaxNode(SourceFile);

    public abstract partial record NameSyntax(SourceFile SourceFile)
        : ExpressionSyntax(SourceFile);

    public abstract partial record SimpleNameSyntax(SourceFile SourceFile)
        : NameSyntax(SourceFile);

    public abstract partial record StatementSyntax(SourceFile SourceFile)
        : SyntaxNode(SourceFile);

    public abstract partial record MemberSyntax(SourceFile SourceFile)
        : SyntaxNode(SourceFile);

    public sealed partial record IntOperandInstructionSyntax(SourceFile SourceFile, SyntaxToken OpCode, SyntaxToken Operand)
        : InstructionSyntax(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.IntOperandInstruction;

        public override int GetHashCode()
        {
            return HashCode.Combine(OpCode, Operand);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return OpCode;
            yield return Operand;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitIntOperandInstruction(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitIntOperandInstruction(this);
    }

    public sealed partial record LoadStringInstructionSyntax(SourceFile SourceFile, SyntaxToken OpCode, SyntaxToken Operand)
        : InstructionSyntax(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.LoadStringInstruction;

        public override int GetHashCode()
        {
            return HashCode.Combine(OpCode, Operand);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return OpCode;
            yield return Operand;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitLoadStringInstruction(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitLoadStringInstruction(this);
    }

    public sealed partial record LabelOperandInstructionSyntax(SourceFile SourceFile, SyntaxToken OpCode, SyntaxToken Label)
        : InstructionSyntax(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.LabelOperandInstruction;

        public override int GetHashCode()
        {
            return HashCode.Combine(OpCode, Label);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return OpCode;
            yield return Label;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitLabelOperandInstruction(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitLabelOperandInstruction(this);
    }

    public sealed partial record CallInstructionSyntax(SourceFile SourceFile, SyntaxToken OpCode, SyntaxToken Label, SyntaxToken ArgumentCount)
        : InstructionSyntax(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.CallInstruction;

        public override int GetHashCode()
        {
            return HashCode.Combine(OpCode, Label, ArgumentCount);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return OpCode;
            yield return Label;
            yield return ArgumentCount;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitCallInstruction(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitCallInstruction(this);
    }

    public sealed partial record FunctionInstructionSyntax(SourceFile SourceFile, SyntaxToken OpCode, SyntaxToken Label, SyntaxToken LocalCount)
        : InstructionSyntax(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.FunctionInstruction;

        public override int GetHashCode()
        {
            return HashCode.Combine(OpCode, Label, LocalCount);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return OpCode;
            yield return Label;
            yield return LocalCount;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitFunctionInstruction(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitFunctionInstruction(this);
    }

    public sealed partial record NoOperandInstructionSyntax(SourceFile SourceFile, SyntaxToken OpCode)
        : InstructionSyntax(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.NoOperandInstruction;

        public override int GetHashCode()
        {
            return HashCode.Combine(OpCode);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return OpCode;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitNoOperandInstruction(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitNoOperandInstruction(this);
    }

    public sealed partial record AssemblyListing(SourceFile SourceFile, ImmutableArray<InstructionSyntax> Instructions, SyntaxToken EndOfFileToken)
        : SyntaxNode(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.AssemblyListing;

        public override int GetHashCode()
        {
            return HashCode.Combine(Instructions, EndOfFileToken);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            foreach (var child in Instructions)
                yield return child;

            yield return EndOfFileToken;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitAssemblyListing(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitAssemblyListing(this);
    }

    public sealed partial record AssignmentExpressionSyntax(SourceFile SourceFile, ExpressionSyntax Name, SyntaxToken EqualsToken, ExpressionSyntax Expression)
        : ExpressionSyntax(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.AssignmentExpression;

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, EqualsToken, Expression);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Name;
            yield return EqualsToken;
            yield return Expression;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitAssignmentExpression(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitAssignmentExpression(this);
    }

    public sealed partial record ArrayCreationExpressionSyntax(SourceFile SourceFile, SyntaxToken NewKeyword, NameSyntax Type, SyntaxToken OpenBracket, ExpressionSyntax? ArrayRank, SyntaxToken CloseBracket, ArrayInitializerExpressionSyntax? Initializer)
        : ExpressionSyntax(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.ArrayCreationExpression;

        public override int GetHashCode()
        {
            return HashCode.Combine(NewKeyword, Type, OpenBracket, ArrayRank, CloseBracket, Initializer);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return NewKeyword;
            yield return Type;
            yield return OpenBracket;
            if (ArrayRank != null)
            {
                yield return ArrayRank;
            }
            yield return CloseBracket;
            if (Initializer != null)
            {
                yield return Initializer;
            }
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitArrayCreationExpression(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitArrayCreationExpression(this);
    }

    public sealed partial record ArrayInitializerExpressionSyntax(SourceFile SourceFile, SyntaxToken OpenBraceToken, SeparatedSyntaxList<ExpressionSyntax> Arguments, SyntaxToken CloseBraceToken)
        : SyntaxNode(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.ArrayInitializer;

        public override int GetHashCode()
        {
            return HashCode.Combine(OpenBraceToken, Arguments, CloseBraceToken);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return OpenBraceToken;
            foreach (var child in Arguments.GetWithSeparators())
                yield return child;
            yield return CloseBraceToken;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitArrayInitializerExpression(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitArrayInitializerExpression(this);
    }

    public sealed partial record BinaryExpressionSyntax(SourceFile SourceFile, ExpressionSyntax Left, SyntaxToken OperatorToken, ExpressionSyntax Right)
        : ExpressionSyntax(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.BinaryExpression;

        public override int GetHashCode()
        {
            return HashCode.Combine(Left, OperatorToken, Right);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Left;
            yield return OperatorToken;
            yield return Right;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitBinaryExpression(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitBinaryExpression(this);
    }

    public sealed partial record BlockExpressionSyntax(SourceFile SourceFile, SyntaxToken OpenBraceToken, ImmutableArray<StatementSyntax> Statements, ExpressionSyntax Expression, SyntaxToken CloseBraceToken)
        : ExpressionSyntax(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.BlockExpression;

        public override int GetHashCode()
        {
            return HashCode.Combine(OpenBraceToken, Statements, Expression, CloseBraceToken);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return OpenBraceToken;
            foreach (var child in Statements)
                yield return child;

            yield return Expression;
            yield return CloseBraceToken;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitBlockExpression(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitBlockExpression(this);
    }

    public sealed partial record NewExpressionSyntax(SourceFile SourceFile, SyntaxToken NewKeyword, NameSyntax Type, SyntaxToken OpenParenToken, SeparatedSyntaxList<ExpressionSyntax> Arguments, SyntaxToken CloseParenToken)
        : ExpressionSyntax(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.NewExpression;

        public override int GetHashCode()
        {
            return HashCode.Combine(NewKeyword, Type, OpenParenToken, Arguments, CloseParenToken);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return NewKeyword;
            yield return Type;
            yield return OpenParenToken;
            foreach (var child in Arguments.GetWithSeparators())
                yield return child;
            yield return CloseParenToken;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitNewExpression(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitNewExpression(this);
    }

    public sealed partial record CallExpressionSyntax(SourceFile SourceFile, ExpressionSyntax Expression, SyntaxToken OpenParenToken, SeparatedSyntaxList<ExpressionSyntax> Arguments, SyntaxToken CloseParenToken)
        : ExpressionSyntax(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.CallExpression;

        public override int GetHashCode()
        {
            return HashCode.Combine(Expression, OpenParenToken, Arguments, CloseParenToken);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Expression;
            yield return OpenParenToken;
            foreach (var child in Arguments.GetWithSeparators())
                yield return child;
            yield return CloseParenToken;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitCallExpression(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitCallExpression(this);
    }

    public sealed partial record ForExpressionSyntax(SourceFile SourceFile, SyntaxToken ForKeyword, SyntaxToken OpenParenToken, SyntaxToken Variable, SyntaxToken LessThanDashToken, ExpressionSyntax FromExpression, SyntaxToken ToKeyword, ExpressionSyntax ToExpression, SyntaxToken CloseParenToken, ExpressionSyntax Body)
        : ExpressionSyntax(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.ForExpression;

        public override int GetHashCode()
        {
            var hc = new HashCode();
            hc.Add(ForKeyword);
            hc.Add(OpenParenToken);
            hc.Add(Variable);
            hc.Add(LessThanDashToken);
            hc.Add(FromExpression);
            hc.Add(ToKeyword);
            hc.Add(ToExpression);
            hc.Add(CloseParenToken);
            hc.Add(Body);
            return hc.ToHashCode();
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return ForKeyword;
            yield return OpenParenToken;
            yield return Variable;
            yield return LessThanDashToken;
            yield return FromExpression;
            yield return ToKeyword;
            yield return ToExpression;
            yield return CloseParenToken;
            yield return Body;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitForExpression(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitForExpression(this);
    }

    public sealed partial record GroupExpressionSyntax(SourceFile SourceFile, SyntaxToken OpenParenToken, ExpressionSyntax Expression, SyntaxToken CloseParenToken)
        : ExpressionSyntax(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.GroupExpression;

        public override int GetHashCode()
        {
            return HashCode.Combine(OpenParenToken, Expression, CloseParenToken);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return OpenParenToken;
            yield return Expression;
            yield return CloseParenToken;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitGroupExpression(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitGroupExpression(this);
    }

    public sealed partial record IfExpressionSyntax(SourceFile SourceFile, SyntaxToken IfKeyword, SyntaxToken OpenParenToken, ExpressionSyntax ConditionExpression, SyntaxToken CloseParenToken, ExpressionSyntax ThenExpression, SyntaxToken ElseKeyword, ExpressionSyntax ElseExpression)
        : ExpressionSyntax(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.IfExpression;

        public override int GetHashCode()
        {
            return HashCode.Combine(IfKeyword, OpenParenToken, ConditionExpression, CloseParenToken, ThenExpression, ElseKeyword, ElseExpression);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return IfKeyword;
            yield return OpenParenToken;
            yield return ConditionExpression;
            yield return CloseParenToken;
            yield return ThenExpression;
            yield return ElseKeyword;
            yield return ElseExpression;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitIfExpression(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitIfExpression(this);
    }

    public sealed partial record IndexExpressionSyntax(SourceFile SourceFile, ExpressionSyntax Expression, SyntaxToken OpenBracket, ExpressionSyntax Index, SyntaxToken CloseBracket)
        : ExpressionSyntax(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.IndexExpression;

        public override int GetHashCode()
        {
            return HashCode.Combine(Expression, OpenBracket, Index, CloseBracket);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Expression;
            yield return OpenBracket;
            yield return Index;
            yield return CloseBracket;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitIndexExpression(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitIndexExpression(this);
    }

    public sealed partial record MemberAccessExpressionSyntax(SourceFile SourceFile, ExpressionSyntax Expression, SyntaxToken DotToken, IdentifierNameSyntax Name)
        : ExpressionSyntax(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.MemberAccessExpression;

        public override int GetHashCode()
        {
            return HashCode.Combine(Expression, DotToken, Name);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Expression;
            yield return DotToken;
            yield return Name;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitMemberAccessExpression(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitMemberAccessExpression(this);
    }

    public sealed partial record ThisExpressionSyntax(SourceFile SourceFile, SyntaxToken ThisToken)
        : ExpressionSyntax(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.ThisExpression;

        public override int GetHashCode()
        {
            return HashCode.Combine(ThisToken);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return ThisToken;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitThisExpression(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitThisExpression(this);
    }

    public sealed partial record NullExpressionSyntax(SourceFile SourceFile, SyntaxToken NullToken)
        : ExpressionSyntax(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.NullExpression;

        public override int GetHashCode()
        {
            return HashCode.Combine(NullToken);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return NullToken;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitNullExpression(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitNullExpression(this);
    }

    public sealed partial record WhileExpressionSyntax(SourceFile SourceFile, SyntaxToken WhileKeyword, SyntaxToken OpenParenToken, ExpressionSyntax ConditionExpression, SyntaxToken CloseParenToken, ExpressionSyntax Body)
        : ExpressionSyntax(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.WhileExpression;

        public override int GetHashCode()
        {
            return HashCode.Combine(WhileKeyword, OpenParenToken, ConditionExpression, CloseParenToken, Body);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return WhileKeyword;
            yield return OpenParenToken;
            yield return ConditionExpression;
            yield return CloseParenToken;
            yield return Body;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitWhileExpression(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitWhileExpression(this);
    }

    public sealed partial record UnaryExpressionSyntax(SourceFile SourceFile, SyntaxToken OperatorToken, ExpressionSyntax Operand)
        : ExpressionSyntax(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.UnaryExpression;

        public override int GetHashCode()
        {
            return HashCode.Combine(OperatorToken, Operand);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return OperatorToken;
            yield return Operand;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitUnaryExpression(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitUnaryExpression(this);
    }

    public sealed partial record UnitExpressionSyntax(SourceFile SourceFile, SyntaxToken OpenParenToken, SyntaxToken CloseParenToken)
        : ExpressionSyntax(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.UnitExpression;

        public override int GetHashCode()
        {
            return HashCode.Combine(OpenParenToken, CloseParenToken);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return OpenParenToken;
            yield return CloseParenToken;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitUnitExpression(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitUnitExpression(this);
    }

    public sealed partial record IdentifierNameSyntax(SourceFile SourceFile, SyntaxToken Identifier)
        : SimpleNameSyntax(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.IdentifierName;

        public override int GetHashCode()
        {
            return HashCode.Combine(Identifier);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Identifier;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitIdentifierName(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitIdentifierName(this);
    }

    public sealed partial record GenericNameSyntax(SourceFile SourceFile, SyntaxToken Identifier, TypeArgumentList TypeArgumentList)
        : SimpleNameSyntax(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.GenericName;

        public override int GetHashCode()
        {
            return HashCode.Combine(Identifier, TypeArgumentList);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Identifier;
            yield return TypeArgumentList;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitGenericName(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitGenericName(this);
    }

    public sealed partial record TypeArgumentList(SourceFile SourceFile, SyntaxToken LessThan, SeparatedSyntaxList<NameSyntax> ArgumentList, SyntaxToken GreaterThan)
        : SyntaxNode(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.TypeArgumentList;

        public override int GetHashCode()
        {
            return HashCode.Combine(LessThan, ArgumentList, GreaterThan);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return LessThan;
            foreach (var child in ArgumentList.GetWithSeparators())
                yield return child;
            yield return GreaterThan;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitTypeArgumentList(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitTypeArgumentList(this);
    }

    public sealed partial record QualifiedNameSyntax(SourceFile SourceFile, NameSyntax Left, SyntaxToken DotToken, SimpleNameSyntax Right)
        : NameSyntax(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.QualifiedName;

        public override int GetHashCode()
        {
            return HashCode.Combine(Left, DotToken, Right);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Left;
            yield return DotToken;
            yield return Right;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitQualifiedName(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitQualifiedName(this);
    }

    public sealed partial record BreakStatementSyntax(SourceFile SourceFile, SyntaxToken BreakKeyword)
        : StatementSyntax(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.BreakStatement;

        public override int GetHashCode()
        {
            return HashCode.Combine(BreakKeyword);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return BreakKeyword;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitBreakStatement(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitBreakStatement(this);
    }

    public sealed partial record ContinueStatementSyntax(SourceFile SourceFile, SyntaxToken ContinueKeyword)
        : StatementSyntax(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.ContinueStatement;

        public override int GetHashCode()
        {
            return HashCode.Combine(ContinueKeyword);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return ContinueKeyword;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitContinueStatement(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitContinueStatement(this);
    }

    public sealed partial record ExpressionStatementSyntax(SourceFile SourceFile, ExpressionSyntax Expression)
        : StatementSyntax(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.ExpressionStatement;

        public override int GetHashCode()
        {
            return HashCode.Combine(Expression);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Expression;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitExpressionStatement(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitExpressionStatement(this);
    }

    public sealed partial record VariableDeclarationStatementSyntax(SourceFile SourceFile, SyntaxToken ValOrVarToken, SyntaxToken IdentifierToken, TypeAnnotationSyntax? TypeAnnotation, InitializerSyntax? Initializer)
        : StatementSyntax(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.VariableDeclarationStatement;

        public override int GetHashCode()
        {
            return HashCode.Combine(ValOrVarToken, IdentifierToken, TypeAnnotation, Initializer);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return ValOrVarToken;
            yield return IdentifierToken;
            if (TypeAnnotation != null)
            {
                yield return TypeAnnotation;
            }
            if (Initializer != null)
            {
                yield return Initializer;
            }
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitVariableDeclarationStatement(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitVariableDeclarationStatement(this);
    }

    public sealed partial record GlobalStatementSyntax(SourceFile SourceFile, StatementSyntax Statement)
        : MemberSyntax(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.GlobalStatement;

        public override int GetHashCode()
        {
            return HashCode.Combine(Statement);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Statement;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitGlobalStatement(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitGlobalStatement(this);
    }

    public sealed partial record CompilationUnitSyntax(SourceFile SourceFile, NamespaceDeclarationSyntax? Namespace, ImmutableArray<UsingDirectiveSyntax> Usings, ImmutableArray<MemberSyntax> Members, SyntaxToken EndOfFileToken)
        : SyntaxNode(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.CompilationUnit;

        public override int GetHashCode()
        {
            return HashCode.Combine(Namespace, Usings, Members, EndOfFileToken);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            if (Namespace != null)
            {
                yield return Namespace;
            }
            foreach (var child in Usings)
                yield return child;

            foreach (var child in Members)
                yield return child;

            yield return EndOfFileToken;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitCompilationUnit(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitCompilationUnit(this);
    }

    public sealed partial record NamespaceDeclarationSyntax(SourceFile SourceFile, SyntaxToken NamespaceKeyword, NameSyntax Name)
        : SyntaxNode(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.NamespaceDeclaration;

        public override int GetHashCode()
        {
            return HashCode.Combine(NamespaceKeyword, Name);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return NamespaceKeyword;
            yield return Name;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitNamespaceDeclaration(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitNamespaceDeclaration(this);
    }

    public sealed partial record ParameterSyntax(SourceFile SourceFile, SyntaxToken Identifier, TypeAnnotationSyntax TypeAnnotation)
        : SyntaxNode(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.Parameter;

        public override int GetHashCode()
        {
            return HashCode.Combine(Identifier, TypeAnnotation);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Identifier;
            yield return TypeAnnotation;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitParameter(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitParameter(this);
    }

    public sealed partial record InitializerSyntax(SourceFile SourceFile, SyntaxToken EqualsToken, ExpressionSyntax Expression)
        : SyntaxNode(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.Initializer;

        public override int GetHashCode()
        {
            return HashCode.Combine(EqualsToken, Expression);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return EqualsToken;
            yield return Expression;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitInitializer(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitInitializer(this);
    }

    public sealed partial record TypeAnnotationSyntax(SourceFile SourceFile, SyntaxToken ColonToken, NameSyntax Type)
        : SyntaxNode(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.TypeAnnotation;

        public override int GetHashCode()
        {
            return HashCode.Combine(ColonToken, Type);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return ColonToken;
            yield return Type;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitTypeAnnotation(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitTypeAnnotation(this);
    }

    public sealed partial record UsingDirectiveSyntax(SourceFile SourceFile, SyntaxToken UsingKeyword, SyntaxToken? UsingStyleKeyword, NameSyntax Name)
        : SyntaxNode(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.UsingDirective;

        public override int GetHashCode()
        {
            return HashCode.Combine(UsingKeyword, UsingStyleKeyword, Name);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return UsingKeyword;
            if (UsingStyleKeyword != null)
            {
                yield return UsingStyleKeyword;
            }
            yield return Name;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitUsingDirective(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitUsingDirective(this);
    }

    public sealed partial record TemplateSyntax(SourceFile SourceFile, SyntaxToken OpenBrace, ImmutableArray<MemberSyntax> Members, SyntaxToken CloseBrace)
        : SyntaxNode(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.Template;

        public override int GetHashCode()
        {
            return HashCode.Combine(OpenBrace, Members, CloseBrace);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return OpenBrace;
            foreach (var child in Members)
                yield return child;

            yield return CloseBrace;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitTemplate(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitTemplate(this);
    }

    public sealed partial record FunctionBodySyntax(SourceFile SourceFile, SyntaxToken EqualsToken, ExpressionSyntax Body)
        : MemberSyntax(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.FunctionBody;

        public override int GetHashCode()
        {
            return HashCode.Combine(EqualsToken, Body);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return EqualsToken;
            yield return Body;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitFunctionBody(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitFunctionBody(this);
    }

    public sealed partial record FunctionDeclarationSyntax(SourceFile SourceFile, SyntaxToken DefKeyword, SyntaxToken Identifier, SyntaxToken OpenParenToken, SeparatedSyntaxList<ParameterSyntax> Parameters, SyntaxToken CloseParenToken, TypeAnnotationSyntax? TypeAnnotation, FunctionBodySyntax? Body)
        : MemberSyntax(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.FunctionDeclaration;

        public override int GetHashCode()
        {
            return HashCode.Combine(DefKeyword, Identifier, OpenParenToken, Parameters, CloseParenToken, TypeAnnotation, Body);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return DefKeyword;
            yield return Identifier;
            yield return OpenParenToken;
            foreach (var child in Parameters.GetWithSeparators())
                yield return child;
            yield return CloseParenToken;
            if (TypeAnnotation != null)
            {
                yield return TypeAnnotation;
            }
            if (Body != null)
            {
                yield return Body;
            }
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitFunctionDeclaration(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitFunctionDeclaration(this);
    }

    public sealed partial record ClassDeclarationSyntax(SourceFile SourceFile, SyntaxToken ClassKeyword, SyntaxToken Identifier, SyntaxToken OpenParenToken, SeparatedSyntaxList<ParameterSyntax> Fields, SyntaxToken CloseParenToken, TemplateSyntax? Template)
        : MemberSyntax(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.ClassDeclaration;

        public override int GetHashCode()
        {
            return HashCode.Combine(ClassKeyword, Identifier, OpenParenToken, Fields, CloseParenToken, Template);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return ClassKeyword;
            yield return Identifier;
            yield return OpenParenToken;
            foreach (var child in Fields.GetWithSeparators())
                yield return child;
            yield return CloseParenToken;
            if (Template != null)
            {
                yield return Template;
            }
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitClassDeclaration(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitClassDeclaration(this);
    }

    public sealed partial record ObjectDeclarationSyntax(SourceFile SourceFile, SyntaxToken ObjectKeyword, SyntaxToken Identifier, TemplateSyntax Template)
        : MemberSyntax(SourceFile) {
        public override SyntaxKind Kind => SyntaxKind.ObjectDeclaration;

        public override int GetHashCode()
        {
            return HashCode.Combine(ObjectKeyword, Identifier, Template);
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return ObjectKeyword;
            yield return Identifier;
            yield return Template;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public override void Accept(SyntaxVisitor visitor) => visitor.VisitObjectDeclaration(this);

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitObjectDeclaration(this);
    }

}
