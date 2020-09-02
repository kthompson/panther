using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Mono.Cecil;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Emit;
using Panther.CodeAnalysis.Lowering;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;
using Panther.IO;

namespace Panther.CodeAnalysis
{
    public class Compilation
    {
        public bool IsScript { get; }
        public Compilation? Previous { get; }
        public ImmutableArray<SyntaxTree> SyntaxTrees { get; }
        public ImmutableArray<AssemblyDefinition> References { get; }
        public ImmutableArray<TypeSymbol> Types => BoundAssembly.Types.OfType<TypeSymbol>().ToImmutableArray();

        private BoundGlobalScope? _globalScope;

        private BoundGlobalScope GlobalScope
        {
            get
            {
                if (_globalScope == null)
                {
                    var globalScope = Binder.BindGlobalScope(IsScript, Previous?.GlobalScope, SyntaxTrees, References);
                    Interlocked.CompareExchange(ref _globalScope, globalScope, null);
                }

                return _globalScope;
            }
        }

        private BoundAssembly? _boundAssembly;
        private BoundAssembly BoundAssembly
        {
            get
            {
                if (_boundAssembly == null)
                {
                    var previous = this.Previous?.BoundAssembly;
                    var bindAssembly = Binder.BindAssembly(IsScript, previous, GlobalScope);
                    Interlocked.CompareExchange(ref _boundAssembly, bindAssembly, null);
                }

                return _boundAssembly;
            }
        }

        private Compilation(ImmutableArray<AssemblyDefinition> references, bool isScript, Compilation? previous, params SyntaxTree[] syntaxTrees)
        {
            References = references;
            IsScript = isScript;
            Previous = previous;
            SyntaxTrees = syntaxTrees.ToImmutableArray();
        }

        public static (ImmutableArray<Diagnostic> diagnostics, Compilation? compilation) Create(string[] references, params SyntaxTree[] syntaxTrees)
        {
            var bag = new DiagnosticBag();
            var assemblies = ImmutableArray.CreateBuilder<AssemblyDefinition>();
            foreach (var reference in references)
            {
                try
                {
                    var asm = AssemblyDefinition.ReadAssembly(reference);
                    assemblies.Add(asm);
                }
                catch (BadImageFormatException)
                {
                    bag.ReportInvalidReference(reference);
                }
            }

            if (bag.Any())
            {
                return (bag.ToImmutableArray(), null);
            }

            return (ImmutableArray<Diagnostic>.Empty, Create(assemblies.ToImmutable(), syntaxTrees));
        }

        public static Compilation Create(ImmutableArray<AssemblyDefinition> references, params SyntaxTree[] syntaxTrees) =>
            new Compilation(references, false, null, syntaxTrees);

        public static Compilation CreateScript(ImmutableArray<AssemblyDefinition> references, Compilation? previous, params SyntaxTree[] syntaxTrees) =>
            new Compilation(references, isScript: true, previous, syntaxTrees);

        public IEnumerable<Symbol> GetSymbols()
        {
            Compilation? compilation = this;
            var symbolNames = new HashSet<string>();

            while (compilation != null)
            {
                foreach (var type in compilation.Types.Where(type => symbolNames.Add(type.Name)))
                {
                    yield return type;

                    foreach (var member in type.GetMembers())
                    {
                        yield return member;
                    }
                }

                compilation = compilation.Previous;
            }
        }

        public void EmitTree(TextWriter writer)
        {
            var entryPoint = GlobalScope.EntryPoint?.Symbol;
            foreach (var type in GlobalScope.Types)
            {
                // TODO: print type symbol
                foreach (var function in type.MethodDefinitions)
                {
                    // emit entry point last
                    if (entryPoint == function.Key)
                        continue;

                    EmitTree(function.Key, function.Value, writer);
                    writer.WriteLine();
                }
            }


            if (entryPoint != null)
            {
                EmitTree(entryPoint, writer);
                writer.WriteLine();
            }
        }

        public void EmitTree(MethodSymbol method, TextWriter writer)
        {
            BoundAssembly? assembly = BoundAssembly;

            while (assembly != null)
            {
                foreach (var boundType in assembly.Types)
                {
                    if (boundType.MethodDefinitions.TryGetValue(method, out var block))
                    {
                        EmitTree(method, block, writer);

                        return;
                    }
                }

                assembly = assembly.Previous;
            }

            method.WriteTo(writer);
        }

        private static void EmitTree(MethodSymbol method, BoundBlockExpression block, TextWriter writer)
        {
            method.WriteTo(writer);
            writer.WritePunctuation(" = ");
            writer.WriteLine();
            block.WriteTo(writer);
        }

        public EmitResult Emit(string moduleName, string outputPath) =>
            Emitter.Emit(BoundAssembly, moduleName, outputPath);

        internal EmitResult Emit(string moduleName, string outputPath, Dictionary<FieldSymbol, FieldReference> previousGlobals, Dictionary<MethodSymbol, MethodReference> previousMethods) =>
            Emitter.Emit(BoundAssembly, moduleName, outputPath, previousGlobals, previousMethods);
    }
}