﻿using System;
using System.Collections.Immutable;
using System.Linq;
using FsCheck;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Symbols;

namespace Panther.Tests.CodeAnalysis.Lowering
{
    class BindingGenerators
    {
        public static readonly Gen<TypeSymbol> TypeSymbolGen = Gen.OneOf(
            Gen.Constant(Panther.CodeAnalysis.Symbols.TypeSymbol.Bool),
            Gen.Constant(Panther.CodeAnalysis.Symbols.TypeSymbol.Int),
            Gen.Constant(Panther.CodeAnalysis.Symbols.TypeSymbol.String),
            Gen.Constant(Panther.CodeAnalysis.Symbols.TypeSymbol.Unit)
        );

        public static Arbitrary<BoundStatement> BoundStatement() =>
            Gen.OneOf(
                Arb.Generate<BoundConditionalGotoStatement>().Select(x => (BoundStatement)x),
                Arb.Generate<BoundExpressionStatement>().Select(x => (BoundStatement)x),
                Arb.Generate<BoundGotoStatement>().Select(x => (BoundStatement)x),
                Arb.Generate<BoundLabelStatement>().Select(x => (BoundStatement)x),
                Arb.Generate<BoundVariableDeclarationStatement>().Select(x => (BoundStatement)x)
            ).ToArbitrary();

        public static Arbitrary<BoundConditionalGotoStatement> BoundConditionalGotoStatement() =>
        (
            from label in Arb.Generate<BoundLabel>()
            from jumpIfTrue in Arb.Generate<bool>()
            from expr in GenBoundExpression(Panther.CodeAnalysis.Symbols.TypeSymbol.Bool)
            select new BoundConditionalGotoStatement(label, expr, jumpIfTrue)
        ).ToArbitrary();

        public static Arbitrary<BoundExpressionStatement> BoundExpressionStatement() =>
        (
            from type in Arb.Generate<TypeSymbol>()
            from expr in GenBoundExpression(type)
            select new BoundExpressionStatement(expr)
        ).ToArbitrary();

        public static Arbitrary<BoundVariableDeclarationStatement> BoundVariableDeclarationStatement() =>
        (
            from type in Arb.Generate<TypeSymbol>()
            from variable in GenLocalVariableSymbol(type)
            from expr in GenBoundExpression(type)
            select new BoundVariableDeclarationStatement(variable, expr)
        ).ToArbitrary();

        public static Arbitrary<TypeSymbol> TypeSymbol => TypeSymbolGen.ToArbitrary();

        public static Arbitrary<BoundUnitExpression> BoundUnitExpression =>
            Gen.Constant(Panther.CodeAnalysis.Binding.BoundUnitExpression.Default).ToArbitrary();

        public static Arbitrary<GlobalVariableSymbol> GlobalVariableSymbol() =>
        (
            from ident in Identifier()
            from type in Arb.Generate<TypeSymbol>()
            from readOnly in Arb.Generate<bool>()
            select new GlobalVariableSymbol(ident, readOnly, type)
        ).ToArbitrary();

        public static Gen<string> Identifier() =>
            from count in Gen.Choose(5, 10)
            from chars in Gen.ArrayOf(count, Gen.Elements<char>("abcdefghijklmnopqrstuvwxyz"))
            select new string(chars);

        public static Arbitrary<BoundLabel> BoundLabel() =>
            Identifier().Select(x => new BoundLabel(x)).ToArbitrary();

        public static Arbitrary<BoundLabelStatement> BoundLabelStatement() =>
            Arb.Generate<BoundLabel>().Select(x => new BoundLabelStatement(x)).ToArbitrary();

        public static Gen<BoundAssignmentExpression> GenBoundAssignmentExpression(TypeSymbol typeSymbol) =>
            from variable in GenLocalVariableSymbol(typeSymbol)
            from initializer in GenBoundExpression(typeSymbol)
            select new BoundAssignmentExpression(variable, initializer);

        public static Gen<BoundBlockExpression> GenBoundBlockExpression(TypeSymbol typeSymbol) =>
            from statements in Arb.Generate<ImmutableArray<BoundStatement>>()
            from expr in GenBoundExpression(typeSymbol)
            select new BoundBlockExpression(statements, expr);
        public static Gen<FunctionSymbol> GenFunctionSymbol(TypeSymbol typeSymbol) =>
            from name in Identifier()
            from parameters in Arb.Generate<ImmutableArray<ParameterSymbol>>()
            select new FunctionSymbol(name, parameters, typeSymbol);

        public static Gen<LocalVariableSymbol> GenLocalVariableSymbol(TypeSymbol typeSymbol) =>
            from ident in Identifier()
            from readOnly in Arb.Generate<bool>()
            select new LocalVariableSymbol(ident, readOnly, typeSymbol);

        public static Gen<BoundExpression> GenBoundLiteralExpression(TypeSymbol typeSymbol)
        {
            Gen<BoundExpression> UnitGen() =>
                Gen
                    .Constant(Panther.CodeAnalysis.Binding.BoundUnitExpression.Default)
                    .Select(x => (BoundExpression) x);

            Gen<BoundExpression> StringGen() =>
                Arb
                    .Generate<NonNull<string>>().Select(x => new BoundLiteralExpression(x.Item))
                    .Select(x => (BoundExpression) x);

            Gen<BoundExpression> IntGen() =>
                Arb
                    .Generate<int>()
                    .Select(x => new BoundLiteralExpression(x))
                    .Select(x => (BoundExpression) x);

            Gen<BoundExpression> BoolGen() =>
                Arb
                    .Generate<bool>()
                    .Select(x => new BoundLiteralExpression(x))
                    .Select(x => (BoundExpression) x);

            if (typeSymbol == Panther.CodeAnalysis.Symbols.TypeSymbol.Bool)
                return BoolGen();

            if (typeSymbol == Panther.CodeAnalysis.Symbols.TypeSymbol.Int)
                return IntGen();

            if (typeSymbol == Panther.CodeAnalysis.Symbols.TypeSymbol.String)
                return StringGen();

            if (typeSymbol == Panther.CodeAnalysis.Symbols.TypeSymbol.Unit)
                return UnitGen();


            if (typeSymbol == Panther.CodeAnalysis.Symbols.TypeSymbol.Any)
                return Gen.OneOf(BoolGen(), IntGen(), StringGen(), UnitGen());

            throw new ArgumentOutOfRangeException(nameof(typeSymbol));
        }

        public static Arbitrary<ImmutableArray<A>> ImmutableArray<A>()
        {
            return Gen.Sized(size =>
                    from n in Gen.Choose(0, size)
                    from array in Gen.ArrayOf(n, Gen.Resize(n == 0 ? 0 : size / n, Arb.Generate<A>()))
                    select System.Collections.Immutable.ImmutableArray.Create(array))
                .ToArbitrary();
        }

        public static Gen<BoundCallExpression> GenBoundCallExpression(TypeSymbol typeSymbol) =>
            from function in GenFunctionSymbol(typeSymbol)
            from args in Gen.Sequence(function.Parameters.Select(p => GenBoundExpression(p.Type)))
            select new BoundCallExpression(function, args.ToImmutableArray());

        public static Gen<BoundExpression> GenBoundExpression(TypeSymbol typeSymbol)
        {
            return Gen.Sized(size =>
            {
                if (size <= 1)
                {
                    return GenBoundLiteralExpression(typeSymbol);
                }

                return Gen.Resize(size / 2 ,  Gen.OneOf(
                    // Arb.Generate<BoundBinaryExpression>().Select(x => (BoundExpression) x),
                    // Arb.Generate<BoundConversionExpression>().Select(x => (BoundExpression) x),
                    // Arb.Generate<BoundIfExpression>().Select(x => (BoundExpression) x),
                    // Arb.Generate<BoundUnaryExpression>().Select(x => (BoundExpression) x),
                    // Arb.Generate<BoundUnitExpression>().Select(x => (BoundExpression) x),
                    // Arb.Generate<BoundVariableExpression>().Select(x => (BoundExpression) x),
                    // Arb.Generate<BoundForExpression>().Select(x => (BoundExpression) x),
                    // Arb.Generate<BoundWhileExpression>().Select(x => (BoundExpression) x)
                    GenBoundAssignmentExpression(typeSymbol).Select(x => (BoundExpression) x),
                    GenBoundBlockExpression(typeSymbol).Select(x => (BoundExpression) x),
                    GenBoundCallExpression(typeSymbol).Select(x => (BoundExpression) x),
                    GenBoundLiteralExpression(typeSymbol)
                ));
            });
        }
    }
}