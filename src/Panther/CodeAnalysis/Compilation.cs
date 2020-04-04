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
        public SyntaxTree SyntaxTree { get; }
        private BoundGlobalScope? _globalScope;

        internal BoundGlobalScope GlobalScope
        {
            get
            {
                if (_globalScope == null)
                {
                    var globalScope = Binder.BindGlobalScope(Previous?.GlobalScope, SyntaxTree.Root);
                    Interlocked.CompareExchange(ref _globalScope, globalScope, null);
                }

                return _globalScope;
            }
        }

        public Compilation(SyntaxTree syntaxTree, IBuiltins builtins)
            : this(null, syntaxTree, builtins)
        {
        }

        private Compilation(Compilation? previous, SyntaxTree syntaxTree, IBuiltins builtins)
        {
            _builtins = builtins;
            Previous = previous;
            SyntaxTree = syntaxTree;
        }

        public Compilation ContinueWith(SyntaxTree syntaxTree) => new Compilation(this, syntaxTree, this._builtins);

        public EvaluationResult Evaluate(Dictionary<VariableSymbol, object> variables)
        {
            var globalScope = GlobalScope;
            var diagnostics = SyntaxTree.Diagnostics.Concat(globalScope.Diagnostics).ToImmutableArray();

            if (diagnostics.Any())
            {
                return new EvaluationResult(diagnostics, null);
            }

            var program = Binder.BindProgram(GlobalScope);

            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NCRUNCH")))
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