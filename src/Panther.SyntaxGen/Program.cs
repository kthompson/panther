using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

#nullable disable

namespace Panther.SyntaxGen;

class Program
{
    private static Tree _tree;
    private static Tree _boundTree;

    static void Main(string[] args)
    {
        _boundTree = ReadTree("Typed.xml");
        _tree = ReadTree("Syntax.xml");

        GenerateSyntaxTree();
        GenerateSyntaxVisitor();

        GenerateTypedTree();
        GenerateTypedVisitor();
    }

    private static void GenerateTypedTree()
    {
        using var writer = new StringWriter();
        using var indentedTextWriter = new IndentedTextWriter(writer);

        indentedTextWriter.WriteLine("using System;");
        indentedTextWriter.WriteLine("using System.IO;");
        indentedTextWriter.WriteLine("using System.Collections.Generic;");
        indentedTextWriter.WriteLine("using System.Collections.Immutable;");
        indentedTextWriter.WriteLine("using Panther.CodeAnalysis.Symbols;");
        indentedTextWriter.WriteLine("using Panther.CodeAnalysis.Syntax;");
        indentedTextWriter.WriteLine();
        indentedTextWriter.WriteLine("#nullable enable");
        indentedTextWriter.WriteLine();
        indentedTextWriter.WriteLine("namespace Panther.CodeAnalysis.Typing;");

        foreach (var node in _boundTree.Types.OfType<AbstractNode>())
        {
            var name = node.Name;
            indentedTextWriter.Write($"internal abstract partial record {name}(SyntaxNode Syntax");

            foreach (var field in node.Fields)
            {
                indentedTextWriter.Write(", ");
                indentedTextWriter.Write(field.Type);
                indentedTextWriter.Write(" ");
                indentedTextWriter.Write(field.Name);
            }

            indentedTextWriter.WriteLine(")");
            indentedTextWriter.Indent++;
            indentedTextWriter.Write(": ");
            indentedTextWriter.Write(node.Base ?? _boundTree.Root);
            indentedTextWriter.Write("(Syntax);");
            indentedTextWriter.Indent--;
            indentedTextWriter.WriteLine();
            indentedTextWriter.WriteLineNoTabs("");
        }

        foreach (var node in _boundTree.Types.OfType<Node>())
        {
            var name = node.Name;
            indentedTextWriter.Write($"internal sealed partial record {name}(SyntaxNode Syntax");

            foreach (var field in node.Fields)
            {
                indentedTextWriter.Write(", ");
                indentedTextWriter.Write(field.Type);
                indentedTextWriter.Write(" ");
                indentedTextWriter.Write(field.Name);
            }

            indentedTextWriter.WriteLine(")");
            indentedTextWriter.Indent++;
            indentedTextWriter.Write(": ");
            indentedTextWriter.Write(node.Base ?? _boundTree.Root);
            indentedTextWriter.WriteLine("(Syntax) {");

            EmitTypedKind(node, indentedTextWriter);

            EmitToString(indentedTextWriter);

            EmitAcceptTypedNodeVisitor(indentedTextWriter, name);

            indentedTextWriter.Indent--;
            indentedTextWriter.WriteLine("}");
            writer.WriteLine();
        }

        var contents = writer.ToString();

        var path = Path.Combine(
            "..",
            "..",
            "..",
            "..",
            "Panther",
            "CodeAnalysis",
            "Typing",
            "Typing.g.cs"
        );
        File.WriteAllText(path, contents);
        Console.WriteLine($"Wrote bound nodes to {path}");
    }

    private static void GenerateTypedVisitor()
    {
        using var writer = new StringWriter();
        using var indentedTextWriter = new IndentedTextWriter(writer);

        indentedTextWriter.WriteLine("using System;");
        indentedTextWriter.WriteLine("using System.IO;");
        indentedTextWriter.WriteLine("using System.Collections.Generic;");
        indentedTextWriter.WriteLine("using System.Collections.Immutable;");
        indentedTextWriter.WriteLine();
        indentedTextWriter.WriteLine("#nullable enable");
        indentedTextWriter.WriteLine();
        indentedTextWriter.WriteLine("namespace Panther.CodeAnalysis.Typing;");

        indentedTextWriter.WriteLine("internal partial class TypedNodeVisitor");
        indentedTextWriter.WriteLine("{");
        indentedTextWriter.Indent++;

        using (var enumerator = _boundTree.ConcreteNodes.GetEnumerator())
        {
            if (enumerator.MoveNext())
            {
                var node = enumerator.Current;

                while (enumerator.MoveNext())
                {
                    WriteVisitorMethod(indentedTextWriter, node, false);
                    indentedTextWriter.WriteLineNoTabs("");

                    node = enumerator.Current;
                }

                WriteVisitorMethod(indentedTextWriter, node, false);
            }
        }

        indentedTextWriter.Indent--;
        indentedTextWriter.WriteLine("}");

        indentedTextWriter.WriteLine("internal partial class TypedNodeVisitor<TResult>");
        indentedTextWriter.WriteLine("{");
        indentedTextWriter.Indent++;

        using (var enumerator = _boundTree.ConcreteNodes.GetEnumerator())
        {
            if (enumerator.MoveNext())
            {
                var node = enumerator.Current;

                while (enumerator.MoveNext())
                {
                    WriteVisitorMethod(indentedTextWriter, node, true);
                    indentedTextWriter.WriteLineNoTabs("");

                    node = enumerator.Current;
                }

                WriteVisitorMethod(indentedTextWriter, node, true);
            }
        }

        indentedTextWriter.Indent--;
        indentedTextWriter.WriteLine("}");

        var contents = writer.ToString();

        var path = Path.Combine(
            "..",
            "..",
            "..",
            "..",
            "Panther",
            "CodeAnalysis",
            "Typing",
            "TypedNodeVisitor.g.cs"
        );
        File.WriteAllText(path, contents);
        Console.WriteLine($"Wrote bound visitor to {path}");
    }

    private static void GenerateSyntaxVisitor()
    {
        using var writer = new StringWriter();
        using var indentedTextWriter = new IndentedTextWriter(writer);

        indentedTextWriter.WriteLine("using System;");
        indentedTextWriter.WriteLine("using System.IO;");
        indentedTextWriter.WriteLine("using System.Collections.Generic;");
        indentedTextWriter.WriteLine("using System.Collections.Immutable;");
        indentedTextWriter.WriteLine();
        indentedTextWriter.WriteLine("#nullable enable");
        indentedTextWriter.WriteLine();
        indentedTextWriter.WriteLine("namespace Panther.CodeAnalysis.Syntax;");

        indentedTextWriter.WriteLine("public partial class SyntaxVisitor");
        indentedTextWriter.WriteLine("{");
        indentedTextWriter.Indent++;

        using (var enumerator = _tree.ConcreteNodes.GetEnumerator())
        {
            if (enumerator.MoveNext())
            {
                var node = enumerator.Current;

                while (enumerator.MoveNext())
                {
                    WriteVisitorMethod(indentedTextWriter, node, false);
                    indentedTextWriter.WriteLineNoTabs("");

                    node = enumerator.Current;
                }

                WriteVisitorMethod(indentedTextWriter, node, false);
            }
        }

        indentedTextWriter.Indent--;
        indentedTextWriter.WriteLine("}");

        indentedTextWriter.WriteLine("public partial class SyntaxVisitor<TResult>");
        indentedTextWriter.WriteLine("{");
        indentedTextWriter.Indent++;

        using (var enumerator = _tree.ConcreteNodes.GetEnumerator())
        {
            if (enumerator.MoveNext())
            {
                var node = enumerator.Current;

                while (enumerator.MoveNext())
                {
                    WriteVisitorMethod(indentedTextWriter, node, true);
                    indentedTextWriter.WriteLineNoTabs("");

                    node = enumerator.Current;
                }

                WriteVisitorMethod(indentedTextWriter, node, true);
            }
        }

        indentedTextWriter.Indent--;
        indentedTextWriter.WriteLine("}");

        var contents = writer.ToString();

        var path = Path.Combine(
            "..",
            "..",
            "..",
            "..",
            "Panther",
            "CodeAnalysis",
            "Syntax",
            "SyntaxVisitor.g.cs"
        );
        File.WriteAllText(path, contents);
        Console.WriteLine($"Wrote syntax visitor to {path}");
    }

    private static void WriteVisitorMethod(
        IndentedTextWriter indentedTextWriter,
        TreeType node,
        bool generic
    )
    {
        var name = node.Name;
        var methodName = Regex.Replace(Regex.Replace(name, "^Typed", ""), "(Syntax$|^Syntax)", "");
        var returnType = generic ? "TResult" : "void";
        indentedTextWriter.WriteLine(
            $"public virtual {returnType} Visit{methodName}({name} node) =>"
        );
        indentedTextWriter.Indent++;
        indentedTextWriter.WriteLine("this.DefaultVisit(node);");
        indentedTextWriter.Indent--;
    }

    private static void GenerateSyntaxTree()
    {
        using var writer = new StringWriter();
        using var indentedTextWriter = new IndentedTextWriter(writer);

        indentedTextWriter.WriteLine("using System;");
        indentedTextWriter.WriteLine("using System.IO;");
        indentedTextWriter.WriteLine("using System.Collections.Generic;");
        indentedTextWriter.WriteLine("using System.Collections.Immutable;");
        indentedTextWriter.WriteLine("using Panther.CodeAnalysis.Text;");
        indentedTextWriter.WriteLine();
        indentedTextWriter.WriteLine("#nullable enable");
        indentedTextWriter.WriteLine();
        indentedTextWriter.WriteLine("namespace Panther.CodeAnalysis.Syntax;");

        foreach (var node in _tree.Types.OfType<AbstractNode>())
        {
            var name = node.Name;
            indentedTextWriter.Write(
                $"public abstract partial record {name}(SourceFile SourceFile"
            );

            foreach (var field in node.Fields)
            {
                indentedTextWriter.Write(", ");
                indentedTextWriter.Write(field.Type);
                indentedTextWriter.Write(" ");
                indentedTextWriter.Write(field.Name);
            }

            indentedTextWriter.WriteLine(")");
            indentedTextWriter.Indent++;
            indentedTextWriter.Write(": ");
            indentedTextWriter.Write(node.Base ?? _tree.Root);
            indentedTextWriter.Write("(SourceFile);");
            indentedTextWriter.Indent--;
            indentedTextWriter.WriteLine();
            indentedTextWriter.WriteLineNoTabs("");
        }

        foreach (var node in _tree.Types.OfType<Node>())
        {
            var name = node.Name;
            indentedTextWriter.Write($"public sealed partial record {name}(SourceFile SourceFile");

            foreach (var field in node.Fields)
            {
                indentedTextWriter.Write(", ");
                indentedTextWriter.Write(field.Type);
                indentedTextWriter.Write(" ");
                indentedTextWriter.Write(field.Name);
            }

            indentedTextWriter.WriteLine(")");
            indentedTextWriter.Indent++;
            indentedTextWriter.Write(": ");
            indentedTextWriter.Write(node.Base ?? _tree.Root);
            indentedTextWriter.WriteLine("(SourceFile) {");

            EmitKind(node, indentedTextWriter);

            EmitGetHashCode(indentedTextWriter, node);

            EmitGetChildren(indentedTextWriter, node, writer);

            EmitToString(indentedTextWriter);

            EmitAcceptSyntaxVisitor(indentedTextWriter, name);

            indentedTextWriter.Indent--;
            indentedTextWriter.WriteLine("}");
            writer.WriteLine();
        }

        var contents = writer.ToString();

        var path = Path.Combine(
            "..",
            "..",
            "..",
            "..",
            "Panther",
            "CodeAnalysis",
            "Syntax",
            "Syntax.g.cs"
        );
        File.WriteAllText(path, contents);
        Console.WriteLine($"Wrote syntax to {path}");
    }

    private static void EmitAcceptTypedNodeVisitor(IndentedTextWriter writer, string name)
    {
        var methodName = Regex.Replace(name, "^Typed", "");
        writer.WriteLineNoTabs("");
        writer.WriteLine(
            $"public override void Accept(TypedNodeVisitor visitor) => visitor.Visit{methodName}(this);"
        );
        writer.WriteLineNoTabs("");
        writer.WriteLine(
            $"public override TResult Accept<TResult>(TypedNodeVisitor<TResult> visitor) => visitor.Visit{methodName}(this);"
        );
    }

    private static void EmitAcceptSyntaxVisitor(IndentedTextWriter writer, string name)
    {
        var methodName = Regex.Replace(name, "(Syntax$|^Syntax)", "");
        writer.WriteLineNoTabs("");
        writer.WriteLine(
            $"public override void Accept(SyntaxVisitor visitor) => visitor.Visit{methodName}(this);"
        );
        writer.WriteLineNoTabs("");
        writer.WriteLine(
            $"public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.Visit{methodName}(this);"
        );
    }

    private static void EmitTypedKind(Node node, IndentedTextWriter indentedTextWriter)
    {
        var kind = node.Kinds.FirstOrDefault();
        if (kind != null)
        {
            indentedTextWriter.WriteLine(
                $"public override TypedNodeKind Kind => TypedNodeKind.{kind.Name};"
            );
            indentedTextWriter.WriteLineNoTabs("");
        }
    }

    private static void EmitKind(Node node, IndentedTextWriter indentedTextWriter, string kindType = "Syntax")
    {
        var kind = node.Kinds.FirstOrDefault();
        if (kind != null)
        {
            indentedTextWriter.WriteLine(
                $"public override {kindType}Kind Kind => {kindType}Kind.{kind.Name};"
            );
            indentedTextWriter.WriteLineNoTabs("");
        }
    }
    
    private static void EmitToString(IndentedTextWriter indentedTextWriter)
    {
        indentedTextWriter.WriteLine("public override string ToString()");
        indentedTextWriter.WriteLine("{");
        indentedTextWriter.Indent++;
        indentedTextWriter.WriteLine("using var writer = new StringWriter();");
        indentedTextWriter.WriteLine("this.WriteTo(writer);");
        indentedTextWriter.WriteLine("return writer.ToString();");
        indentedTextWriter.Indent--;
        indentedTextWriter.WriteLine("}");
    }

    private static void EmitGetChildren(
        IndentedTextWriter indentedTextWriter,
        Node node,
        StringWriter writer
    )
    {
        indentedTextWriter.WriteLine("public override IEnumerable<SyntaxNode> GetChildren()");
        indentedTextWriter.WriteLine("{");
        indentedTextWriter.Indent++;
        foreach (var field in node.Fields)
        {
            var canBeNull = field.Type.EndsWith("?");
            if (canBeNull)
            {
                indentedTextWriter.WriteLine($"if ({field.Name} != null)");
                indentedTextWriter.WriteLine("{");
                indentedTextWriter.Indent++;
            }

            // TODO: verify that the field is a ImmutableArray<T> where T : SyntaxNode
            if (field.Type.StartsWith("ImmutableArray<"))
            {
                indentedTextWriter.WriteLine($"foreach (var child in {field.Name})");
                indentedTextWriter.Indent++;
                indentedTextWriter.WriteLine($"yield return child;");
                indentedTextWriter.Indent--;
                indentedTextWriter.WriteLineNoTabs("");
            }
            else if (field.Type.StartsWith("SeparatedSyntaxList<"))
            {
                indentedTextWriter.WriteLine(
                    $"foreach (var child in {field.Name}.GetWithSeparators())"
                );
                indentedTextWriter.Indent++;
                indentedTextWriter.WriteLine($"yield return child;");
                indentedTextWriter.Indent--;
            }
            else
            {
                indentedTextWriter.WriteLine($"yield return {field.Name};");
            }

            if (canBeNull)
            {
                indentedTextWriter.Indent--;
                indentedTextWriter.WriteLine("}");
            }
        }

        indentedTextWriter.Indent--;
        indentedTextWriter.WriteLine("}");
        indentedTextWriter.WriteLineNoTabs("");
    }

    private static void EmitEquals(IndentedTextWriter indentedTextWriter, string name, Node node)
    {
        indentedTextWriter.WriteLine($"public override bool Equals({name}? other)");
        indentedTextWriter.WriteLine("{");
        indentedTextWriter.Indent++;
        indentedTextWriter.WriteLine("if (ReferenceEquals(null, other)) return false;");
        indentedTextWriter.WriteLine("if (ReferenceEquals(this, other)) return true;");
        indentedTextWriter.WriteLine("return ");
        indentedTextWriter.Indent++;
        using (var enumerator = node.Fields.GetEnumerator())
        {
            if (enumerator.MoveNext())
            {
                var field = enumerator.Current;

                while (enumerator.MoveNext())
                {
                    indentedTextWriter.WriteLine($"{field!.Name}.Equals(other.{field.Name}) &&");
                    field = enumerator.Current;
                }

                indentedTextWriter.WriteLine($"{field!.Name}.Equals(other.{field.Name});");
            }
        }

        indentedTextWriter.Indent--;
        indentedTextWriter.Indent--;
        indentedTextWriter.WriteLine("}");
        indentedTextWriter.WriteLineNoTabs("");
    }

    private static void EmitGetHashCode(IndentedTextWriter indentedTextWriter, Node node)
    {
        indentedTextWriter.WriteLine("public override int GetHashCode()");
        indentedTextWriter.WriteLine("{");
        indentedTextWriter.Indent++;
        var fields = node.Fields.ToList();
        if (fields.Count > 8)
        {
            indentedTextWriter.WriteLine("var hc = new HashCode();");
            foreach (var field in fields)
            {
                indentedTextWriter.WriteLine($"hc.Add({field!.Name});");
            }
            indentedTextWriter.WriteLine("return hc.ToHashCode();");
        }
        else
        {
            indentedTextWriter.Write("return HashCode.Combine(");
            using (var enumerator = fields.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    var field = enumerator.Current;

                    while (enumerator.MoveNext())
                    {
                        indentedTextWriter.Write($"{field!.Name}, ");
                        field = enumerator.Current;
                    }

                    indentedTextWriter.Write($"{field!.Name}");
                }
            }

            indentedTextWriter.WriteLine(");");
        }

        indentedTextWriter.Indent--;
        indentedTextWriter.WriteLine("}");
        indentedTextWriter.WriteLineNoTabs("");
    }

    private static Tree ReadTree(string inputFile)
    {
        var reader = XmlReader.Create(
            inputFile,
            new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit }
        );
        var serializer = new XmlSerializer(typeof(Tree));
        return (Tree)serializer.Deserialize(reader);
    }
}
