using System.Collections.Generic;

namespace Panther.CodeAnalysis.Syntax
{
    public abstract partial class NameSyntax : ExpressionSyntax
    {
        protected NameSyntax(SyntaxTree syntaxTree) : base(syntaxTree)
        {
        }

        public abstract string ToText();
    }

    // public sealed partial class QualifiedNameSyntax : NameSyntax
    // {
    //     public NameSyntax Left { get; }
    //     public SyntaxToken DotToken { get; }
    //     public IdentifierNameSyntax Right { get; }
    //
    //     public QualifiedNameSyntax(SyntaxTree syntaxTree, NameSyntax left, SyntaxToken dotToken, IdentifierNameSyntax right)
    //         : base(syntaxTree)
    //     {
    //         Left = left;
    //         DotToken = dotToken;
    //         Right = right;
    //     }
    //
    //     public override string ToText() => $"{Left.ToText()}.{Right.ToText()}";
    // }

    public sealed partial class IdentifierNameSyntax : NameSyntax
    {
        public SyntaxToken Identifier { get; }

        public IdentifierNameSyntax(SyntaxTree syntaxTree, SyntaxToken identifier) : base(syntaxTree)
        {
            Identifier = identifier;
        }

        public override string ToText() => Identifier.Text;
    }

    public sealed partial class MemberAccessExpressionSyntax : ExpressionSyntax
    {
        public ExpressionSyntax Expression { get; }
        public SyntaxToken DotToken { get; }
        public IdentifierNameSyntax Name { get; }
        public MemberAccessExpressionSyntax(SyntaxTree syntaxTree, ExpressionSyntax expression, SyntaxToken dotToken,
            IdentifierNameSyntax name) : base(syntaxTree)
        {
            Expression = expression;
            Name = name;
            DotToken = dotToken;
        }
    }
}