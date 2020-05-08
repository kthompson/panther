using System.Collections.Immutable;

namespace Panther.CodeAnalysis.Syntax
{
    public sealed partial class CompilationUnitSyntax : SyntaxNode
    {
        public ImmutableArray<MemberSyntax> Members { get; }
        public SyntaxToken EndOfFileToken { get; }

        public CompilationUnitSyntax(SyntaxTree syntaxTree, ImmutableArray<MemberSyntax> members,
            SyntaxToken endOfFileToken) : base(syntaxTree)
        {
            Members = members;
            EndOfFileToken = endOfFileToken;
        }
    }
}