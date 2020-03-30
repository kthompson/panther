using System;
using System.Collections.Generic;
using System.Linq;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Syntax
{
    public class SyntaxToken : SyntaxNode
    {
        public SyntaxToken(SyntaxKind kind, int position, string text, object? value, bool isInsertedToken = false)
        {
            Kind = kind;
            Position = position;
            Text = text;
            Value = value;
            IsInsertedToken = isInsertedToken;
        }

        public override SyntaxKind Kind { get; }
        public int Position { get; }
        public string Text { get; }
        public object? Value { get; }
        public bool IsInsertedToken { get; }
        public override TextSpan Span => new TextSpan(Position, Text?.Length ?? 0);
    }
}