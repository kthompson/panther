using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
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
        private readonly IBuiltins _builtins;
        public bool IsScript { get; }
        public Compilation? Previous { get; }
        public ImmutableArray<SyntaxTree> SyntaxTrees { get; }
        public FunctionSymbol? MainFunction => GlobalScope.MainFunction;
        public ImmutableArray<FunctionSymbol> Functions => GlobalScope.Functions;
        public ImmutableArray<VariableSymbol> Variables => GlobalScope.Variables;

        private BoundGlobalScope? _globalScope;

        internal BoundGlobalScope GlobalScope
        {
            get
            {
                if (_globalScope == null)
                {
                    var globalScope = Binder.BindGlobalScope(IsScript, Previous?.GlobalScope, SyntaxTrees);
                    Interlocked.CompareExchange(ref _globalScope, globalScope, null);
                }

                return _globalScope;
            }
        }

        private Compilation(bool isScript, Compilation? previous, IBuiltins? builtins, params SyntaxTree[] syntaxTrees)
        {
            _builtins = builtins ?? Builtins.Default;
            IsScript = isScript;
            Previous = previous;
            SyntaxTrees = syntaxTrees.ToImmutableArray();
        }

        public static Compilation Create(params SyntaxTree[] syntaxTrees) =>
            Create(null, syntaxTrees);

        public static Compilation Create(IBuiltins? builtins, params SyntaxTree[] syntaxTrees) =>
            new Compilation(false, null, null, syntaxTrees);

        public static Compilation CreateScript(Compilation? previous, params SyntaxTree[] syntaxTrees) =>
            CreateScript(previous, previous?._builtins, syntaxTrees);

        public static Compilation CreateScript(Compilation? previous, IBuiltins? builtins, params SyntaxTree[] syntaxTrees) =>
            new Compilation(isScript: true, previous, builtins, syntaxTrees);

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

        public EvaluationResult Evaluate(Dictionary<VariableSymbol, object> variables)
        {
            var globalScope = GlobalScope;
            var syntaxDiags =
                from tree in SyntaxTrees
                from diag in tree.Diagnostics
                select diag;

            var diagnostics = syntaxDiags.Concat(globalScope.Diagnostics).ToImmutableArray();

            if (diagnostics.Any())
            {
                return new EvaluationResult(diagnostics, null);
            }

            var program = GetProgram();

            // var appPath = Environment.GetCommandLineArgs()[0];
            // var appDirectory = Path.GetDirectoryName(appPath);
            // var cfgPath = Path.Combine(appDirectory, "cfg.dot");
            // var cfgExpression = !program.Expression.Statements.Any() && program.Functions.Any()
            //     ? program.Functions.Last().Value
            //     : program.Expression;
            // var cfg = ControlFlowGraph.Create(cfgExpression);
            // using var stream = new StreamWriter(cfgPath);
            // cfg.WriteTo(stream);

            if (program.Diagnostics.Any())
                return new EvaluationResult(program.Diagnostics.ToImmutableArray(), null);

            var evaluator = new Evaluator(program, variables, _builtins);
            var value = evaluator.Evaluate();

            return new EvaluationResult(ImmutableArray<Diagnostic>.Empty, value);
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

        public void EmitTree(FunctionSymbol function, TextWriter writer)
        {
            BoundProgram? program = GetProgram();

            while (program != null)
            {
                if (program.Functions.TryGetValue(function, out var block))
                {

                    function.WriteTo(writer);
                    writer.WritePunctuation(" = ");
                    writer.WriteLine();
                    block.WriteTo(writer);

                    return;
                }

                program = program.Previous;
            }

            function.WriteTo(writer);

        }

        public ImmutableArray<Diagnostic> Emit(string moduleName, string[] references, string outputPath)
        {
            var program = GetProgram();
            return Emitter.Emit(program, moduleName, references, outputPath);
        }
    }
}