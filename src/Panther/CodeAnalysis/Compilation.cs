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
        public MethodSymbol? MainFunction => GlobalScope.MainFunction;
        public ImmutableArray<MethodSymbol> Functions => GlobalScope.Functions;
        public ImmutableArray<VariableSymbol> Variables => GlobalScope.Variables;

        private BoundGlobalScope? _globalScope;

        internal BoundGlobalScope GlobalScope
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
                foreach (var function in compilation.Functions.Where(function => symbolNames.Add(function.Name)))
                    yield return function;

                foreach (var variable in compilation.Variables.Where(variable => symbolNames.Add(variable.Name)))
                    yield return variable;

                compilation = compilation.Previous;
            }
        }

        private BoundProgram GetProgram()
        {
            var previous = this.Previous?.GetProgram();
            return Binder.BindProgram(IsScript, previous, GlobalScope);
        }

        public void EmitTree(TextWriter writer)
        {
            var mainFunction = GlobalScope.MainFunction;
            var scriptFunction = GlobalScope.ScriptFunction;
            foreach (var function in GlobalScope.Functions)
            {
                var isMain = mainFunction == function;
                var isScript = scriptFunction == function;

                EmitTree(function, writer);
                writer.WriteLine();
            }

            if (mainFunction != null)
            {
                EmitTree(mainFunction, writer);
                writer.WriteLine();
            }
            else if (scriptFunction != null)
            {
                EmitTree(scriptFunction, writer);
                writer.WriteLine();
            }
        }

        public void EmitTree(MethodSymbol method, TextWriter writer)
        {
            BoundProgram? program = GetProgram();

            while (program != null)
            {
                if (program.Functions.TryGetValue(method, out var block))
                {

                    method.WriteTo(writer);
                    writer.WritePunctuation(" = ");
                    writer.WriteLine();
                    block.WriteTo(writer);

                    return;
                }

                program = program.Previous;
            }

            method.WriteTo(writer);
        }

        public EmitResult Emit(string moduleName, string outputPath)
        {
            var program = GetProgram();
            return Emitter.Emit(program, moduleName, outputPath);
        }

        internal EmitResult Emit(string moduleName, string outputPath, Dictionary<GlobalVariableSymbol, FieldReference> previousGlobals, Dictionary<MethodSymbol, MethodReference> previousMethods)
        {
            var program = GetProgram();
            return Emitter.Emit(program, moduleName, outputPath, previousGlobals, previousMethods);
        }
    }
}