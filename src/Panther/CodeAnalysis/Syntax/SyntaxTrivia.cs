﻿using System.Collections.Generic;
using System.Collections.Immutable;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Syntax;

public sealed record SyntaxTrivia : SyntaxNode
{
    public SyntaxTrivia(SourceFile sourceFile, SyntaxKind kind, string text, int position)
        : base(sourceFile)
    {
        Kind = kind;
        Text = text;
        _position = position;
    }

    private readonly int _position;
    public override SyntaxKind Kind { get; }
    public string Text { get; }

    public override TextSpan Span => new TextSpan(_position, Text.Length);
    public override TextSpan FullSpan => Span;

    public override IEnumerable<SyntaxNode> GetChildren() => ImmutableArray<SyntaxNode>.Empty;

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitTrivia(this);

    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) =>
        visitor.VisitTrivia(this);
}
