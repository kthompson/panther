using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Immutable;

#nullable enable

namespace Panther.CodeAnalysis.Syntax
{
    public abstract partial record ExpressionSyntax(SyntaxTree SyntaxTree)
        : SyntaxNode(SyntaxTree);

    public abstract partial record NameSyntax(SyntaxTree SyntaxTree)
        : ExpressionSyntax(SyntaxTree);

    public abstract partial record StatementSyntax(SyntaxTree SyntaxTree)
        : SyntaxNode(SyntaxTree);

    public abstract partial record MemberSyntax(SyntaxTree SyntaxTree)
        : SyntaxNode(SyntaxTree);

    public sealed partial record AssignmentExpressionSyntax(SyntaxTree SyntaxTree, ExpressionSyntax Name, SyntaxToken EqualsToken, ExpressionSyntax Expression)
        : ExpressionSyntax(SyntaxTree) {
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

    public sealed partial record BinaryExpressionSyntax(SyntaxTree SyntaxTree, ExpressionSyntax Left, SyntaxToken OperatorToken, ExpressionSyntax Right)
        : ExpressionSyntax(SyntaxTree) {
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

    public sealed partial record BlockExpressionSyntax(SyntaxTree SyntaxTree, SyntaxToken OpenBraceToken, ImmutableArray<StatementSyntax> Statements, ExpressionSyntax Expression, SyntaxToken CloseBraceToken)
        : ExpressionSyntax(SyntaxTree) {
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

    public sealed partial record NewExpressionSyntax(SyntaxTree SyntaxTree, SyntaxToken NewKeyword, NameSyntax Type, SyntaxToken OpenParenToken, SeparatedSyntaxList<ExpressionSyntax> Arguments, SyntaxToken CloseParenToken)
        : ExpressionSyntax(SyntaxTree) {
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

    public sealed partial record CallExpressionSyntax(SyntaxTree SyntaxTree, ExpressionSyntax Expression, SyntaxToken OpenParenToken, SeparatedSyntaxList<ExpressionSyntax> Arguments, SyntaxToken CloseParenToken)
        : ExpressionSyntax(SyntaxTree) {
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

    public sealed partial record ForExpressionSyntax(SyntaxTree SyntaxTree, SyntaxToken ForKeyword, SyntaxToken OpenParenToken, SyntaxToken Variable, SyntaxToken LessThanDashToken, ExpressionSyntax FromExpression, SyntaxToken ToKeyword, ExpressionSyntax ToExpression, SyntaxToken CloseParenToken, ExpressionSyntax Body)
        : ExpressionSyntax(SyntaxTree) {
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

    public sealed partial record GroupExpressionSyntax(SyntaxTree SyntaxTree, SyntaxToken OpenParenToken, ExpressionSyntax Expression, SyntaxToken CloseParenToken)
        : ExpressionSyntax(SyntaxTree) {
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

    public sealed partial record IfExpressionSyntax(SyntaxTree SyntaxTree, SyntaxToken IfKeyword, SyntaxToken OpenParenToken, ExpressionSyntax ConditionExpression, SyntaxToken CloseParenToken, ExpressionSyntax ThenExpression, SyntaxToken ElseKeyword, ExpressionSyntax ElseExpression)
        : ExpressionSyntax(SyntaxTree) {
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

    public sealed partial record IndexExpressionSyntax(SyntaxTree SyntaxTree, ExpressionSyntax Expression, SyntaxToken OpenBracket, ExpressionSyntax Index, SyntaxToken CloseBracket)
        : ExpressionSyntax(SyntaxTree) {
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

    public sealed partial record MemberAccessExpressionSyntax(SyntaxTree SyntaxTree, ExpressionSyntax Expression, SyntaxToken DotToken, IdentifierNameSyntax Name)
        : ExpressionSyntax(SyntaxTree) {
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

    public sealed partial record WhileExpressionSyntax(SyntaxTree SyntaxTree, SyntaxToken WhileKeyword, SyntaxToken OpenParenToken, ExpressionSyntax ConditionExpression, SyntaxToken CloseParenToken, ExpressionSyntax Body)
        : ExpressionSyntax(SyntaxTree) {
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

    public sealed partial record UnaryExpressionSyntax(SyntaxTree SyntaxTree, SyntaxToken OperatorToken, ExpressionSyntax Operand)
        : ExpressionSyntax(SyntaxTree) {
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

    public sealed partial record UnitExpressionSyntax(SyntaxTree SyntaxTree, SyntaxToken OpenParenToken, SyntaxToken CloseParenToken)
        : ExpressionSyntax(SyntaxTree) {
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

    public sealed partial record IdentifierNameSyntax(SyntaxTree SyntaxTree, SyntaxToken Identifier)
        : NameSyntax(SyntaxTree) {
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

    public sealed partial record QualifiedNameSyntax(SyntaxTree SyntaxTree, NameSyntax Left, SyntaxToken DotToken, IdentifierNameSyntax Right)
        : NameSyntax(SyntaxTree) {
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

    public sealed partial record BreakStatementSyntax(SyntaxTree SyntaxTree, SyntaxToken BreakKeyword)
        : StatementSyntax(SyntaxTree) {
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

    public sealed partial record ContinueStatementSyntax(SyntaxTree SyntaxTree, SyntaxToken ContinueKeyword)
        : StatementSyntax(SyntaxTree) {
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

    public sealed partial record ExpressionStatementSyntax(SyntaxTree SyntaxTree, ExpressionSyntax Expression)
        : StatementSyntax(SyntaxTree) {
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

    public sealed partial record VariableDeclarationStatementSyntax(SyntaxTree SyntaxTree, SyntaxToken ValOrVarToken, SyntaxToken IdentifierToken, TypeAnnotationSyntax? TypeAnnotation, InitializerSyntax? Initializer)
        : StatementSyntax(SyntaxTree) {
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

    public sealed partial record GlobalStatementSyntax(SyntaxTree SyntaxTree, StatementSyntax Statement)
        : MemberSyntax(SyntaxTree) {
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

    public sealed partial record CompilationUnitSyntax(SyntaxTree SyntaxTree, NamespaceDeclarationSyntax? Namespace, ImmutableArray<UsingDirectiveSyntax> Usings, ImmutableArray<MemberSyntax> Members, SyntaxToken EndOfFileToken)
        : SyntaxNode(SyntaxTree) {
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

    public sealed partial record NamespaceDeclarationSyntax(SyntaxTree SyntaxTree, SyntaxToken NamespaceKeyword, NameSyntax Name)
        : SyntaxNode(SyntaxTree) {
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

    public sealed partial record ParameterSyntax(SyntaxTree SyntaxTree, SyntaxToken Identifier, TypeAnnotationSyntax TypeAnnotation)
        : SyntaxNode(SyntaxTree) {
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

    public sealed partial record InitializerSyntax(SyntaxTree SyntaxTree, SyntaxToken EqualsToken, ExpressionSyntax Expression)
        : SyntaxNode(SyntaxTree) {
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

    public sealed partial record TypeAnnotationSyntax(SyntaxTree SyntaxTree, SyntaxToken ColonToken, NameSyntax Type)
        : SyntaxNode(SyntaxTree) {
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

    public sealed partial record UsingDirectiveSyntax(SyntaxTree SyntaxTree, SyntaxToken UsingKeyword, SyntaxToken? UsingStyleKeyword, NameSyntax Name)
        : SyntaxNode(SyntaxTree) {
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

    public sealed partial record TemplateSyntax(SyntaxTree SyntaxTree, SyntaxToken OpenBrace, ImmutableArray<MemberSyntax> Members, SyntaxToken CloseBrace)
        : SyntaxNode(SyntaxTree) {
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

    public sealed partial record FunctionBodySyntax(SyntaxTree SyntaxTree, SyntaxToken EqualsToken, ExpressionSyntax Body)
        : MemberSyntax(SyntaxTree) {
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

    public sealed partial record FunctionDeclarationSyntax(SyntaxTree SyntaxTree, SyntaxToken DefKeyword, SyntaxToken Identifier, SyntaxToken OpenParenToken, SeparatedSyntaxList<ParameterSyntax> Parameters, SyntaxToken CloseParenToken, TypeAnnotationSyntax? TypeAnnotation, FunctionBodySyntax? Body)
        : MemberSyntax(SyntaxTree) {
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

    public sealed partial record ClassDeclarationSyntax(SyntaxTree SyntaxTree, SyntaxToken ClassKeyword, SyntaxToken Identifier, SyntaxToken OpenParenToken, SeparatedSyntaxList<ParameterSyntax> Fields, SyntaxToken CloseParenToken, TemplateSyntax? Template)
        : MemberSyntax(SyntaxTree) {
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

    public sealed partial record ObjectDeclarationSyntax(SyntaxTree SyntaxTree, SyntaxToken ObjectKeyword, SyntaxToken Identifier, TemplateSyntax Template)
        : MemberSyntax(SyntaxTree) {
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
