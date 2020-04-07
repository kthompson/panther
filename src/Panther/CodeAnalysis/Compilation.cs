using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Lowering;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis
{
    public class Compilation
    {
        private readonly IBuiltins _builtins;
        public Compilation? Previous { get; }
        public ImmutableArray<SyntaxTree> SyntaxTrees { get; }
        public ImmutableArray<FunctionSymbol> Functions => GlobalScope.Functions;
        public ImmutableArray<VariableSymbol> Variables => GlobalScope.Variables;

        private BoundGlobalScope? _globalScope;

        internal BoundGlobalScope GlobalScope
        {
            get
            {
                if (_globalScope == null)
                {
                    var globalScope = Binder.BindGlobalScope(Previous?.GlobalScope, SyntaxTrees);
                    Interlocked.CompareExchange(ref _globalScope, globalScope, null);
                }

                return _globalScope;
            }
        }

        public Compilation(params SyntaxTree[] syntaxTree)
            : this((IBuiltins?)null, syntaxTree)
        {
        }

        public Compilation(IBuiltins? builtins, params SyntaxTree[] syntaxTree)
            : this(null, builtins, syntaxTree)
        {
        }

        private Compilation(Compilation? previous, params SyntaxTree[] syntaxTrees)
            : this(previous, null, syntaxTrees)
        {
        }

        private Compilation(Compilation? previous, IBuiltins? builtins, params SyntaxTree[] syntaxTrees)
        {
            _builtins = builtins ?? Builtins.Default;
            Previous = previous;
            SyntaxTrees = syntaxTrees.ToImmutableArray();
        }

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

        public Compilation ContinueWith(SyntaxTree syntaxTree) => new Compilation(this, this._builtins, syntaxTree);

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

            var program = Binder.BindProgram(GlobalScope);

            // temp hack for CI/NCRUNCH to not break
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NCRUNCH") ?? Environment.GetEnvironmentVariable("BUILD_SERVER")))
            {
                var appPath = Environment.GetCommandLineArgs()[0];
                var appDirectory = Path.GetDirectoryName(appPath);
                var cfgPath = Path.Combine(appDirectory, "cfg.dot");
                var cfgExpression = !program.Expression.Statements.Any() && program.Functions.Any()
                    ? program.Functions.Last().Value
                    : program.Expression;
                var cfg = ControlFlowGraph.Create(cfgExpression);
                using var stream = new StreamWriter(cfgPath);
                cfg.WriteTo(stream);
            }

            if (program.Diagnostics.Any())
                return new EvaluationResult(program.Diagnostics.ToImmutableArray(), null);

            var evaluator = new Evaluator(program, variables, _builtins);
            var value = evaluator.Evaluate();

            return new EvaluationResult(ImmutableArray<Diagnostic>.Empty, value);
        }

        public void EmitTree(TextWriter writer)
        {
            var program = Binder.BindProgram(GlobalScope);

            if (program.Expression.Statements.Any() || program.Expression.Expression != BoundUnitExpression.Default)
            {
                program.Expression.WriteTo(writer);
            }
            else
            {
                foreach (var functionBody in program.Functions)
                {
                    if (!GlobalScope.Functions.Contains(functionBody.Key))
                        continue;

                    functionBody.Key.WriteTo(writer);
                    functionBody.Value.WriteTo(writer);
                }
            }
        }
    }
}