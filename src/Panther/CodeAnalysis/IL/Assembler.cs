using System.Collections.Immutable;
using Panther.CodeAnalysis.Syntax;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.IL;

/**
 * TODO:
 * - need to write instructions to an object file
 *   - object file should have segments
 *   - symbol map
 *   - relocation/address table
 *   - constant table?
 *   - data table?
 * - loader
 * - linker that uses relocation table
 * - We already have VMLoader in the vm-test branch with some of this logic
 *
 */

public class Assembler
{
    public SourceFile File { get; }
    public ImmutableArray<Diagnostic> Diagnostics { get; }
    public AssemblyListingSyntax Listing { get; }

    // private Listing? _listing = null;

    // private Listing TypedListing
    // {
    //     get
    //     {
    //         if (_listing == null)
    //         {
    //             var listing = new Listing(Listing.Instructions.Select(TypeInstruction).ToImmutableArray());
    //             Interlocked.CompareExchange(ref _listing, listing, null);
    //         }
    //
    //         return _listing!;
    //     }
    // }

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

    public void Emit(string outputPath) { }
}

record Label(int? Scope, string Name)
{
    public override string ToString() => Scope.HasValue ? $".{Name}" : $":{Name}";
}

abstract record Instruction
{
    // Arguments
    record Ldarg(int Index) : Instruction;

    record Starg(int Index) : Instruction;

    // Locals
    record Ldloc(int Index) : Instruction;

    record Stloc(int Index) : Instruction;

    // Fields
    record Ldfld(int Index) : Instruction;

    record Stfld(int Index) : Instruction;

    record Ldsfld(int Label) : Instruction;

    record Stsfld(int Label) : Instruction;

    // Constants
    record Ldc(int Value) : Instruction;

    record Ldstr(string Value) : Instruction;

    // Stack
    record Pop : Instruction;

    record Ret : Instruction;

    record Nop : Instruction;

    record Call(int Label, int Arguments) : Instruction;

    record Function(int Label, int Locals) : Instruction;

    // Arithmetic
    record Add : Instruction;

    record And : Instruction;

    record Div : Instruction;

    record Mul : Instruction;

    record Neg : Instruction;

    record Not : Instruction;

    record Or : Instruction;

    record Sub : Instruction;

    record Xor : Instruction;

    // Comparison
    record Ceq : Instruction;

    record Cgt : Instruction;

    record Clt : Instruction;

    // Branch
    record Br : Instruction;

    record Brfalse : Instruction;

    record Brtrue : Instruction;

    // Heap Allocation
    record New(int Slots) : Instruction;
}
