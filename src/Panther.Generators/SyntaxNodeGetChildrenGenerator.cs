using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Panther.Generators
{
    [Generator]
    public class SyntaxNodeGetChildrenGenerator : ISourceGenerator
    {
        public void Initialize(InitializationContext context)
        {
        }

        public void Execute(SourceGeneratorContext context)
        {
            using var writer = new StringWriter();
            using var indentedTextWriter = new IndentedTextWriter(writer);

            indentedTextWriter.WriteLine("using System;");
            indentedTextWriter.WriteLine("using System.Collections.Generic;");
            indentedTextWriter.WriteLine();
            indentedTextWriter.WriteLine("namespace Panther.CodeAnalysis.Syntax");
            indentedTextWriter.WriteLine("{");
            indentedTextWriter.Indent++;


            var compilation = context.Compilation;

            var immutableArrayType = compilation.GetTypeByMetadataName("System.Collections.Immutable.ImmutableArray`1");
            var syntaxNodeType = compilation.GetTypeByMetadataName("Panther.CodeAnalysis.Syntax.SyntaxNode");
            var separatedSyntaxListType = compilation.GetTypeByMetadataName("Panther.CodeAnalysis.Syntax.SeparatedSyntaxList`1");
            var allTypes = GetAllTypes(compilation.Assembly);
            var syntaxNodeTypes = allTypes.Where(type => !type.IsAbstract && IsDerivedFrom(type, syntaxNodeType) && IsPartial(type));

            foreach (var syntaxNode in syntaxNodeTypes)
            {
                var name = syntaxNode.Name;
                indentedTextWriter.WriteLine($"partial class {name}");
                indentedTextWriter.WriteLine("{");
                indentedTextWriter.Indent++;
                var kind = name.EndsWith("Syntax") ? name.Substring(0, name.LastIndexOf("Syntax")) : name;

                indentedTextWriter.WriteLine($"public override SyntaxKind Kind => SyntaxKind.{kind};");
                writer.WriteLine();
                indentedTextWriter.WriteLine("public override IEnumerable<SyntaxNode> GetChildren()");
                indentedTextWriter.WriteLine("{");
                indentedTextWriter.Indent++;

                foreach (var propertySymbol in syntaxNode.GetMembers().OfType<IPropertySymbol>())
                {
                    if (!(propertySymbol.Type is INamedTypeSymbol propertyType)) continue;

                    var canBeNull = propertySymbol.NullableAnnotation == NullableAnnotation.Annotated;
                    if (canBeNull)
                    {
                        indentedTextWriter.WriteLine($"if ({propertySymbol.Name} != null)");
                        indentedTextWriter.WriteLine("{");
                        indentedTextWriter.Indent++;
                    }

                    if (IsDerivedFrom(propertyType, syntaxNodeType))
                    {
                        indentedTextWriter.WriteLine($"yield return {propertySymbol.Name};");
                    }
                    else if (propertyType.TypeArguments.Length == 1 &&
                             IsDerivedFrom(propertyType.TypeArguments[0], syntaxNodeType) &&
                             SymbolEqualityComparer.Default.Equals(propertyType.OriginalDefinition, immutableArrayType))
                    {

                        indentedTextWriter.WriteLine($"foreach (var child in {propertySymbol.Name})");
                        indentedTextWriter.Indent++;
                        indentedTextWriter.WriteLine($"yield return child;");
                        indentedTextWriter.Indent--;
                    }
                    else if (SymbolEqualityComparer.Default.Equals(propertyType.OriginalDefinition, separatedSyntaxListType) &&
                             IsDerivedFrom(propertyType.TypeArguments[0], syntaxNodeType))
                    {
                        indentedTextWriter.WriteLine($"foreach (var child in {propertySymbol.Name}.GetWithSeparators())");
                        indentedTextWriter.Indent++;
                        indentedTextWriter.WriteLine($"yield return child;");
                        indentedTextWriter.Indent--;
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


            // HACK the source generator doesnt completely work since we aren't on the appropriate .net version
            // and not all tooling supports source generators, so manually generate them for now
            var path = Path.Combine(Path.GetDirectoryName(compilation.SyntaxTrees.Select(tree => tree.FilePath).First()) ?? ".", "SyntaxNode_GetChildren.g.cs");
            File.WriteAllText(path, writer.ToString());

            // context.AddSource("SyntaxNode_GetChildren.g.cs", SourceText.From(writer.ToString(), Encoding.UTF8));
        }

        bool IsPartial(INamedTypeSymbol type)
        {
            var items = from syntaxReference in type.DeclaringSyntaxReferences
                        let syntax = syntaxReference.GetSyntax()
                        where syntax is TypeDeclarationSyntax
                        let typeDeclarationSyntax = (TypeDeclarationSyntax)syntax
                        from modifier in typeDeclarationSyntax.Modifiers
                        where modifier.ValueText == "partial"
                        select modifier;

            return items.Any();
        }

        bool IsDerivedFrom(ITypeSymbol type, INamedTypeSymbol baseType)
        {
            while (type.BaseType != null)
            {
                if (SymbolEqualityComparer.Default.Equals(type.BaseType, baseType))
                    return true;

                type = type.BaseType;
            }

            return false;
        }

        private List<INamedTypeSymbol> GetAllTypes(IAssemblySymbol compilationAssembly)
        {
            var items = new List<INamedTypeSymbol>();
            GetAllTypes(items, compilationAssembly.GlobalNamespace);
            items.Sort((x, y) => string.Compare(x.MetadataName, y.MetadataName, StringComparison.Ordinal));
            return items;
        }

        private void GetAllTypes(List<INamedTypeSymbol> items, INamespaceOrTypeSymbol symbol)
        {
            if (symbol is INamedTypeSymbol typeSymbol)
                items.Add(typeSymbol);

            foreach (var member in symbol.GetMembers())
            {
                if (member is INamespaceOrTypeSymbol child)
                    GetAllTypes(items, child);
            }
        }
    }
}