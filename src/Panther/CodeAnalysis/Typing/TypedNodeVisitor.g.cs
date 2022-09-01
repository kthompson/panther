using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Immutable;

#nullable enable

namespace Panther.CodeAnalysis.Typing;
internal partial class TypedNodeVisitor
{
    public virtual void VisitArrayCreationExpression(TypedArrayCreationExpression node) =>
        this.DefaultVisit(node);

    public virtual void VisitAssignmentExpression(TypedAssignmentExpression node) =>
        this.DefaultVisit(node);

    public virtual void VisitAssignmentStatement(TypedAssignmentStatement node) =>
        this.DefaultVisit(node);

    public virtual void VisitBinaryExpression(TypedBinaryExpression node) =>
        this.DefaultVisit(node);

    public virtual void VisitBlockExpression(TypedBlockExpression node) =>
        this.DefaultVisit(node);

    public virtual void VisitBreakStatement(TypedBreakStatement node) =>
        this.DefaultVisit(node);

    public virtual void VisitCallExpression(TypedCallExpression node) =>
        this.DefaultVisit(node);

    public virtual void VisitConditionalGotoStatement(TypedConditionalGotoStatement node) =>
        this.DefaultVisit(node);

    public virtual void VisitContinueStatement(TypedContinueStatement node) =>
        this.DefaultVisit(node);

    public virtual void VisitConversionExpression(TypedConversionExpression node) =>
        this.DefaultVisit(node);

    public virtual void VisitErrorExpression(TypedErrorExpression node) =>
        this.DefaultVisit(node);

    public virtual void VisitExpressionStatement(TypedExpressionStatement node) =>
        this.DefaultVisit(node);

    public virtual void VisitFieldExpression(TypedFieldExpression node) =>
        this.DefaultVisit(node);

    public virtual void VisitForExpression(TypedForExpression node) =>
        this.DefaultVisit(node);

    public virtual void VisitGlobalStatement(TypedGlobalStatement node) =>
        this.DefaultVisit(node);

    public virtual void VisitGotoStatement(TypedGotoStatement node) =>
        this.DefaultVisit(node);

    public virtual void VisitGroupExpression(TypedGroupExpression node) =>
        this.DefaultVisit(node);

    public virtual void VisitIfExpression(TypedIfExpression node) =>
        this.DefaultVisit(node);

    public virtual void VisitIndexExpression(TypedIndexExpression node) =>
        this.DefaultVisit(node);

    public virtual void VisitLabelStatement(TypedLabelStatement node) =>
        this.DefaultVisit(node);

    public virtual void VisitLiteralExpression(TypedLiteralExpression node) =>
        this.DefaultVisit(node);

    public virtual void VisitMethodExpression(TypedMethodExpression node) =>
        this.DefaultVisit(node);

    public virtual void VisitNamespaceExpression(TypedNamespaceExpression node) =>
        this.DefaultVisit(node);

    public virtual void VisitNewExpression(TypedNewExpression node) =>
        this.DefaultVisit(node);

    public virtual void VisitNopStatement(TypedNopStatement node) =>
        this.DefaultVisit(node);

    public virtual void VisitPropertyExpression(TypedPropertyExpression node) =>
        this.DefaultVisit(node);

    public virtual void VisitUnaryExpression(TypedUnaryExpression node) =>
        this.DefaultVisit(node);

    public virtual void VisitUnitExpression(TypedUnitExpression node) =>
        this.DefaultVisit(node);

    public virtual void VisitVariableDeclarationStatement(TypedVariableDeclarationStatement node) =>
        this.DefaultVisit(node);

    public virtual void VisitVariableExpression(TypedVariableExpression node) =>
        this.DefaultVisit(node);

    public virtual void VisitWhileExpression(TypedWhileExpression node) =>
        this.DefaultVisit(node);
}
internal partial class TypedNodeVisitor<TResult>
{
    public virtual TResult VisitArrayCreationExpression(TypedArrayCreationExpression node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitAssignmentExpression(TypedAssignmentExpression node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitAssignmentStatement(TypedAssignmentStatement node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitBinaryExpression(TypedBinaryExpression node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitBlockExpression(TypedBlockExpression node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitBreakStatement(TypedBreakStatement node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitCallExpression(TypedCallExpression node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitConditionalGotoStatement(TypedConditionalGotoStatement node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitContinueStatement(TypedContinueStatement node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitConversionExpression(TypedConversionExpression node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitErrorExpression(TypedErrorExpression node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitExpressionStatement(TypedExpressionStatement node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitFieldExpression(TypedFieldExpression node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitForExpression(TypedForExpression node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitGlobalStatement(TypedGlobalStatement node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitGotoStatement(TypedGotoStatement node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitGroupExpression(TypedGroupExpression node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitIfExpression(TypedIfExpression node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitIndexExpression(TypedIndexExpression node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitLabelStatement(TypedLabelStatement node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitLiteralExpression(TypedLiteralExpression node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitMethodExpression(TypedMethodExpression node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitNamespaceExpression(TypedNamespaceExpression node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitNewExpression(TypedNewExpression node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitNopStatement(TypedNopStatement node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitPropertyExpression(TypedPropertyExpression node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitUnaryExpression(TypedUnaryExpression node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitUnitExpression(TypedUnitExpression node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitVariableDeclarationStatement(TypedVariableDeclarationStatement node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitVariableExpression(TypedVariableExpression node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitWhileExpression(TypedWhileExpression node) =>
        this.DefaultVisit(node);
}
