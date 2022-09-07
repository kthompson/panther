using System;
using System.CodeDom.Compiler;
using System.IO;
using Panther.CodeAnalysis.Syntax;
using Panther.IO;
using Type = Panther.CodeAnalysis.Symbols.Type;

namespace Panther.CodeAnalysis.Typing;

static class TypedNodePrinterExtensions
{
    public static void WriteTo(this TypedNode node, TextWriter writer)
    {
        var indentedWriter = writer is IndentedTextWriter indentedTextWriter
            ? indentedTextWriter
            : new IndentedTextWriter(writer);

        WriteTo(node, indentedWriter);
    }

    public static void WriteTo(this TypedNode node, IndentedTextWriter writer) =>
        TypedNodePrinter.WriteTo(node, writer);
}

internal class TypedNodePrinter : TypedNodeVisitor
{
    private readonly IndentedTextWriter _writer;

    private TypedNodePrinter(IndentedTextWriter writer)
    {
        _writer = writer;
    }

    public static void WriteTo(TypedNode node, IndentedTextWriter writer) =>
        node.Accept(new TypedNodePrinter(writer));

    private void WriteNestedExpression(TypedNode node)
    {
        if (node is TypedBlockExpression)
        {
            node.Accept(this);
            return;
        }

        _writer.Indent++;
        node.Accept(this);
        _writer.Indent--;
    }

    private void WriteNestedExpression(TypedExpression node, OperatorPrecedence parentPrecedence)
    {
        if (node is TypedBinaryExpression binaryExpression)
        {
            WriteNestedExpression(
                binaryExpression,
                parentPrecedence,
                binaryExpression.Operator.SyntaxKind.GetBinaryOperatorPrecedence()
                    ?? throw new Exception("Invalid operator")
            );
        }
        else
        {
            node.Accept(this);
        }
    }

    private void WriteNestedExpression(
        TypedNode node,
        OperatorPrecedence parent,
        OperatorPrecedence current
    )
    {
        if (parent >= current)
        {
            _writer.WritePunctuation("(");
            node.Accept(this);
            _writer.WritePunctuation(")");
        }
        else
        {
            node.Accept(this);
        }
    }

    protected override void DefaultVisit(TypedNode node) =>
        throw new NotSupportedException(node.Kind.ToString());

    public override void VisitTypeExpression(TypedTypeExpression node)
    {
        _writer.WriteIdentifier(node.Type.Symbol.Name);
    }

    public override void VisitIndexExpression(TypedIndexExpression node)
    {
        node.Expression.Accept(this);
        _writer.WritePunctuation("[");
        node.Index.Accept(this);
        _writer.WritePunctuation("]");
    }

    public override void VisitPropertyExpression(TypedPropertyExpression node)
    {
        node.Expression.Accept(this);
        _writer.WritePunctuation(".");
        _writer.WriteIdentifier(node.Property.Name);
    }

    public override void VisitNewExpression(TypedNewExpression node)
    {
        _writer.WriteKeyword("new ");
        _writer.WriteIdentifier(node.Type.Symbol.Name);
        _writer.WritePunctuation("(");
        var iterator = node.Arguments.GetEnumerator();
        if (iterator.MoveNext())
        {
            while (true)
            {
                iterator.Current.Accept(this);
                if (!iterator.MoveNext())
                    break;

                _writer.WritePunctuation(", ");
            }
        }
        _writer.WritePunctuation(")");
    }

    public override void VisitFieldExpression(TypedFieldExpression node)
    {
        if (node.Expression != null)
        {
            node.Expression.Accept(this);
        }
        else
        {
            _writer.WriteKeyword("this");
        }
        _writer.WritePunctuation(".");
        _writer.WriteIdentifier(node.Field.Name);
    }

    public override void VisitNopStatement(TypedNopStatement node)
    {
        _writer.WriteKeyword("nop");
        _writer.WriteLine();
    }

    public override void VisitAssignmentStatement(TypedAssignmentStatement node)
    {
        node.Left.Accept(this);
        _writer.WritePunctuation(" = ");
        node.Right.Accept(this);
        _writer.WriteLine();
    }

    public override void VisitAssignmentExpression(TypedAssignmentExpression node)
    {
        node.Left.Accept(this);
        _writer.WritePunctuation(" = ");
        node.Right.Accept(this);
    }

    public override void VisitIfExpression(TypedIfExpression node)
    {
        _writer.WriteKeyword("if ");
        _writer.WritePunctuation("(");
        node.Condition.Accept(this);
        _writer.WritePunctuation(") ");
        _writer.WriteLine();
        WriteNestedExpression(node.Then);
        _writer.WriteKeyword(" else ");
        _writer.WriteLine();
        WriteNestedExpression(node.Else);
    }

    public override void VisitUnitExpression(TypedUnitExpression node)
    {
        _writer.WritePunctuation("()");
    }

    public override void VisitVariableExpression(TypedVariableExpression node)
    {
        _writer.WriteIdentifier(node.Variable.Name);
    }

    public override void VisitForExpression(TypedForExpression node)
    {
        _writer.WriteKeyword("for ");
        _writer.WritePunctuation("(");
        _writer.WriteIdentifier(node.Variable.Name);
        _writer.WritePunctuation(" <- ");
        node.LowerTyped.Accept(this);
        _writer.WriteKeyword(" to ");
        node.UpperTyped.Accept(this);
        _writer.WritePunctuation(") ");
        _writer.WriteLine();
        WriteNestedExpression(node.Body);
    }

    public override void VisitWhileExpression(TypedWhileExpression node)
    {
        _writer.WriteKeyword("while ");
        _writer.WritePunctuation("(");
        node.Condition.Accept(this);
        _writer.WritePunctuation(") ");
        _writer.WriteLine();
        WriteNestedExpression(node.Body);
    }

    public override void VisitExpressionStatement(TypedExpressionStatement node)
    {
        node.Expression.Accept(this);
        _writer.WriteLine();
    }

    public override void VisitVariableDeclarationStatement(TypedVariableDeclarationStatement node)
    {
        _writer.WriteKeyword(node.Variable.IsReadOnly ? "val " : "var ");
        _writer.WriteIdentifier(node.Variable.Name);
        _writer.WritePunctuation(": ");
        _writer.WriteKeyword(node.Variable.Type.ToString());
        if (node.Expression != null)
        {
            _writer.WritePunctuation(" = ");
            node.Expression.Accept(this);
        }
        _writer.WriteLine();
    }

    public override void VisitCallExpression(TypedCallExpression node)
    {
        if (node.Expression != null)
        {
            node.Expression.Accept(this);
            _writer.WritePunctuation(".");
        }
        else if (!node.Method.IsStatic)
        {
            _writer.WriteKeyword("this");
            _writer.WritePunctuation(".");
        }
        _writer.WriteIdentifier(node.Method.Name);
        _writer.WritePunctuation("(");
        var iterator = node.Arguments.GetEnumerator();
        if (iterator.MoveNext())
        {
            while (true)
            {
                iterator.Current.Accept(this);
                if (!iterator.MoveNext())
                    break;

                _writer.WritePunctuation(", ");
            }
        }

        _writer.WritePunctuation(")");
    }

    public override void VisitBlockExpression(TypedBlockExpression node)
    {
        if (node.Statements.Length == 0)
        {
            _writer.WritePunctuation("{ ");
            node.Expression.Accept(this);
            _writer.WritePunctuation(" }");
            return;
        }

        _writer.WritePunctuation("{");
        _writer.WriteLine();
        _writer.Indent++;

        foreach (var statement in node.Statements)
        {
            statement.Accept(this);
        }

        node.Expression.Accept(this);
        _writer.WriteLine();

        _writer.Indent--;
        _writer.WritePunctuation("}");
    }

    public override void VisitLabelStatement(TypedLabelStatement node)
    {
        var dedent = (_writer.Indent > 0);
        if (dedent)
            _writer.Indent--;

        _writer.WritePunctuation(node.TypedLabel.Name);
        _writer.WritePunctuation(":");
        _writer.WriteLine();

        if (dedent)
            _writer.Indent++;
    }

    public override void VisitGotoStatement(TypedGotoStatement node)
    {
        _writer.WriteKeyword("goto ");
        _writer.WriteIdentifier(node.TypedLabel.Name);
        _writer.WriteLine();
    }

    public override void VisitConditionalGotoStatement(TypedConditionalGotoStatement node)
    {
        _writer.WriteKeyword("goto ");
        _writer.WriteIdentifier(node.TypedLabel.Name);
        _writer.WriteKeyword(node.JumpIfTrue ? " if " : " unless ");
        node.Condition.Accept(this);
        _writer.WriteLine();
    }

    public override void VisitErrorExpression(TypedErrorExpression node)
    {
        _writer.WriteKeyword("err");
    }

    public override void VisitLiteralExpression(TypedLiteralExpression node)
    {
        var value = node.Value.ToString() ?? "";
        if (node.Type == Type.Bool)
        {
            _writer.WriteKeyword(value);
        }
        else if (node.Type == Type.Int)
        {
            _writer.WriteNumber(value);
        }
        else if (node.Type == Type.String)
        {
            _writer.WriteString("\"" + value.Replace("\"", "\"\"") + "\"");
        }
        else if (node.Type == Type.Char)
        {
            _writer.WriteString("'" + value.Replace("'", "\\'") + "'");
        }
        else
        {
            throw new Exception($"Unexpected kind for literal expression: {node.Kind}");
        }
    }

    public override void VisitUnaryExpression(TypedUnaryExpression node)
    {
        var op =
            SyntaxFacts.GetText(node.Operator.SyntaxKind)
            ?? throw new Exception("Invalid operator");

        _writer.WritePunctuation(op);
        WriteNestedExpression(node.Operand, OperatorPrecedence.Prefix);
    }

    public override void VisitBinaryExpression(TypedBinaryExpression node)
    {
        var op =
            SyntaxFacts.GetText(node.Operator.SyntaxKind)
            ?? throw new Exception("Invalid operator");
        var precedence =
            node.Operator.SyntaxKind.GetBinaryOperatorPrecedence()
            ?? throw new Exception("Invalid operator");

        WriteNestedExpression(node.Left, precedence);
        _writer.Write(" ");
        _writer.WritePunctuation(op);
        _writer.Write(" ");
        WriteNestedExpression(node.Right, precedence);
    }

    public override void VisitConversionExpression(TypedConversionExpression node)
    {
        //TODO: writer.WriteIdentifier(node.Type.Name);
        _writer.WriteIdentifier(node.Type.ToString());
        _writer.WritePunctuation("(");
        node.Expression.Accept(this);
        _writer.WritePunctuation(")");
    }

    // private IEnumerable<(string name, object value)> GetProperties()
    // {
    //     var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
    //     foreach (var property in properties)
    //     {
    //         if (property.Name == nameof(Kind) || property.Name == nameof(TypedBinaryExpression.Operator))
    //             continue;
    //
    //         if (typeof(TypedNode).IsAssignableFrom(property.PropertyType) ||
    //             typeof(IEnumerable<TypedNode>).IsAssignableFrom(property.PropertyType))
    //             continue;
    //
    //         var value = property.GetValue(this);
    //         if (value != null)
    //         {
    //             yield return (property.Name, value);
    //         }
    //     }
    // }

    // private static void PrettyPrint(TextWriter writer, TypedNode node, string indent = "", bool isLast = true)
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
    // private static string GetText(TypedNode node)
    // {
    //     if (node is TypedBinaryExpression binary)
    //         return $"{binary.Operator.Kind}Expression";
    //
    //     if (node is TypedUnaryExpression unary)
    //         return $"{unary.Operator.Kind}Expression";
    //
    //     return node.Kind.ToString();
    // }
    //
    // private static ConsoleColor GetColor(TypedNode node)
    // {
    //     if (node is TypedExpression)
    //         return ConsoleColor.Blue;
    //
    //     if (node is TypedStatement)
    //         return ConsoleColor.Cyan;
    //
    //     return ConsoleColor.Yellow;
    // }
}
