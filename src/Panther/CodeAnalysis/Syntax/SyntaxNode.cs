using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Syntax
{
    public abstract class SyntaxNode
    {
        public SyntaxTree SyntaxTree { get; }

        protected SyntaxNode(SyntaxTree syntaxTree)
        {
            SyntaxTree = syntaxTree;
        }

        public abstract SyntaxKind Kind { get; }

        public virtual TextSpan Span
        {
            get
            {
                var children = GetChildren().ToArray();
                var first = children.First().Span;
                var last = children.Last().Span;

                return TextSpan.FromBounds(first.Start, last.End);
            }
        }

        public virtual TextSpan FullSpan
        {
            get
            {
                var children = GetChildren().ToArray();
                var first = children.First().FullSpan;
                var last = children.Last().FullSpan;

                return TextSpan.FromBounds(first.Start, last.End);
            }
        }

        public TextLocation Location => new TextLocation(SyntaxTree.Text, Span);

        public IEnumerable<SyntaxNode> GetChildren()
        {
            var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                if (typeof(SyntaxNode).IsAssignableFrom(property.PropertyType))
                {
                    var child = (SyntaxNode)property.GetValue(this);
                    if (child != null)
                        yield return child;
                }
                else if (typeof(SeparatedSyntaxList).IsAssignableFrom(property.PropertyType))
                {
                    var children = (SeparatedSyntaxList)property.GetValue(this);
                    if (children == null) continue;
                    foreach (var child in children.GetWithSeparators())
                        yield return child;
                }
                else if (typeof(IEnumerable<SyntaxNode>).IsAssignableFrom(property.PropertyType))
                {
                    var children = (IEnumerable<SyntaxNode>)property.GetValue(this);
                    if (children == null) continue;
                    foreach (var child in children)
                        yield return child;
                }
            }
        }

        public virtual IEnumerable<SyntaxNode> DescendantsAndSelf()
        {
            yield return this;

            foreach (var descendant in Descendants())
                yield return descendant;
        }

        public IEnumerable<SyntaxNode> Descendants() =>
            GetChildren()
                .SelectMany(child => child.DescendantsAndSelf());

        public void WriteTo(TextWriter writer)
        {
            PrettyPrint(writer, this);
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            WriteTo(writer);
            return writer.ToString();
        }

        private static void PrettyPrint(TextWriter writer, SyntaxNode node, string indent = "", bool isLast = true)
        {
            var isConsole = writer == Console.Out;

            var marker = isLast ? "└──" : "├──";

            writer.Write(indent);

            if (isConsole)
                Console.ForegroundColor = ConsoleColor.DarkGray;

            writer.Write(marker);

            if (isConsole)
                Console.ForegroundColor = node is SyntaxToken ? ConsoleColor.Blue : ConsoleColor.Cyan;

            writer.Write(node.Kind);

            if (node is SyntaxToken t)
            {
                if (t.Value != null)
                {
                    writer.Write(" ");
                    writer.Write(t.Value);
                }

                if (t.IsInsertedToken)
                {
                    writer.Write(" ");
                    writer.Write("(inserted)");
                }
            }

            if (isConsole)
                Console.ResetColor();

            writer.WriteLine();

            indent += isLast ? "    " : "│   ";

            using var enumerator = node.GetChildren().GetEnumerator();
            if (enumerator.MoveNext())
            {
                var previous = enumerator.Current;

                while (enumerator.MoveNext())
                {
                    PrettyPrint(writer, previous, indent, false);
                    previous = enumerator.Current;
                }

                PrettyPrint(writer, previous, indent);
            }
        }
    }
}