using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Immutable;

#nullable enable

namespace Panther.CodeAnalysis.Binding
{
    internal partial class BoundNodeVisitor
    {
        public virtual void VisitAssignmentExpression(BoundAssignmentExpression node) =>
            this.DefaultVisit(node);

        public virtual void VisitAssignmentStatement(BoundAssignmentStatement node) =>
            this.DefaultVisit(node);

        public virtual void VisitBinaryExpression(BoundBinaryExpression node) =>
            this.DefaultVisit(node);

        public virtual void VisitBlockExpression(BoundBlockExpression node) =>
            this.DefaultVisit(node);

        public virtual void VisitBreakStatement(BoundBreakStatement node) =>
            this.DefaultVisit(node);

        public virtual void VisitCallExpression(BoundCallExpression node) =>
            this.DefaultVisit(node);

        public virtual void VisitConditionalGotoStatement(BoundConditionalGotoStatement node) =>
            this.DefaultVisit(node);

        public virtual void VisitContinueStatement(BoundContinueStatement node) =>
            this.DefaultVisit(node);

        public virtual void VisitConversionExpression(BoundConversionExpression node) =>
            this.DefaultVisit(node);

        public virtual void VisitErrorExpression(BoundErrorExpression node) =>
            this.DefaultVisit(node);

        public virtual void VisitExpressionStatement(BoundExpressionStatement node) =>
            this.DefaultVisit(node);

        public virtual void VisitFieldExpression(BoundFieldExpression node) =>
            this.DefaultVisit(node);

        public virtual void VisitForExpression(BoundForExpression node) =>
            this.DefaultVisit(node);

        public virtual void VisitGlobalStatement(BoundGlobalStatement node) =>
            this.DefaultVisit(node);

        public virtual void VisitGotoStatement(BoundGotoStatement node) =>
            this.DefaultVisit(node);

        public virtual void VisitGroupExpression(BoundGroupExpression node) =>
            this.DefaultVisit(node);

        public virtual void VisitIfExpression(BoundIfExpression node) =>
            this.DefaultVisit(node);

        public virtual void VisitLabelStatement(BoundLabelStatement node) =>
            this.DefaultVisit(node);

        public virtual void VisitLiteralExpression(BoundLiteralExpression node) =>
            this.DefaultVisit(node);

        public virtual void VisitMethodExpression(BoundMethodExpression node) =>
            this.DefaultVisit(node);

        public virtual void VisitNewExpression(BoundNewExpression node) =>
            this.DefaultVisit(node);

        public virtual void VisitNopStatement(BoundNopStatement node) =>
            this.DefaultVisit(node);

        public virtual void VisitUnaryExpression(BoundUnaryExpression node) =>
            this.DefaultVisit(node);

        public virtual void VisitUnitExpression(BoundUnitExpression node) =>
            this.DefaultVisit(node);

        public virtual void VisitVariableDeclarationStatement(BoundVariableDeclarationStatement node) =>
            this.DefaultVisit(node);

        public virtual void VisitVariableExpression(BoundVariableExpression node) =>
            this.DefaultVisit(node);

        public virtual void VisitWhileExpression(BoundWhileExpression node) =>
            this.DefaultVisit(node);
    }
    internal partial class BoundNodeVisitor<TResult>
    {
        public virtual TResult VisitAssignmentExpression(BoundAssignmentExpression node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitAssignmentStatement(BoundAssignmentStatement node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitBinaryExpression(BoundBinaryExpression node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitBlockExpression(BoundBlockExpression node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitBreakStatement(BoundBreakStatement node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitCallExpression(BoundCallExpression node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitConditionalGotoStatement(BoundConditionalGotoStatement node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitContinueStatement(BoundContinueStatement node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitConversionExpression(BoundConversionExpression node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitErrorExpression(BoundErrorExpression node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitExpressionStatement(BoundExpressionStatement node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitFieldExpression(BoundFieldExpression node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitForExpression(BoundForExpression node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitGlobalStatement(BoundGlobalStatement node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitGotoStatement(BoundGotoStatement node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitGroupExpression(BoundGroupExpression node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitIfExpression(BoundIfExpression node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitLabelStatement(BoundLabelStatement node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitLiteralExpression(BoundLiteralExpression node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitMethodExpression(BoundMethodExpression node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitNewExpression(BoundNewExpression node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitNopStatement(BoundNopStatement node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitUnaryExpression(BoundUnaryExpression node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitUnitExpression(BoundUnitExpression node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitVariableDeclarationStatement(BoundVariableDeclarationStatement node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitVariableExpression(BoundVariableExpression node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitWhileExpression(BoundWhileExpression node) =>
            this.DefaultVisit(node);
    }
}
