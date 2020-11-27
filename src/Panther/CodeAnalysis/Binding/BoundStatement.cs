using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    internal abstract record BoundStatement(SyntaxNode Syntax)
        : BoundNode(Syntax);
}