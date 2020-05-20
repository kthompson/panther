using System.Collections.Immutable;

namespace Panther.CodeAnalysis.Syntax
{
    public sealed partial class ObjectDeclarationSyntax : MemberSyntax
    {
        public SyntaxToken ObjectKeyword { get; }
        public SyntaxToken Identifier { get; }
        public SyntaxToken OpenBrace { get; }
        public ImmutableArray<MemberSyntax> Members { get; }
        public SyntaxToken CloseBrace { get; }

        public ObjectDeclarationSyntax(SyntaxTree syntaxTree, SyntaxToken objectKeyword, SyntaxToken identifier, SyntaxToken openBrace, ImmutableArray<MemberSyntax> members, SyntaxToken closeBrace) : base(syntaxTree)
        {
            ObjectKeyword = objectKeyword;
            Identifier = identifier;
            OpenBrace = openBrace;
            Members = members;
            CloseBrace = closeBrace;
        }
    }
}