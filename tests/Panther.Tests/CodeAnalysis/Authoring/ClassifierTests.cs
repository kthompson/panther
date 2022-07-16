using System;
using Panther.CodeAnalysis.Authoring;
using Panther.CodeAnalysis.Syntax;
using Panther.CodeAnalysis.Text;
using Xunit;

namespace Panther.Tests.CodeAnalysis.Authoring;

public class ClassifierTests
{
    [Fact]
    public void ClassifierSupportsInvalidCharacters()
    {
        var text = "#dump main";
        var tree = SyntaxTree.Parse(text);
        var fullSpan = tree.Root.FullSpan;

        Assert.Collection(
            Classifier.Classify(tree, fullSpan),
            span =>
            {
                Assert.Equal(Classification.Text, span.Classification);
                Assert.Equal(TextSpan.FromBounds(0, 1), span.Span);
                Assert.Equal("#", tree.File.ToString(span.Span));
            },
            span =>
            {
                Assert.Equal(Classification.Identifier, span.Classification);
                Assert.Equal(TextSpan.FromBounds(1, 5), span.Span);
                Assert.Equal("dump", tree.File.ToString(span.Span));
            },
            span =>
            {
                Assert.Equal(Classification.Text, span.Classification);
                Assert.Equal(TextSpan.FromBounds(5, 6), span.Span);
                Assert.Equal(" ", tree.File.ToString(span.Span));
            },
            span =>
            {
                Assert.Equal(Classification.Identifier, span.Classification);
                Assert.Equal(TextSpan.FromBounds(6, 10), span.Span);
                Assert.Equal("main", tree.File.ToString(span.Span));
            }
        );
    }

    [Fact]
    public void ClassifierSupportsWhitespace()
    {
        var text = "       ";
        var tree = SyntaxTree.Parse(text);
        var fullSpan = tree.Root.FullSpan;

        Assert.Collection(
            Classifier.Classify(tree, fullSpan),
            span =>
            {
                Assert.Equal(Classification.Text, span.Classification);
                Assert.Equal(TextSpan.FromBounds(0, 7), span.Span);
                Assert.Equal("       ", tree.File.ToString(span.Span));
            }
        );
    }

    [Fact]
    public void ClassifierSupportsABlock()
    {
        var text = AnnotatedText.Parse(
            @"
                        {
                        }"
        );

        var tree = SyntaxTree.Parse(text.Text);
        var fullSpan = tree.Root.FullSpan;

        var newLineWidth = Environment.NewLine.Length;

        Assert.Collection(
            Classifier.Classify(tree, fullSpan),
            span =>
            {
                Assert.Equal(Classification.Text, span.Classification);
                Assert.Equal(TextSpan.FromBounds(0, 1), span.Span);
                Assert.Equal("{", tree.File.ToString(span.Span));
            },
            span =>
            {
                Assert.Equal(Classification.Text, span.Classification);
                Assert.Equal(TextSpan.FromBounds(1, 1 + newLineWidth), span.Span);
                Assert.Equal(Environment.NewLine, tree.File.ToString(span.Span));
            },
            span =>
            {
                // eof token
                Assert.Equal(Classification.Text, span.Classification);
                Assert.Equal(TextSpan.FromBounds(1 + newLineWidth, 2 + newLineWidth), span.Span);
                Assert.Equal("}", tree.File.ToString(span.Span));
            }
        );
    }

    [Fact]
    public void ClassifierSupportsAssignmentStatement()
    {
        var text = "val x =  \"hello world\" // tacos";
        var tree = SyntaxTree.Parse(text);
        var fullSpan = tree.Root.FullSpan;

        Assert.Collection(
            Classifier.Classify(tree, fullSpan),
            span =>
            {
                // val
                Assert.Equal(Classification.Keyword, span.Classification);
                Assert.Equal(TextSpan.FromBounds(0, 3), span.Span);
                Assert.Equal("val", tree.File.ToString(span.Span));
            },
            span =>
            {
                // x leading whitespace
                Assert.Equal(Classification.Text, span.Classification);
                Assert.Equal(TextSpan.FromBounds(3, 4), span.Span);
                Assert.Equal(" ", tree.File.ToString(span.Span));
            },
            span =>
            {
                // x
                Assert.Equal(Classification.Identifier, span.Classification);
                Assert.Equal(TextSpan.FromBounds(4, 5), span.Span);
                Assert.Equal("x", tree.File.ToString(span.Span));
            },
            span =>
            {
                // = leading whitespace
                Assert.Equal(Classification.Text, span.Classification);
                Assert.Equal(TextSpan.FromBounds(5, 6), span.Span);
                Assert.Equal(" ", tree.File.ToString(span.Span));
            },
            span =>
            {
                // =
                Assert.Equal(Classification.Text, span.Classification);
                Assert.Equal(TextSpan.FromBounds(6, 7), span.Span);
                Assert.Equal("=", tree.File.ToString(span.Span));
            },
            span =>
            {
                // string leading whitespace
                Assert.Equal(Classification.Text, span.Classification);
                Assert.Equal(TextSpan.FromBounds(7, 9), span.Span);
                Assert.Equal("  ", tree.File.ToString(span.Span));
            },
            span =>
            {
                // string
                Assert.Equal(Classification.String, span.Classification);
                Assert.Equal(TextSpan.FromBounds(9, 22), span.Span);
                Assert.Equal("\"hello world\"", tree.File.ToString(span.Span));
            },
            span =>
            {
                // comment whitespace
                Assert.Equal(Classification.Text, span.Classification);
                Assert.Equal(TextSpan.FromBounds(22, 23), span.Span);
                Assert.Equal(" ", tree.File.ToString(span.Span));
            },
            span =>
            {
                // comment
                Assert.Equal(Classification.Comment, span.Classification);
                Assert.Equal(TextSpan.FromBounds(23, 31), span.Span);
                Assert.Equal("// tacos", tree.File.ToString(span.Span));
            }
        );
    }

    [Fact]
    public void ClassifierSupportsNumbers()
    {
        var text = "27 // a number";
        var tree = SyntaxTree.Parse(text);
        var fullSpan = tree.Root.FullSpan;

        Assert.Collection(
            Classifier.Classify(tree, fullSpan),
            span =>
            {
                // 27
                Assert.Equal(Classification.Number, span.Classification);
                Assert.Equal(TextSpan.FromBounds(0, 2), span.Span);
                Assert.Equal("27", tree.File.ToString(span.Span));
            },
            span =>
            {
                // trailing whitespace
                Assert.Equal(Classification.Text, span.Classification);
                Assert.Equal(TextSpan.FromBounds(2, 3), span.Span);
                Assert.Equal(" ", tree.File.ToString(span.Span));
            },
            span =>
            {
                // comment
                Assert.Equal(Classification.Comment, span.Classification);
                Assert.Equal(TextSpan.FromBounds(3, 14), span.Span);
                Assert.Equal("// a number", tree.File.ToString(span.Span));
            }
        );
    }
}
