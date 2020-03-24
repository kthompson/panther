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

        public IEnumerable<SyntaxNode> GetChildren()
        {
            var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                if (typeof(SyntaxNode).IsAssignableFrom(property.PropertyType))
                {
                    var child = (SyntaxNode)property.GetValue(this);
                    yield return child;
                }
                else if (typeof(IEnumerable<SyntaxNode>).IsAssignableFrom(property.PropertyType))
                {
                    var children = (IEnumerable<SyntaxNode>)property.GetValue(this);
                    foreach (var child in children)
                        yield return child;
                }
            }
        }

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

            if (node is SyntaxToken t && t.Value != null)
            {
                writer.Write(" ");
                writer.Write(t.Value);
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