using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    internal abstract class BoundNode
    {
        public abstract BoundNodeKind Kind { get; }

        public IEnumerable<BoundNode> GetChildren()
        {
            var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                if (typeof(BoundNode).IsAssignableFrom(property.PropertyType))
                {
                    var child = (BoundNode)property.GetValue(this);
                    yield return child;
                }
                else if (typeof(IEnumerable<BoundNode>).IsAssignableFrom(property.PropertyType))
                {
                    var children = (IEnumerable<BoundNode>)property.GetValue(this);
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

        private IEnumerable<(string name, object value)> GetProperties()
        {
            var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                if (property.Name == nameof(Kind) || property.Name == nameof(BoundBinaryExpression.Operator))
                    continue;

                if (typeof(BoundNode).IsAssignableFrom(property.PropertyType) ||
                    typeof(IEnumerable<BoundNode>).IsAssignableFrom(property.PropertyType))
                    continue;

                var value = property.GetValue(this);
                if (value != null)
                {
                    yield return (property.Name, value);
                }
            }
        }

        private static void PrettyPrint(TextWriter writer, BoundNode node, string indent = "", bool isLast = true)
        {
            var isConsole = writer == Console.Out;

            var marker = isLast ? "└──" : "├──";

            writer.Write(indent);

            if (isConsole)
                Console.ForegroundColor = ConsoleColor.DarkGray;

            writer.Write(marker);

            if (isConsole)
                Console.ForegroundColor = GetColor(node);

            writer.Write(GetText(node));
            var firstProperty = true;

            foreach (var (name, value) in node.GetProperties())
            {
                if (firstProperty)
                {
                    writer.Write(" ");
                    firstProperty = false;
                }
                else
                {
                    if (isConsole) Console.ForegroundColor = ConsoleColor.DarkGray;
                    writer.Write(", ");
                }
                if (isConsole) Console.ForegroundColor = ConsoleColor.Yellow;
                writer.Write(name);
                if (isConsole) Console.ForegroundColor = ConsoleColor.DarkGray;
                writer.Write(" = ");
                if (isConsole) Console.ForegroundColor = ConsoleColor.DarkYellow;
                writer.Write(value);
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

        private static string GetText(BoundNode node)
        {
            if (node is BoundBinaryExpression binary)
                return $"{binary.Operator.Kind}Expression";

            if (node is BoundUnaryExpression unary)
                return $"{unary.Operator.Kind}Expression";

            return node.Kind.ToString();
        }

        private static ConsoleColor GetColor(BoundNode node)
        {
            if (node is BoundExpression)
                return ConsoleColor.Blue;

            if (node is BoundStatement)
                return ConsoleColor.Cyan;

            return ConsoleColor.Yellow;
        }
    }
}