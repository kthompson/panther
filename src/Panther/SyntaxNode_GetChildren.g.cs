using System;
using System.Collections.Generic;

namespace Panther.CodeAnalysis.Syntax
{
    partial class AssignmentExpressionSyntax
    {
        public override SyntaxKind Kind => SyntaxKind.AssignmentExpression;
        
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Name;
            yield return EqualsToken;
            yield return Expression;
        }
    }
    
    partial class BinaryExpressionSyntax
    {
        public override SyntaxKind Kind => SyntaxKind.BinaryExpression;
        
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Left;
            yield return OperatorToken;
            yield return Right;
        }
    }
    
    partial class BlockExpressionSyntax
    {
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
    
    partial class BreakExpressionSyntax
    {
        public override SyntaxKind Kind => SyntaxKind.BreakExpression;
        
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return BreakKeyword;
        }
    }
    
    partial class CallExpressionSyntax
    {
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
    
    partial class CompilationUnitSyntax
    {
        public override SyntaxKind Kind => SyntaxKind.CompilationUnit;
        
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            foreach (var child in Members)
                yield return child;
            yield return EndOfFileToken;
        }
    }
    
    partial class ContinueExpressionSyntax
    {
        public override SyntaxKind Kind => SyntaxKind.ContinueExpression;
        
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return ContinueKeyword;
        }
    }
    
    partial class ExpressionStatementSyntax
    {
        public override SyntaxKind Kind => SyntaxKind.ExpressionStatement;
        
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Expression;
        }
    }
    
    partial class ForExpressionSyntax
    {
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
    
    partial class FunctionDeclarationSyntax
    {
        public override SyntaxKind Kind => SyntaxKind.FunctionDeclaration;
        
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
    
    partial class GlobalStatementSyntax
    {
        public override SyntaxKind Kind => SyntaxKind.GlobalStatement;
        
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Statement;
        }
    }
    
    partial class GroupExpressionSyntax
    {
        public override SyntaxKind Kind => SyntaxKind.GroupExpression;
        
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return OpenParenToken;
            yield return Expression;
            yield return CloseParenToken;
        }
    }
    
    partial class IdentifierNameSyntax
    {
        public override SyntaxKind Kind => SyntaxKind.IdentifierName;
        
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Identifier;
        }
    }
    
    partial class IfExpressionSyntax
    {
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
    
    partial class LiteralExpressionSyntax
    {
        public override SyntaxKind Kind => SyntaxKind.LiteralExpression;
        
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return LiteralToken;
            if (Value != null)
            {
            }
        }
    }
    
    partial class MemberAccessExpressionSyntax
    {
        public override SyntaxKind Kind => SyntaxKind.MemberAccessExpression;
        
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Expression;
            yield return DotToken;
            yield return Name;
        }
    }
    
    partial class ParameterSyntax
    {
        public override SyntaxKind Kind => SyntaxKind.Parameter;
        
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Identifier;
            yield return TypeAnnotation;
        }
    }
    
    partial class TypeAnnotationSyntax
    {
        public override SyntaxKind Kind => SyntaxKind.TypeAnnotation;
        
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return ColonToken;
            yield return IdentifierToken;
        }
    }
    
    partial class UnaryExpressionSyntax
    {
        public override SyntaxKind Kind => SyntaxKind.UnaryExpression;
        
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return OperatorToken;
            yield return Operand;
        }
    }
    
    partial class UnitExpressionSyntax
    {
        public override SyntaxKind Kind => SyntaxKind.UnitExpression;
        
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return OpenParenToken;
            yield return CloseParenToken;
        }
    }
    
    partial class VariableDeclarationStatementSyntax
    {
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
    
    partial class WhileExpressionSyntax
    {
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
    
}
