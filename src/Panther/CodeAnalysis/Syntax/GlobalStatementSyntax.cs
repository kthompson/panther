namespace Panther.CodeAnalysis.Syntax
{
    public sealed partial class GlobalStatementSyntax : MemberSyntax
    {
        public GlobalStatementSyntax(SyntaxTree syntaxTree, StatementSyntax statement) : base(syntaxTree)
        {
            Statement = statement;
        }

        public StatementSyntax Statement { get; }
    }
}