using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Immutable;

#nullable enable

namespace Panther.CodeAnalysis.Syntax
{
    public partial class SyntaxVisitor
    {
        public virtual void VisitAssignmentExpression(AssignmentExpressionSyntax node) =>
            this.DefaultVisit(node);

        public virtual void VisitBinaryExpression(BinaryExpressionSyntax node) =>
            this.DefaultVisit(node);

        public virtual void VisitBlockExpression(BlockExpressionSyntax node) =>
            this.DefaultVisit(node);

        public virtual void VisitBreakExpression(BreakExpressionSyntax node) =>
            this.DefaultVisit(node);

        public virtual void VisitCallExpression(CallExpressionSyntax node) =>
            this.DefaultVisit(node);

        public virtual void VisitClassDeclaration(ClassDeclarationSyntax node) =>
            this.DefaultVisit(node);

        public virtual void VisitCompilationUnit(CompilationUnitSyntax node) =>
            this.DefaultVisit(node);

        public virtual void VisitContinueExpression(ContinueExpressionSyntax node) =>
            this.DefaultVisit(node);

        public virtual void VisitExpressionStatement(ExpressionStatementSyntax node) =>
            this.DefaultVisit(node);

        public virtual void VisitForExpression(ForExpressionSyntax node) =>
            this.DefaultVisit(node);

        public virtual void VisitFunctionDeclaration(FunctionDeclarationSyntax node) =>
            this.DefaultVisit(node);

        public virtual void VisitGlobalStatement(GlobalStatementSyntax node) =>
            this.DefaultVisit(node);

        public virtual void VisitGroupExpression(GroupExpressionSyntax node) =>
            this.DefaultVisit(node);

        public virtual void VisitIdentifierName(IdentifierNameSyntax node) =>
            this.DefaultVisit(node);

        public virtual void VisitIfExpression(IfExpressionSyntax node) =>
            this.DefaultVisit(node);

        public virtual void VisitMemberAccessExpression(MemberAccessExpressionSyntax node) =>
            this.DefaultVisit(node);

        public virtual void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node) =>
            this.DefaultVisit(node);

        public virtual void VisitNamespaceMembers(NamespaceMembersSyntax node) =>
            this.DefaultVisit(node);

        public virtual void VisitNestedNamespace(NestedNamespaceSyntax node) =>
            this.DefaultVisit(node);

        public virtual void VisitNewExpression(NewExpressionSyntax node) =>
            this.DefaultVisit(node);

        public virtual void VisitObjectDeclaration(ObjectDeclarationSyntax node) =>
            this.DefaultVisit(node);

        public virtual void VisitParameter(ParameterSyntax node) =>
            this.DefaultVisit(node);

        public virtual void VisitQualifiedName(QualifiedNameSyntax node) =>
            this.DefaultVisit(node);

        public virtual void VisitTemplate(TemplateSyntax node) =>
            this.DefaultVisit(node);

        public virtual void VisitTypeAnnotation(TypeAnnotationSyntax node) =>
            this.DefaultVisit(node);

        public virtual void VisitUnaryExpression(UnaryExpressionSyntax node) =>
            this.DefaultVisit(node);

        public virtual void VisitUnitExpression(UnitExpressionSyntax node) =>
            this.DefaultVisit(node);

        public virtual void VisitUsingDirective(UsingDirectiveSyntax node) =>
            this.DefaultVisit(node);

        public virtual void VisitVariableDeclarationStatement(VariableDeclarationStatementSyntax node) =>
            this.DefaultVisit(node);

        public virtual void VisitWhileExpression(WhileExpressionSyntax node) =>
            this.DefaultVisit(node);
    }
    public partial class SyntaxVisitor<TResult>
    {
        public virtual TResult VisitAssignmentExpression(AssignmentExpressionSyntax node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitBinaryExpression(BinaryExpressionSyntax node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitBlockExpression(BlockExpressionSyntax node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitBreakExpression(BreakExpressionSyntax node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitCallExpression(CallExpressionSyntax node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitClassDeclaration(ClassDeclarationSyntax node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitCompilationUnit(CompilationUnitSyntax node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitContinueExpression(ContinueExpressionSyntax node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitExpressionStatement(ExpressionStatementSyntax node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitForExpression(ForExpressionSyntax node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitFunctionDeclaration(FunctionDeclarationSyntax node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitGlobalStatement(GlobalStatementSyntax node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitGroupExpression(GroupExpressionSyntax node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitIdentifierName(IdentifierNameSyntax node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitIfExpression(IfExpressionSyntax node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitMemberAccessExpression(MemberAccessExpressionSyntax node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitNamespaceDeclaration(NamespaceDeclarationSyntax node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitNamespaceMembers(NamespaceMembersSyntax node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitNestedNamespace(NestedNamespaceSyntax node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitNewExpression(NewExpressionSyntax node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitObjectDeclaration(ObjectDeclarationSyntax node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitParameter(ParameterSyntax node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitQualifiedName(QualifiedNameSyntax node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitTemplate(TemplateSyntax node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitTypeAnnotation(TypeAnnotationSyntax node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitUnaryExpression(UnaryExpressionSyntax node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitUnitExpression(UnitExpressionSyntax node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitUsingDirective(UsingDirectiveSyntax node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitVariableDeclarationStatement(VariableDeclarationStatementSyntax node) =>
            this.DefaultVisit(node);

        public virtual TResult VisitWhileExpression(WhileExpressionSyntax node) =>
            this.DefaultVisit(node);
    }
}
