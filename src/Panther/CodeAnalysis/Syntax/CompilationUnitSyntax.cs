using System.Collections.Immutable;

namespace Panther.CodeAnalysis.Syntax
{
    public sealed partial class CompilationUnitSyntax : SyntaxNode
    {
        public ImmutableArray<NamespaceDirectiveSyntax> NamespaceDirectives { get; }
        public ImmutableArray<UsingDirectiveSyntax> Usings { get; }
        public ImmutableArray<GlobalStatementSyntax> Statements { get; }
        public ImmutableArray<MemberSyntax> Members { get; }
        public SyntaxToken EndOfFileToken { get; }

        public CompilationUnitSyntax(SyntaxTree syntaxTree,
            ImmutableArray<NamespaceDirectiveSyntax> namespaceDirectives,
            ImmutableArray<UsingDirectiveSyntax> usings,
            ImmutableArray<GlobalStatementSyntax> statements,
            ImmutableArray<MemberSyntax> members,
            SyntaxToken endOfFileToken) : base(syntaxTree)
        {
            Usings = usings;
            Statements = statements;
            NamespaceDirectives = namespaceDirectives;
            Members = members;
            EndOfFileToken = endOfFileToken;
        }
    }
}