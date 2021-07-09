using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace Panther.SyntaxGen
{

    [XmlRoot]
    public class Tree
    {
        [XmlAttribute]
        public string Root;

        [XmlElement(ElementName = "Node", Type = typeof(Node))]
        [XmlElement(ElementName = "AbstractNode", Type = typeof(AbstractNode))]
        [XmlElement(ElementName = "PredefinedNode", Type = typeof(PredefinedNode))]
        public List<TreeType> Types;
    }

    public class Node : TreeType
    {
        [XmlAttribute]
        public string Root;

        [XmlAttribute]
        public string Errors;

        [XmlElement(ElementName = "Kind", Type = typeof(Kind))]
        public List<Kind> Kinds = new List<Kind>();

        public IEnumerable<Field> Fields => this.Children.OfType<Field>();
    }

    public class TreeType
    {
        [XmlAttribute]
        public string Name;

        [XmlAttribute]
        public string Base;

        [XmlAttribute]
        public string SkipConvenienceFactories;

        [XmlElement(ElementName = "Field", Type = typeof(Field))]
        public List<TreeTypeChild> Children = new List<TreeTypeChild>();
    }

    public class PredefinedNode : TreeType
    {
    }

    public class AbstractNode : TreeType
    {
        public readonly List<Field> Fields = new List<Field>();
    }

    public class TreeTypeChild
    {
    }

    public class Field : TreeTypeChild
    {
        [XmlAttribute]
        public string Name;

        [XmlAttribute]
        public string Type;

        [XmlAttribute]
        public string Optional;

        [XmlAttribute]
        public string Override;

        [XmlAttribute]
        public string New;

        [XmlAttribute]
        public int MinCount;

        [XmlAttribute]
        public bool AllowTrailingSeparator;

        [XmlElement(ElementName = "Kind", Type = typeof(Kind))]
        public List<Kind> Kinds = new List<Kind>();

        public bool IsToken => Type == "SyntaxToken";
        public bool IsOptional => Optional == "true";
    }

    public class Kind
    {
        [XmlAttribute]
        public string Name;

        public override bool Equals(object obj)
            => obj is Kind kind &&
               Name == kind.Name;

        public override int GetHashCode()
            => Name.GetHashCode();
    }


    class Program
    {
        private static Dictionary<string, TreeType> _typeLookup;
        private static Tree _tree;

        static void Main(string[] args)
        {
            _tree = ReadTree("Syntax.xml");
            _typeLookup = _tree.Types.ToDictionary(t => t.Name);

            using var writer = new StringWriter();
            using var indentedTextWriter = new IndentedTextWriter(writer);

            indentedTextWriter.WriteLine("using System;");
            indentedTextWriter.WriteLine("using System.Collections.Generic;");
            indentedTextWriter.WriteLine("using System.Collections.Immutable;");
            indentedTextWriter.WriteLine();
            indentedTextWriter.WriteLine("#nullable enable");
            indentedTextWriter.WriteLine();
            indentedTextWriter.WriteLine("namespace Panther.CodeAnalysis.Syntax");
            indentedTextWriter.WriteLine("{");
            indentedTextWriter.Indent++;

            foreach (var node in _tree.Types.OfType<AbstractNode>())
            {
                var name = node.Name;
                indentedTextWriter.Write($"public abstract partial record {name}(SyntaxTree SyntaxTree");

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
                indentedTextWriter.Write("(SyntaxTree);");
                indentedTextWriter.Indent--;
                indentedTextWriter.WriteLine();
                writer.WriteLine();
            }

            foreach (var node in _tree.Types.OfType<Node>())
            {
                var name = node.Name;
                indentedTextWriter.Write($"public sealed partial record {name}(SyntaxTree SyntaxTree");

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
                indentedTextWriter.WriteLine("(SyntaxTree) {");

                var kind = node.Kinds.FirstOrDefault();
                if (kind != null)
                {
                    indentedTextWriter.WriteLine($"public override SyntaxKind Kind => SyntaxKind.{kind.Name};");
                    indentedTextWriter.WriteLine("");
                }

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
                        writer.WriteLine();
                    }
                    else if (field.Type.StartsWith("SeparatedSyntaxList<"))
                    {
                        indentedTextWriter.WriteLine($"foreach (var child in {field.Name}.GetWithSeparators())");
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

                indentedTextWriter.Indent--;
                indentedTextWriter.WriteLine("}");
                writer.WriteLine();
            }

            indentedTextWriter.Indent--;
            indentedTextWriter.WriteLine("}");

            var path = Path.Combine("..", "..", "..", "..", "Panther", "CodeAnalysis", "Syntax", "Syntax.g.cs");
            File.WriteAllText(path, writer.ToString());

            Console.WriteLine($"Wrote syntax to {path}");
        }


        private static Tree ReadTree(string inputFile)
        {
            var reader = XmlReader.Create(inputFile, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit });
            var serializer = new XmlSerializer(typeof(Tree));
            return (Tree)serializer.Deserialize(reader);
        }
    }
}