using System.Collections.Generic;
using System.Linq;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis
{
    public class Compilation
    {
        public SyntaxTree Syntax { get; }

        public Compilation(SyntaxTree syntax)
        {
            Syntax = syntax;
        }

        public EvaluationResult Evaluate(Dictionary<VariableSymbol, object> variables)
        {
            var binder = new Binder(variables);
            var boundExpression = binder.BindExpression(Syntax.Root);
            var evaluator = new Evaluator(boundExpression, variables);
            var value = evaluator.Evaluate();

            var diagnostics = Syntax.Diagnostics.Concat(binder.Diagnostics).ToArray();

            if (diagnostics.Any())
            {
                return new EvaluationResult(diagnostics, null);
            }

            return new EvaluationResult(Enumerable.Empty<Diagnostic>(), value);
        }
    }
}