using System.Collections.Immutable;
using System.Linq;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Binding;

/// <summary>
/// Given a set of method symbols and a set of arguments determine the Cost to bind each method.
///
/// Once the methods costs are determined, remove all invalid costs/errors
/// Given the remaining costs, find the minimum cost
/// If there are more than one method that match the minimum cost then this results in an error
///
/// Cost is defined as the number of operations required to align our arguments with our
/// methods type.
/// </summary>
/// <typeparam name="TResult"></typeparam>
static class MethodBindCost
{
    private static MethodBindCostResult Analyze(
        int id,
        Symbol method,
        ImmutableArray<TypedExpression> arguments
    )
    {
        if (method.Type is not MethodType methodType)
            return new MethodBindCostResult.SymbolNot(id);

        var methodParamTypes = methodType.Parameters.Select(p => p.Type).ToImmutableArray();
        if (methodParamTypes.Length != arguments.Length)
            return new MethodBindCostResult.ParameterNumberMismatch(id);

        var conversions = arguments
            .Select(arg => arg.Type)
            .Zip(methodParamTypes)
            .Select(tup => Conversion.Classify(tup.First, tup.Second))
            .ToImmutableArray();
        var cost = 0;
        for (var index = 0; index < conversions.Length; index++)
        {
            var conversion = conversions[index];
            if (conversion.IsIdentity)
                continue;

            if (conversion.IsImplicit)
            {
                cost++;
                continue;
            }

            return new MethodBindCostResult.ParameterTypeMismatch(id, index, conversion.IsExplicit);
        }

        return new MethodBindCostResult.MethodBindCostValue(id, cost);
    }

    public static Symbol? Analyze(
        ImmutableArray<Symbol> methods,
        ImmutableArray<TypedExpression> arguments
    )
    {
        var (_, results) = methods
            .Select((method, index) => Analyze(index, method, arguments))
            .Partition(cost => cost is MethodBindCostResult.MethodBindCostError);

        if (results.IsEmpty)
        {
            // TODO: improve errors
            return null;
        }

        var costs = results
            .Select(cost => (MethodBindCostResult.MethodBindCostValue)cost)
            .ToImmutableArray();

        var min = costs.Select(cost => cost.Value).Min();

        var matches = costs.Where(cost => cost.Value == min).ToImmutableArray();

        if (matches.Length == 1)
        {
            return methods[matches[0].Id];
        }

        // ambiguous
        return null;
    }
}

abstract record MethodBindCostResult(int Id)
{
    internal abstract record MethodBindCostError(int Id) : MethodBindCostResult(Id);

    internal record MethodBindCostValue(int Id, int Value) : MethodBindCostResult(Id);

    internal record SymbolNot(int Id) : MethodBindCostError(Id);

    internal record ParameterNumberMismatch(int Id) : MethodBindCostError(Id);

    internal record ParameterTypeMismatch(int Id, int Index, bool ExplicitConversionAvailable)
        : MethodBindCostError(Id);
}
