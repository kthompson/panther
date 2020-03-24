using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis
{
    public class Compilation
    {
        public Compilation Previous { get; }
        public SyntaxTree SyntaxTree { get; }
        private BoundGlobalScope _globalScope;

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

        public Compilation(SyntaxTree syntaxTree)
            : this(null, syntaxTree)
        {
        }

        private Compilation(Compilation previous, SyntaxTree syntaxTree)
        {
            Previous = previous;
            SyntaxTree = syntaxTree;
        }

        public Compilation ContinueWith(SyntaxTree syntaxTree) => new Compilation(this, syntaxTree);

        public EvaluationResult Evaluate(Dictionary<VariableSymbol, object> variables)
        {
            var globalScope = GlobalScope;
            var diagnostics = SyntaxTree.Diagnostics.Concat(globalScope.Diagnostics).ToArray();

            if (diagnostics.Any())
            {
                return new EvaluationResult(diagnostics, null);
            }

            var evaluator = new Evaluator(globalScope.Expression, variables);
            var value = evaluator.Evaluate();

            return new EvaluationResult(Enumerable.Empty<Diagnostic>(), value);
        }
    }
}