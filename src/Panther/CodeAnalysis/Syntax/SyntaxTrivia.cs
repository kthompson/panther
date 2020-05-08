using System.Collections.Generic;
using System.Collections.Immutable;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Syntax
{
    public sealed class SyntaxTrivia : SyntaxNode
    {
        public SyntaxTrivia(SyntaxTree syntaxTree, SyntaxKind kind, string text, int position)
            : base(syntaxTree)
        {
            Kind = kind;
            Text = text;
            _position = position;
        }

        private readonly int _position;
        public override SyntaxKind Kind { get; }
        public string Text { get; }

        public override TextSpan Span => new TextSpan(_position, Text?.Length ?? 0);
        public override TextSpan FullSpan => Span;
        public override IEnumerable<SyntaxNode> GetChildren() => ImmutableArray<SyntaxNode>.Empty;
    }
}