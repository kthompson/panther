namespace Panther.CodeAnalysis.Syntax
{
    public sealed partial class GlobalStatementSyntax : SyntaxNode
    {
        public GlobalStatementSyntax(SyntaxTree syntaxTree, StatementSyntax statement) : base(syntaxTree)
        {
            Statement = statement;
        }

        public StatementSyntax Statement { get; }
    }
}