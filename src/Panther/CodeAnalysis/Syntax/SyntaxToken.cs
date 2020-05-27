using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Syntax
{
    public sealed class SyntaxToken : SyntaxNode
    {
        public SyntaxToken(SyntaxTree syntaxTree, SyntaxKind kind, int position, string text, object? value)
            : this(syntaxTree, kind, position, position, text, value, false, ImmutableArray<SyntaxTrivia>.Empty, ImmutableArray<SyntaxTrivia>.Empty)
        {
        }

        public SyntaxToken(SyntaxTree syntaxTree, SyntaxKind kind, int position, string text, object? value, ImmutableArray<SyntaxTrivia> leadingTrivia, ImmutableArray<SyntaxTrivia> trailingTrivia)
            : this(syntaxTree, kind, position, position + (text?.Length ?? 0), text, value, false, leadingTrivia, trailingTrivia)
        {
        }

        public SyntaxToken(SyntaxTree syntaxTree, SyntaxKind kind, int position)
            : this(syntaxTree, kind, position, position, string.Empty, null, true, ImmutableArray<SyntaxTrivia>.Empty, ImmutableArray<SyntaxTrivia>.Empty)
        {
        }

        private SyntaxToken(SyntaxTree syntaxTree, SyntaxKind kind, int position, int end, string text, object? value,
            bool isInsertedToken, ImmutableArray<SyntaxTrivia> leadingTrivia, ImmutableArray<SyntaxTrivia> trailingTrivia)
            : base(syntaxTree)
        {
            Kind = kind;
            Position = position;
            Text = text;
            Value = value;
            IsInsertedToken = isInsertedToken;
            LeadingTrivia = leadingTrivia;
            TrailingTrivia = trailingTrivia;
            _end = end;
        }

        private readonly int _end;

        public override SyntaxKind Kind { get; }
        public int Position { get; }
        public string Text { get; }
        public object? Value { get; }
        public bool IsInsertedToken { get; }

        public ImmutableArray<SyntaxTrivia> LeadingTrivia { get; }
        public ImmutableArray<SyntaxTrivia> TrailingTrivia { get; }
        public override TextSpan Span => TextSpan.FromBounds(Position, _end);

        public override TextSpan FullSpan
        {
            get
            {
                var start = LeadingTrivia.Length == 0 ? Span.Start : LeadingTrivia.First().Span.Start;
                var end = TrailingTrivia.Length == 0 ? Span.End : TrailingTrivia.First().Span.End;

                return TextSpan.FromBounds(start, end);
            }
        }

        public override IEnumerable<SyntaxNode> DescendantsAndSelf()
        {
            foreach (var trivia in LeadingTrivia)
                yield return trivia;

            yield return this;

            foreach (var trivia in TrailingTrivia)
                yield return trivia;
        }
    }
}