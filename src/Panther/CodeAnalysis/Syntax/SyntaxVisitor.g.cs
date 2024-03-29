using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Immutable;

#nullable enable

namespace Panther.CodeAnalysis.Syntax;
public partial class SyntaxVisitor
{
    public virtual void VisitArrayCreationExpression(ArrayCreationExpressionSyntax node) =>
        this.DefaultVisit(node);

    public virtual void VisitArrayInitializerExpression(ArrayInitializerExpressionSyntax node) =>
        this.DefaultVisit(node);

    public virtual void VisitAssemblyListing(AssemblyListing node) =>
        this.DefaultVisit(node);

    public virtual void VisitAssignmentExpression(AssignmentExpressionSyntax node) =>
        this.DefaultVisit(node);

    public virtual void VisitBinaryExpression(BinaryExpressionSyntax node) =>
        this.DefaultVisit(node);

    public virtual void VisitBlockExpression(BlockExpressionSyntax node) =>
        this.DefaultVisit(node);

    public virtual void VisitBreakStatement(BreakStatementSyntax node) =>
        this.DefaultVisit(node);

    public virtual void VisitCallExpression(CallExpressionSyntax node) =>
        this.DefaultVisit(node);

    public virtual void VisitCallInstruction(CallInstructionSyntax node) =>
        this.DefaultVisit(node);

    public virtual void VisitClassDeclaration(ClassDeclarationSyntax node) =>
        this.DefaultVisit(node);

    public virtual void VisitCompilationUnit(CompilationUnitSyntax node) =>
        this.DefaultVisit(node);

    public virtual void VisitContinueStatement(ContinueStatementSyntax node) =>
        this.DefaultVisit(node);

    public virtual void VisitExpressionStatement(ExpressionStatementSyntax node) =>
        this.DefaultVisit(node);

    public virtual void VisitForExpression(ForExpressionSyntax node) =>
        this.DefaultVisit(node);

    public virtual void VisitFunctionBody(FunctionBodySyntax node) =>
        this.DefaultVisit(node);

    public virtual void VisitFunctionDeclaration(FunctionDeclarationSyntax node) =>
        this.DefaultVisit(node);

    public virtual void VisitFunctionInstruction(FunctionInstructionSyntax node) =>
        this.DefaultVisit(node);

    public virtual void VisitGenericName(GenericNameSyntax node) =>
        this.DefaultVisit(node);

    public virtual void VisitGlobalStatement(GlobalStatementSyntax node) =>
        this.DefaultVisit(node);

    public virtual void VisitGroupExpression(GroupExpressionSyntax node) =>
        this.DefaultVisit(node);

    public virtual void VisitIdentifierName(IdentifierNameSyntax node) =>
        this.DefaultVisit(node);

    public virtual void VisitIfExpression(IfExpressionSyntax node) =>
        this.DefaultVisit(node);

    public virtual void VisitIndexExpression(IndexExpressionSyntax node) =>
        this.DefaultVisit(node);

    public virtual void VisitInitializer(InitializerSyntax node) =>
        this.DefaultVisit(node);

    public virtual void VisitIntOperandInstruction(IntOperandInstructionSyntax node) =>
        this.DefaultVisit(node);

    public virtual void VisitLabelOperandInstruction(LabelOperandInstructionSyntax node) =>
        this.DefaultVisit(node);

    public virtual void VisitLiteralExpression(LiteralExpressionSyntax node) =>
        this.DefaultVisit(node);

    public virtual void VisitLoadStringInstruction(LoadStringInstructionSyntax node) =>
        this.DefaultVisit(node);

    public virtual void VisitMemberAccessExpression(MemberAccessExpressionSyntax node) =>
        this.DefaultVisit(node);

    public virtual void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node) =>
        this.DefaultVisit(node);

    public virtual void VisitNewExpression(NewExpressionSyntax node) =>
        this.DefaultVisit(node);

    public virtual void VisitNoOperandInstruction(NoOperandInstructionSyntax node) =>
        this.DefaultVisit(node);

    public virtual void VisitNullExpression(NullExpressionSyntax node) =>
        this.DefaultVisit(node);

    public virtual void VisitObjectDeclaration(ObjectDeclarationSyntax node) =>
        this.DefaultVisit(node);

    public virtual void VisitParameter(ParameterSyntax node) =>
        this.DefaultVisit(node);

    public virtual void VisitQualifiedName(QualifiedNameSyntax node) =>
        this.DefaultVisit(node);

    public virtual void VisitToken(SyntaxToken node) =>
        this.DefaultVisit(node);

    public virtual void VisitTrivia(SyntaxTrivia node) =>
        this.DefaultVisit(node);

    public virtual void VisitTemplate(TemplateSyntax node) =>
        this.DefaultVisit(node);

    public virtual void VisitThisExpression(ThisExpressionSyntax node) =>
        this.DefaultVisit(node);

    public virtual void VisitTypeAnnotation(TypeAnnotationSyntax node) =>
        this.DefaultVisit(node);

    public virtual void VisitTypeArgumentList(TypeArgumentList node) =>
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
    public virtual TResult VisitArrayCreationExpression(ArrayCreationExpressionSyntax node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitArrayInitializerExpression(ArrayInitializerExpressionSyntax node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitAssemblyListing(AssemblyListing node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitAssignmentExpression(AssignmentExpressionSyntax node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitBinaryExpression(BinaryExpressionSyntax node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitBlockExpression(BlockExpressionSyntax node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitBreakStatement(BreakStatementSyntax node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitCallExpression(CallExpressionSyntax node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitCallInstruction(CallInstructionSyntax node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitClassDeclaration(ClassDeclarationSyntax node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitCompilationUnit(CompilationUnitSyntax node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitContinueStatement(ContinueStatementSyntax node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitExpressionStatement(ExpressionStatementSyntax node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitForExpression(ForExpressionSyntax node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitFunctionBody(FunctionBodySyntax node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitFunctionDeclaration(FunctionDeclarationSyntax node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitFunctionInstruction(FunctionInstructionSyntax node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitGenericName(GenericNameSyntax node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitGlobalStatement(GlobalStatementSyntax node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitGroupExpression(GroupExpressionSyntax node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitIdentifierName(IdentifierNameSyntax node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitIfExpression(IfExpressionSyntax node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitIndexExpression(IndexExpressionSyntax node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitInitializer(InitializerSyntax node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitIntOperandInstruction(IntOperandInstructionSyntax node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitLabelOperandInstruction(LabelOperandInstructionSyntax node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitLiteralExpression(LiteralExpressionSyntax node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitLoadStringInstruction(LoadStringInstructionSyntax node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitMemberAccessExpression(MemberAccessExpressionSyntax node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitNamespaceDeclaration(NamespaceDeclarationSyntax node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitNewExpression(NewExpressionSyntax node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitNoOperandInstruction(NoOperandInstructionSyntax node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitNullExpression(NullExpressionSyntax node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitObjectDeclaration(ObjectDeclarationSyntax node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitParameter(ParameterSyntax node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitQualifiedName(QualifiedNameSyntax node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitToken(SyntaxToken node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitTrivia(SyntaxTrivia node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitTemplate(TemplateSyntax node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitThisExpression(ThisExpressionSyntax node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitTypeAnnotation(TypeAnnotationSyntax node) =>
        this.DefaultVisit(node);

    public virtual TResult VisitTypeArgumentList(TypeArgumentList node) =>
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
