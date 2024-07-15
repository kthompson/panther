using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FsCheck;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;
using Panther.CodeAnalysis.Text;
using Panther.CodeAnalysis.Typing;
using Type = Panther.CodeAnalysis.Symbols.Type;

#pragma warning disable 612

namespace Panther.Tests.CodeAnalysis.Lowering;

class BindingGenerators
{
    public static readonly Gen<Type> TypeGen = Gen.OneOf(
        Gen.Constant(Panther.CodeAnalysis.Symbols.Type.Bool),
        Gen.Constant(Panther.CodeAnalysis.Symbols.Type.Int),
        Gen.Constant(Panther.CodeAnalysis.Symbols.Type.String),
        Gen.Constant(Panther.CodeAnalysis.Symbols.Type.Unit)
    );

    private static readonly Symbol TypeSymbol;
    private static readonly Symbol MainSymbol;

    static BindingGenerators()
    {
        TypeSymbol = Symbol.NewRoot().NewClass(TextLocation.None, "Program").Declare();
        MainSymbol = TypeSymbol.NewMethod(TextLocation.None, "Main").Declare();
    }

    public static Arbitrary<SyntaxNode> SyntaxNode() =>
        Gen.Constant((SyntaxNode)new SyntaxToken(null!, SyntaxKind.InvalidTokenTrivia, 0))
            .ToArbitrary();

    public static Arbitrary<TypedStatement> TypedStatement() =>
        Gen.OneOf(
                Arb.Generate<TypedConditionalGotoStatement>().Select(x => (TypedStatement)x),
                Arb.Generate<TypedExpressionStatement>().Select(x => (TypedStatement)x),
                Arb.Generate<TypedGotoStatement>().Select(x => (TypedStatement)x),
                Arb.Generate<TypedLabelStatement>().Select(x => (TypedStatement)x),
                Arb.Generate<TypedVariableDeclarationStatement>().Select(x => (TypedStatement)x)
            )
            .ToArbitrary();

    public static Arbitrary<TypedGotoStatement> TypedGotoStatement() =>
        (
            from token in Arb.Generate<SyntaxNode>()
            from label in Arb.Generate<TypedLabel>()
            select new TypedGotoStatement(token, label)
        ).ToArbitrary();

    public static Arbitrary<TypedConditionalGotoStatement> TypedConditionalGotoStatement() =>
        (
            from token in Arb.Generate<SyntaxNode>()
            from label in Arb.Generate<TypedLabel>()
            from jumpIfTrue in Arb.Generate<bool>()
            from expr in GenTypedExpression(Panther.CodeAnalysis.Symbols.Type.Bool)
            select new TypedConditionalGotoStatement(token, label, expr, jumpIfTrue)
        ).ToArbitrary();

    public static Arbitrary<TypedExpressionStatement> TypedExpressionStatement() =>
        (
            from token in Arb.Generate<SyntaxNode>()
            from type in Arb.Generate<Type>()
            from expr in GenTypedExpression(type)
            select new TypedExpressionStatement(token, expr)
        ).ToArbitrary();

    public static Arbitrary<TypedVariableDeclarationStatement> TypedVariableDeclarationStatement() =>
        (
            from token in Arb.Generate<SyntaxNode>()
            from type in Arb.Generate<Type>()
            from variable in GenLocalVariableSymbol(type)
            from expr in GenTypedExpression(type)
            select new TypedVariableDeclarationStatement(token, variable, expr)
        ).ToArbitrary();

    public static Arbitrary<Type> Type => TypeGen.ToArbitrary();

    public static Arbitrary<TypedUnitExpression> TypedUnitExpression =>
        Gen.Constant(new TypedUnitExpression(null!)).ToArbitrary();

    public static Gen<string> Identifier() =>
        from count in Gen.Choose(5, 10)
        from chars in Gen.ArrayOf(count, Gen.Elements<char>("abcdefghijklmnopqrstuvwxyz"))
        select new string(chars);

    private static HashSet<string> _identifiers = new HashSet<string>();

    private static bool IsUnique(string ident) => _identifiers.Add(ident);

    public static Arbitrary<TypedLabel> TypedLabel() =>
        Identifier().Where(IsUnique).Select(x => new TypedLabel(x)).ToArbitrary();

    public static Arbitrary<TypedLabelStatement> TypedLabelStatement() =>
        (
            from token in Arb.Generate<SyntaxNode>()
            from x in Arb.Generate<TypedLabel>()
            select new TypedLabelStatement(token, x)
        ).ToArbitrary();

    public static Gen<TypedIfExpression> GenIfExpression(Type type) =>
        from condition in GenTypedExpression(Panther.CodeAnalysis.Symbols.Type.Bool)
        from thenExpr in GenTypedExpression(type)
        from elseExpr in GenTypedExpression(type)
        select new TypedIfExpression(null!, condition, thenExpr, elseExpr);

    public static Gen<TypedExpression> GenTypedExpressionLHS(Type type) =>
        from token in Arb.Generate<SyntaxNode>()
        from variable in GenLocalVariableSymbol(type)
        select (TypedExpression)new TypedVariableExpression(token, variable);

    public static Gen<TypedAssignmentExpression> GenTypedAssignmentExpression(Type type) =>
        from token in Arb.Generate<SyntaxNode>()
        from lhs in GenTypedExpressionLHS(type)
        from initializer in GenTypedExpression(type)
        select new TypedAssignmentExpression(token, lhs, initializer);

    public static Gen<TypedBlockExpression> GenTypedBlockExpression(Type type) =>
        from token in Arb.Generate<SyntaxNode>()
        from statements in Arb.Generate<ImmutableArray<TypedStatement>>()
        from expr in GenTypedExpression(type)
        select new TypedBlockExpression(token, statements, expr);

    public static Gen<(string ident, Type type)> GenNameAndType() =>
        from ident in Identifier()
        from type in TypeGen
        select (ident, type);

    public static Gen<int> GenParamSymbols(Symbol method) =>
        from i in Gen.Choose(0, 5)
        from idents in Gen.ArrayOf(i, GenNameAndType())
        let parameters = idents
            .Select(
                (param, n) =>
                    method
                        .NewParameter(TextLocation.None, param.ident, n)
                        .WithType(param.type)
                        .Declare()
            )
            .ToList()
        select i;

    public static Gen<Symbol> GenFunctionSymbol(Type returnType) =>
        from name in Identifier()
        let method = TypeSymbol.NewMethod(TextLocation.None, name).WithType(returnType).Declare()
        from _ in GenParamSymbols(method)
        select method;

    public static Gen<Symbol> GenLocalVariableSymbol(Type type) =>
        from ident in Identifier()
        from readOnly in Arb.Generate<bool>()
        select MainSymbol.NewLocal(TextLocation.None, ident, readOnly).WithType(type).Declare();

    public static Gen<TypedExpression> GenTypedLiteralExpression(Type typeSymbol)
    {
        Gen<TypedExpression> UnitGen() =>
            from token in Arb.Generate<SyntaxNode>()
            select (TypedExpression)new TypedUnitExpression(token!);

        Gen<TypedExpression> StringGen() =>
            from token in Arb.Generate<SyntaxNode>()
            from x in Arb.Generate<NonNull<string>>()
            select (TypedExpression)new TypedLiteralExpression(token, x.Item);

        Gen<TypedExpression> IntGen() =>
            from token in Arb.Generate<SyntaxNode>()
            from x in Arb.Generate<int>()
            select (TypedExpression)new TypedLiteralExpression(token, x);

        Gen<TypedExpression> BoolGen() =>
            from token in Arb.Generate<SyntaxNode>()
            from x in Arb.Generate<bool>()
            select (TypedExpression)new TypedLiteralExpression(token, x);

        if (typeSymbol == Panther.CodeAnalysis.Symbols.Type.Bool)
            return BoolGen();

        if (typeSymbol == Panther.CodeAnalysis.Symbols.Type.Int)
            return IntGen();

        if (typeSymbol == Panther.CodeAnalysis.Symbols.Type.String)
            return StringGen();

        if (typeSymbol == Panther.CodeAnalysis.Symbols.Type.Unit)
            return UnitGen();

        if (typeSymbol == Panther.CodeAnalysis.Symbols.Type.Any)
            return Gen.OneOf(BoolGen(), IntGen(), StringGen(), UnitGen());

        throw new ArgumentOutOfRangeException(nameof(typeSymbol));
    }

    public static Arbitrary<ImmutableArray<A>> ImmutableArray<A>()
    {
        return Gen.Sized(size =>
                from n in Gen.Choose(0, size)
                from array in Gen.ArrayOf(n, Gen.Resize(n == 0 ? 0 : size / n, Arb.Generate<A>()))
                select System.Collections.Immutable.ImmutableArray.Create(array)
            )
            .ToArbitrary();
    }

    public static Gen<TypedCallExpression> GenTypedCallExpression(Type type) =>
        from token in Arb.Generate<SyntaxNode>()
        from function in GenFunctionSymbol(type)
        from args in Gen.Sequence(function.Parameters.Select(p => GenTypedExpression(p.Type)))
        select new TypedCallExpression(token, function, null, args.ToImmutableArray());

    public static Gen<TypedExpression> GenTypedExpression(Type type)
    {
        return Gen.Sized(size =>
        {
            if (size <= 1)
            {
                return GenTypedLiteralExpression(type);
            }

            if (type == Panther.CodeAnalysis.Symbols.Type.Unit)
            {
                return Gen.Resize(
                    size / 2,
                    Gen.OneOf(
                        // Arb.Generate<TypedVariableExpression>().Select(x => (TypedExpression) x),
                        // Arb.Generate<TypedForExpression>().Select(x => (TypedExpression) x),
                        // Arb.Generate<TypedWhileExpression>().Select(x => (TypedExpression) x)

                        GenIfExpression(type).Select(x => (TypedExpression)x),
                        GenTypedAssignmentExpression(type).Select(x => (TypedExpression)x),
                        GenTypedBlockExpression(type).Select(x => (TypedExpression)x),
                        GenTypedCallExpression(type).Select(x => (TypedExpression)x),
                        GenTypedLiteralExpression(type)
                    )
                );
            }

            return Gen.Resize(
                size / 2,
                Gen.OneOf(
                    // Arb.Generate<TypedBinaryExpression>().Select(x => (TypedExpression) x),
                    // Arb.Generate<TypedConversionExpression>().Select(x => (TypedExpression) x),
                    // Arb.Generate<TypedUnaryExpression>().Select(x => (TypedExpression) x),
                    // Arb.Generate<TypedVariableExpression>().Select(x => (TypedExpression) x),

                    GenIfExpression(type).Select(x => (TypedExpression)x),
                    GenTypedBlockExpression(type).Select(x => (TypedExpression)x),
                    GenTypedCallExpression(type).Select(x => (TypedExpression)x),
                    GenTypedLiteralExpression(type)
                )
            );
        });
    }
}
