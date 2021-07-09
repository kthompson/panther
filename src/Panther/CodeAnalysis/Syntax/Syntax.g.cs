using System;
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
        
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Name;
            yield return EqualsToken;
            yield return Expression;
        }
    }

    public sealed partial record BinaryExpressionSyntax(SyntaxTree SyntaxTree, ExpressionSyntax Left, SyntaxToken OperatorToken, ExpressionSyntax Right)
        : ExpressionSyntax(SyntaxTree) {
        public override SyntaxKind Kind => SyntaxKind.BinaryExpression;
        
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Left;
            yield return OperatorToken;
            yield return Right;
        }
    }

    public sealed partial record BlockExpressionSyntax(SyntaxTree SyntaxTree, SyntaxToken OpenBraceToken, ImmutableArray<StatementSyntax> Statements, ExpressionSyntax Expression, SyntaxToken CloseBraceToken)
        : ExpressionSyntax(SyntaxTree) {
        public override SyntaxKind Kind => SyntaxKind.BlockExpression;
        
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return OpenBraceToken;
            foreach (var child in Statements)
                yield return child;

            yield return Expression;
            yield return CloseBraceToken;
        }
    }

    public sealed partial record BreakExpressionSyntax(SyntaxTree SyntaxTree, SyntaxToken BreakKeyword)
        : ExpressionSyntax(SyntaxTree) {
        public override SyntaxKind Kind => SyntaxKind.BreakExpression;
        
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return BreakKeyword;
        }
    }

    public sealed partial record ContinueExpressionSyntax(SyntaxTree SyntaxTree, SyntaxToken ContinueKeyword)
        : ExpressionSyntax(SyntaxTree) {
        public override SyntaxKind Kind => SyntaxKind.ContinueExpression;
        
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return ContinueKeyword;
        }
    }

    public sealed partial record CallExpressionSyntax(SyntaxTree SyntaxTree, ExpressionSyntax Expression, SyntaxToken OpenParenToken, SeparatedSyntaxList<ExpressionSyntax> Arguments, SyntaxToken CloseParenToken)
        : ExpressionSyntax(SyntaxTree) {
        public override SyntaxKind Kind => SyntaxKind.CallExpression;
        
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Expression;
            yield return OpenParenToken;
            foreach (var child in Arguments.GetWithSeparators())
                yield return child;
            yield return CloseParenToken;
        }
    }

    public sealed partial record ForExpressionSyntax(SyntaxTree SyntaxTree, SyntaxToken ForKeyword, SyntaxToken OpenParenToken, SyntaxToken Variable, SyntaxToken LessThanDashToken, ExpressionSyntax FromExpression, SyntaxToken ToKeyword, ExpressionSyntax ToExpression, SyntaxToken CloseParenToken, ExpressionSyntax Body)
        : ExpressionSyntax(SyntaxTree) {
        public override SyntaxKind Kind => SyntaxKind.ForExpression;
        
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
    }

    public sealed partial record GroupExpressionSyntax(SyntaxTree SyntaxTree, SyntaxToken OpenParenToken, ExpressionSyntax Expression, SyntaxToken CloseParenToken)
        : ExpressionSyntax(SyntaxTree) {
        public override SyntaxKind Kind => SyntaxKind.GroupExpression;
        
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return OpenParenToken;
            yield return Expression;
            yield return CloseParenToken;
        }
    }

    public sealed partial record IfExpressionSyntax(SyntaxTree SyntaxTree, SyntaxToken IfKeyword, SyntaxToken OpenParenToken, ExpressionSyntax ConditionExpression, SyntaxToken CloseParenToken, ExpressionSyntax ThenExpression, SyntaxToken ElseKeyword, ExpressionSyntax ElseExpression)
        : ExpressionSyntax(SyntaxTree) {
        public override SyntaxKind Kind => SyntaxKind.IfExpression;
        
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
    }

    public sealed partial record MemberAccessExpressionSyntax(SyntaxTree SyntaxTree, ExpressionSyntax Expression, SyntaxToken DotToken, IdentifierNameSyntax Name)
        : ExpressionSyntax(SyntaxTree) {
        public override SyntaxKind Kind => SyntaxKind.MemberAccessExpression;
        
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Expression;
            yield return DotToken;
            yield return Name;
        }
    }

    public sealed partial record WhileExpressionSyntax(SyntaxTree SyntaxTree, SyntaxToken WhileKeyword, SyntaxToken OpenParenToken, ExpressionSyntax ConditionExpression, SyntaxToken CloseParenToken, ExpressionSyntax Body)
        : ExpressionSyntax(SyntaxTree) {
        public override SyntaxKind Kind => SyntaxKind.WhileExpression;
        
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return WhileKeyword;
            yield return OpenParenToken;
            yield return ConditionExpression;
            yield return CloseParenToken;
            yield return Body;
        }
    }

    public sealed partial record UnaryExpressionSyntax(SyntaxTree SyntaxTree, SyntaxToken OperatorToken, ExpressionSyntax Operand)
        : ExpressionSyntax(SyntaxTree) {
        public override SyntaxKind Kind => SyntaxKind.UnaryExpression;
        
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return OperatorToken;
            yield return Operand;
        }
    }

    public sealed partial record UnitExpressionSyntax(SyntaxTree SyntaxTree, SyntaxToken OpenParenToken, SyntaxToken CloseParenToken)
        : ExpressionSyntax(SyntaxTree) {
        public override SyntaxKind Kind => SyntaxKind.UnitExpression;
        
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return OpenParenToken;
            yield return CloseParenToken;
        }
    }

    public sealed partial record IdentifierNameSyntax(SyntaxTree SyntaxTree, SyntaxToken Identifier)
        : NameSyntax(SyntaxTree) {
        public override SyntaxKind Kind => SyntaxKind.IdentifierName;
        
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Identifier;
        }
    }

    public sealed partial record QualifiedNameSyntax(SyntaxTree SyntaxTree, NameSyntax Left, SyntaxToken DotToken, IdentifierNameSyntax Right)
        : NameSyntax(SyntaxTree) {
        public override SyntaxKind Kind => SyntaxKind.QualifiedName;
        
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Left;
            yield return DotToken;
            yield return Right;
        }
    }

    public sealed partial record ExpressionStatementSyntax(SyntaxTree SyntaxTree, ExpressionSyntax Expression)
        : StatementSyntax(SyntaxTree) {
        public override SyntaxKind Kind => SyntaxKind.ExpressionStatement;
        
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Expression;
        }
    }

    public sealed partial record VariableDeclarationStatementSyntax(SyntaxTree SyntaxTree, SyntaxToken ValOrVarToken, SyntaxToken IdentifierToken, TypeAnnotationSyntax? TypeAnnotation, SyntaxToken EqualsToken, ExpressionSyntax Expression)
        : StatementSyntax(SyntaxTree) {
        public override SyntaxKind Kind => SyntaxKind.VariableDeclarationStatement;
        
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return ValOrVarToken;
            yield return IdentifierToken;
            if (TypeAnnotation != null)
            {
                yield return TypeAnnotation;
            }
            yield return EqualsToken;
            yield return Expression;
        }
    }

    public sealed partial record GlobalStatementSyntax(SyntaxTree SyntaxTree, StatementSyntax Statement)
        : SyntaxNode(SyntaxTree) {
        public override SyntaxKind Kind => SyntaxKind.GlobalStatement;
        
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Statement;
        }
    }

    public sealed partial record CompilationUnitSyntax(SyntaxTree SyntaxTree, ImmutableArray<NamespaceDirectiveSyntax> NamespaceDirectives, ImmutableArray<UsingDirectiveSyntax> Usings, ImmutableArray<GlobalStatementSyntax> Statements, ImmutableArray<MemberSyntax> Members, SyntaxToken EndOfFileToken)
        : SyntaxNode(SyntaxTree) {
        public override SyntaxKind Kind => SyntaxKind.CompilationUnit;
        
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            foreach (var child in NamespaceDirectives)
                yield return child;

            foreach (var child in Usings)
                yield return child;

            foreach (var child in Statements)
                yield return child;

            foreach (var child in Members)
                yield return child;

            yield return EndOfFileToken;
        }
    }

    public sealed partial record NamespaceDirectiveSyntax(SyntaxTree SyntaxTree, SyntaxToken NamespaceKeyword, NameSyntax Name)
        : SyntaxNode(SyntaxTree) {
        public override SyntaxKind Kind => SyntaxKind.NamespaceDirective;
        
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return NamespaceKeyword;
            yield return Name;
        }
    }

    public sealed partial record ParameterSyntax(SyntaxTree SyntaxTree, SyntaxToken Identifier, TypeAnnotationSyntax TypeAnnotation)
        : SyntaxNode(SyntaxTree) {
        public override SyntaxKind Kind => SyntaxKind.Parameter;
        
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Identifier;
            yield return TypeAnnotation;
        }
    }

    public sealed partial record TypeAnnotationSyntax(SyntaxTree SyntaxTree, SyntaxToken ColonToken, SyntaxToken IdentifierToken)
        : SyntaxNode(SyntaxTree) {
        public override SyntaxKind Kind => SyntaxKind.TypeAnnotation;
        
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return ColonToken;
            yield return IdentifierToken;
        }
    }

    public sealed partial record UsingDirectiveSyntax(SyntaxTree SyntaxTree, SyntaxToken UsingKeyword, SyntaxToken? UsingStyleKeyword, NameSyntax Name)
        : SyntaxNode(SyntaxTree) {
        public override SyntaxKind Kind => SyntaxKind.UsingDirective;
        
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return UsingKeyword;
            if (UsingStyleKeyword != null)
            {
                yield return UsingStyleKeyword;
            }
            yield return Name;
        }
    }

    public sealed partial record FunctionDeclarationSyntax(SyntaxTree SyntaxTree, SyntaxToken DefKeyword, SyntaxToken Identifier, SyntaxToken OpenParenToken, SeparatedSyntaxList<ParameterSyntax> Parameters, SyntaxToken CloseParenToken, TypeAnnotationSyntax? TypeAnnotation, SyntaxToken EqualsToken, ExpressionSyntax Body)
        : MemberSyntax(SyntaxTree) {
        public override SyntaxKind Kind => SyntaxKind.ObjectDeclaration;
        
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
            yield return EqualsToken;
            yield return Body;
        }
    }

    public sealed partial record ObjectDeclarationSyntax(SyntaxTree SyntaxTree, SyntaxToken ObjectKeyword, SyntaxToken Identifier, SyntaxToken OpenBrace, ImmutableArray<MemberSyntax> Members, SyntaxToken CloseBrace)
        : MemberSyntax(SyntaxTree) {
        public override SyntaxKind Kind => SyntaxKind.ObjectDeclaration;
        
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return ObjectKeyword;
            yield return Identifier;
            yield return OpenBrace;
            foreach (var child in Members)
                yield return child;

            yield return CloseBrace;
        }
    }

}
