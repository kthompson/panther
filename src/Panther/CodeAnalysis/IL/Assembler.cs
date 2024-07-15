using System.Collections.Immutable;
using Panther.CodeAnalysis.Syntax;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.IL;

public class Assembler
{
    public SourceFile File { get; }
    public ImmutableArray<Diagnostic> Diagnostics { get; }
    public AssemblerListing Listing { get; }

    private Assembler(SourceFile file)
    {
        File = file;
        var parser = new AssemblyParser(file);

        Listing = parser.ParseListing();
        Diagnostics = parser.Diagnostics.ToImmutableArray();
    }

    public static Assembler ParseFile(string fileName)
    {
        var text = System.IO.File.ReadAllText(fileName);
        var sourceText = SourceFile.From(text, fileName);
        return Parse(sourceText);
    }

    public static Assembler Parse(SourceFile source) => new Assembler(source);

    public static Assembler ParseText(string source) => Parse(SourceFile.From(source));
}