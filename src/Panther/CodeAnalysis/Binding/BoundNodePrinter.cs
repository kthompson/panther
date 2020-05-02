using System;
using System.CodeDom.Compiler;
using System.IO;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;
using Panther.IO;

namespace Panther.CodeAnalysis.Binding
{
    internal static class BoundNodePrinter
    {
        public static void WriteTo(this BoundNode node, TextWriter writer)
        {
            var indentedWriter = writer is IndentedTextWriter indentedTextWriter
                ? indentedTextWriter
                : new IndentedTextWriter(writer);

            WriteTo(node, indentedWriter);
        }

        public static void WriteTo(this BoundNode node, IndentedTextWriter writer)
        {
            switch (node)
            {
                case BoundBinaryExpression boundBinaryExpression:
                    WriteBinaryExpression(boundBinaryExpression, writer);
                    break;
                case BoundBlockExpression boundBlockExpression:
                    WriteBlockExpression(boundBlockExpression, writer);
                    break;
                case BoundCallExpression boundCallExpression:
                    WriteCallExpression(boundCallExpression, writer);
                    break;
                case BoundConversionExpression boundConversionExpression:
                    WriteConversionExpression(boundConversionExpression, writer);
                    break;
                case BoundErrorExpression boundErrorExpression:
                    WriteErrorExpression(boundErrorExpression, writer);
                    break;
                case BoundAssignmentExpression boundAssignmentExpression:
                    WriteAssignmentExpression(boundAssignmentExpression, writer);
                    break;
                case BoundForExpression boundForExpression:
                    WriteForExpression(boundForExpression, writer);
                    break;
                case BoundIfExpression boundIfExpression:
                    WriteIfExpression(boundIfExpression, writer);
                    break;
                case BoundLiteralExpression boundLiteralExpression:
                    WriteLiteralExpression(boundLiteralExpression, writer);
                    break;
                case BoundUnaryExpression boundUnaryExpression:
                    WriteUnaryExpression(boundUnaryExpression, writer);
                    break;
                case BoundUnitExpression boundUnitExpression:
                    WriteUnitExpression(boundUnitExpression, writer);
                    break;
                case BoundVariableExpression boundVariableExpression:
                    WriteVariableExpression(boundVariableExpression, writer);
                    break;
                case BoundWhileExpression boundWhileExpression:
                    WriteWhileExpression(boundWhileExpression, writer);
                    break;
                case BoundConditionalGotoStatement boundConditionalGotoStatement:
                    WriteConditionalGotoStatement(boundConditionalGotoStatement, writer);
                    break;
                case BoundExpressionStatement boundExpressionStatement:
                    WriteExpressionStatement(boundExpressionStatement, writer);
                    break;
                case BoundGotoStatement boundGotoStatement:
                    WriteGotoStatement(boundGotoStatement, writer);
                    break;
                case BoundLabelStatement boundLabelStatement:
                    WriteLabelStatement(boundLabelStatement, writer);
                    break;
                case BoundVariableDeclarationStatement boundVariableDeclarationStatement:
                    WriteVariableDeclarationStatement(boundVariableDeclarationStatement, writer);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(node));
            }
        }

        private static void WriteNestedExpression(this IndentedTextWriter writer, BoundNode node)
        {
            if (node is BoundBlockExpression)
            {
                node.WriteTo(writer);
                return;
            }

            writer.Indent++;
            node.WriteTo(writer);
            writer.Indent--;
        }

        private static void WriteAssignmentExpression(BoundAssignmentExpression node, IndentedTextWriter writer)
        {
            writer.WriteIdentifier(node.Variable.Name);
            writer.WritePunctuation(" = ");
            node.Expression.WriteTo(writer);
        }

        private static void WriteIfExpression(BoundIfExpression node, IndentedTextWriter writer)
        {
            writer.WriteKeyword("if ");
            writer.WritePunctuation("(");
            node.Condition.WriteTo(writer);
            writer.WritePunctuation(") ");
            writer.WriteLine();
            writer.WriteNestedExpression(node.Then);
            writer.WriteKeyword(" else ");
            writer.WriteLine();
            writer.WriteNestedExpression(node.Else);
        }

        private static void WriteUnitExpression(BoundUnitExpression node, IndentedTextWriter writer)
        {
            writer.WritePunctuation("()");
        }

        private static void WriteVariableExpression(BoundVariableExpression node, IndentedTextWriter writer)
        {
            writer.WriteIdentifier(node.Variable.Name);
        }

        private static void WriteForExpression(BoundForExpression node, IndentedTextWriter writer)
        {
            writer.WriteKeyword("for ");
            writer.WritePunctuation("(");
            writer.WriteIdentifier(node.Variable.Name);
            writer.WritePunctuation(" <- ");
            node.LowerBound.WriteTo(writer);
            writer.WriteKeyword(" to ");
            node.UpperBound.WriteTo(writer);
            writer.WritePunctuation(") ");
            writer.WriteLine();
            writer.WriteNestedExpression(node.Body);
        }

        private static void WriteWhileExpression(BoundWhileExpression node, IndentedTextWriter writer)
        {
            writer.WriteKeyword("while ");
            writer.WritePunctuation("(");
            node.Condition.WriteTo(writer);
            writer.WritePunctuation(") ");
            writer.WriteLine();
            writer.WriteNestedExpression(node.Body);
        }

        private static void WriteExpressionStatement(BoundExpressionStatement node, IndentedTextWriter writer)
        {
            node.Expression.WriteTo(writer);
            writer.WriteLine();
        }

        private static void WriteVariableDeclarationStatement(BoundVariableDeclarationStatement node, IndentedTextWriter writer)
        {
            writer.WriteKeyword(node.Variable.IsReadOnly ? "val " : "var ");
            writer.WriteIdentifier(node.Variable.Name);
            writer.WritePunctuation(" = ");
            node.Expression.WriteTo(writer);
            writer.WriteLine();
        }

        private static void WriteCallExpression(BoundCallExpression node, IndentedTextWriter writer)
        {
            writer.WriteIdentifier(node.Function.Name);
            writer.WritePunctuation("(");
            var iterator = node.Arguments.GetEnumerator();
            if (iterator.MoveNext())
            {
                while (true)
                {
                    iterator.Current.WriteTo(writer);
                    if (!iterator.MoveNext())
                        break;

                    writer.WritePunctuation(", ");
                }
            }

            writer.WritePunctuation(")");
        }

        private static void WriteBlockExpression(BoundBlockExpression node, IndentedTextWriter writer)
        {
            if (node.Statements.Length == 0)
            {
                writer.WritePunctuation("{ ");
                node.Expression.WriteTo(writer);
                writer.WritePunctuation(" }");
                return;
            }

            writer.WritePunctuation("{");
            writer.WriteLine();
            writer.Indent++;

            foreach (var statement in node.Statements)
            {
                statement.WriteTo(writer);
            }

            node.Expression.WriteTo(writer);
            writer.WriteLine();

            writer.Indent--;
            writer.WritePunctuation("}");
        }


        private static void WriteLabelStatement(BoundLabelStatement node, IndentedTextWriter writer)
        {
            var dedent = (writer.Indent > 0);
            if (dedent)
                writer.Indent--;

            writer.WritePunctuation(node.BoundLabel.Name);
            writer.WritePunctuation(":");
            writer.WriteLine();

            if (dedent)
                writer.Indent++;
        }

        private static void WriteGotoStatement(BoundGotoStatement node, IndentedTextWriter writer)
        {
            writer.WriteKeyword("goto ");
            writer.WriteIdentifier(node.BoundLabel.Name);
            writer.WriteLine();
        }

        private static void WriteConditionalGotoStatement(BoundConditionalGotoStatement node, IndentedTextWriter writer)
        {
            writer.WriteKeyword("goto ");
            writer.WriteIdentifier(node.BoundLabel.Name);
            writer.WriteKeyword(node.JumpIfTrue ? " if " : " unless ");
            node.Condition.WriteTo(writer);
            writer.WriteLine();
        }

        private static void WriteErrorExpression(BoundErrorExpression node, IndentedTextWriter writer)
        {
            writer.WriteKeyword("err");
        }

        private static void WriteLiteralExpression(BoundLiteralExpression node, IndentedTextWriter writer)
        {
            var value = node.Value.ToString();
            if (node.Type == TypeSymbol.Bool)
            {
                writer.WriteKeyword(value);
            }
            else if (node.Type == TypeSymbol.Int)
            {
                writer.WriteNumber(value);
            }
            else if (node.Type == TypeSymbol.String)
            {
                writer.WriteString("\"" + value.Replace("\"", "\"\"") + "\"");
            }
            else
            {
                throw new Exception($"Unexpected kind for literal expression: {node.Kind}");
            }
        }

        private static void WriteUnaryExpression(BoundUnaryExpression node, IndentedTextWriter writer)
        {
            var op = SyntaxFacts.GetText(node.Operator.SyntaxKind) ??
                             throw new Exception("Invalid operator");

            writer.WritePunctuation(op);
            writer.WriteNestedExpression(node.Operand, OperatorPrecedence.Prefix);
        }

        private static void WriteBinaryExpression(BoundBinaryExpression node, IndentedTextWriter writer)
        {
            var op = SyntaxFacts.GetText(node.Operator.SyntaxKind) ??
                             throw new Exception("Invalid operator");
            var precedence = node.Operator.SyntaxKind.GetBinaryOperatorPrecedence() ??
                             throw new Exception("Invalid operator");


            writer.WriteNestedExpression(node.Left, precedence);
            writer.Write(" ");
            writer.WritePunctuation(op);
            writer.Write(" ");
            writer.WriteNestedExpression(node.Right, precedence);
        }

        private static void WriteNestedExpression(this IndentedTextWriter writer,
            BoundExpression node, OperatorPrecedence parentPrecedence)
        {
            if (node is BoundBinaryExpression binaryExpression)
            {
                writer.WriteNestedExpression(binaryExpression, parentPrecedence, binaryExpression.Operator.SyntaxKind.GetBinaryOperatorPrecedence() ?? throw new Exception("Invalid operator"));
            }
            else
            {
                node.WriteTo(writer);
            }
        }


        private static void WriteNestedExpression(this IndentedTextWriter writer, BoundNode node, OperatorPrecedence parent, OperatorPrecedence current)
        {
            if (parent >= current)
            {
                writer.WritePunctuation("(");
                node.WriteTo(writer);
                writer.WritePunctuation(")");
            }
            else
            {
                node.WriteTo(writer);
            }
        }

        private static void WriteConversionExpression(BoundConversionExpression node, IndentedTextWriter writer)
        {
            writer.WriteIdentifier(node.Type.Name);
            writer.WritePunctuation("(");
            node.Expression.WriteTo(writer);
            writer.WritePunctuation(")");
        }

        // private IEnumerable<(string name, object value)> GetProperties()
        // {
        //     var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        //     foreach (var property in properties)
        //     {
        //         if (property.Name == nameof(Kind) || property.Name == nameof(BoundBinaryExpression.Operator))
        //             continue;
        //
        //         if (typeof(BoundNode).IsAssignableFrom(property.PropertyType) ||
        //             typeof(IEnumerable<BoundNode>).IsAssignableFrom(property.PropertyType))
        //             continue;
        //
        //         var value = property.GetValue(this);
        //         if (value != null)
        //         {
        //             yield return (property.Name, value);
        //         }
        //     }
        // }

        // private static void PrettyPrint(TextWriter writer, BoundNode node, string indent = "", bool isLast = true)
        // {
        //     var isConsole = writer == Console.Out;
        //
        //     var marker = isLast ? "└──" : "├──";
        //
        //     writer.Write(indent);
        //
        //     if (isConsole)
        //         Console.ForegroundColor = ConsoleColor.DarkGray;
        //
        //     writer.Write(marker);
        //
        //     writer.SetForeground(GetColor(node));
        //
        //     writer.Write(GetText(node));
        //     var firstProperty = true;
        //
        //     foreach (var (name, value) in node.GetProperties())
        //     {
        //         if (firstProperty)
        //         {
        //             writer.Write(" ");
        //             firstProperty = false;
        //         }
        //         else
        //         {
        //             if (isConsole) Console.ForegroundColor = ConsoleColor.DarkGray;
        //             writer.Write(", ");
        //         }
        //         if (isConsole) Console.ForegroundColor = ConsoleColor.Yellow;
        //         writer.Write(name);
        //         if (isConsole) Console.ForegroundColor = ConsoleColor.DarkGray;
        //         writer.Write(" = ");
        //         if (isConsole) Console.ForegroundColor = ConsoleColor.DarkYellow;
        //         writer.Write(value);
        //     }
        //
        //     if (isConsole)
        //         Console.ResetColor();
        //
        //     writer.WriteLine();
        //
        //     indent += isLast ? "    " : "│   ";
        //
        //     using var enumerator = node.GetChildren().GetEnumerator();
        //     if (enumerator.MoveNext())
        //     {
        //         var previous = enumerator.Current;
        //
        //         while (enumerator.MoveNext())
        //         {
        //             PrettyPrint(writer, previous, indent, false);
        //             previous = enumerator.Current;
        //         }
        //
        //         PrettyPrint(writer, previous, indent);
        //     }
        // }
        //
        // private static string GetText(BoundNode node)
        // {
        //     if (node is BoundBinaryExpression binary)
        //         return $"{binary.Operator.Kind}Expression";
        //
        //     if (node is BoundUnaryExpression unary)
        //         return $"{unary.Operator.Kind}Expression";
        //
        //     return node.Kind.ToString();
        // }
        //
        // private static ConsoleColor GetColor(BoundNode node)
        // {
        //     if (node is BoundExpression)
        //         return ConsoleColor.Blue;
        //
        //     if (node is BoundStatement)
        //         return ConsoleColor.Cyan;
        //
        //     return ConsoleColor.Yellow;
        // }
    }
}