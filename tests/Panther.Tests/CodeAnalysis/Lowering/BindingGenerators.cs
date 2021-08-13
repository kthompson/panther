using System;
using System.Collections.Immutable;
using System.Linq;
using FsCheck;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;
using Type = Panther.CodeAnalysis.Symbols.Type;

#pragma warning disable 612

namespace Panther.Tests.CodeAnalysis.Lowering
{
    class BindingGenerators
    {
        public static readonly Gen<Type> TypeGen = Gen.OneOf(
            Gen.Constant(Panther.CodeAnalysis.Symbols.Type.Bool),
            Gen.Constant(Panther.CodeAnalysis.Symbols.Type.Int),
            Gen.Constant(Panther.CodeAnalysis.Symbols.Type.String),
            Gen.Constant(Panther.CodeAnalysis.Symbols.Type.Unit)
        );

        public static Arbitrary<SyntaxNode> SyntaxNode() =>
            Gen.Constant((SyntaxNode)new SyntaxToken(null!, SyntaxKind.InvalidTokenTrivia, 0)).ToArbitrary();

        public static Arbitrary<BoundStatement> BoundStatement() =>
            Gen.OneOf(
                Arb.Generate<BoundConditionalGotoStatement>().Select(x => (BoundStatement)x),
                Arb.Generate<BoundExpressionStatement>().Select(x => (BoundStatement)x),
                Arb.Generate<BoundGotoStatement>().Select(x => (BoundStatement)x),
                Arb.Generate<BoundLabelStatement>().Select(x => (BoundStatement)x),
                Arb.Generate<BoundVariableDeclarationStatement>().Select(x => (BoundStatement)x)
            ).ToArbitrary();

        public static Arbitrary<BoundGotoStatement> BoundGotoStatement() =>
        (
            from token in Arb.Generate<SyntaxNode>()
            from label in Arb.Generate<BoundLabel>()
            select new BoundGotoStatement(token, label)
        ).ToArbitrary();

        public static Arbitrary<BoundConditionalGotoStatement> BoundConditionalGotoStatement() =>
        (
            from token in Arb.Generate<SyntaxNode>()
            from label in Arb.Generate<BoundLabel>()
            from jumpIfTrue in Arb.Generate<bool>()
            from expr in GenBoundExpression(Panther.CodeAnalysis.Symbols.Type.Bool)
            select new BoundConditionalGotoStatement(token, label, expr, jumpIfTrue)
        ).ToArbitrary();

        public static Arbitrary<BoundExpressionStatement> BoundExpressionStatement() =>
        (
            from token in Arb.Generate<SyntaxNode>()
            from type in Arb.Generate<Type>()
            from expr in GenBoundExpression(type)
            select new BoundExpressionStatement(token, expr)
        ).ToArbitrary();

        public static Arbitrary<BoundVariableDeclarationStatement> BoundVariableDeclarationStatement() =>
        (
            from token in Arb.Generate<SyntaxNode>()
            from type in Arb.Generate<Type>()
            from variable in GenLocalVariableSymbol(type)
            from expr in GenBoundExpression(type)
            select new BoundVariableDeclarationStatement(token, variable, expr)
        ).ToArbitrary();

        public static Arbitrary<Type> Type => TypeGen.ToArbitrary();
        public static Arbitrary<ParameterSymbol> ParameterSymbol() =>
        (
            from ident in Identifier()
            from readOnly in Arb.Generate<bool>()
            from index in Gen.Choose(0, 5)
            from type in Arb.Generate<Type>()
            select new ParameterSymbol(ident, type, index)
        ).ToArbitrary();

        public static Arbitrary<BoundUnitExpression> BoundUnitExpression =>
            Gen.Constant(new BoundUnitExpression(null!)).ToArbitrary();

        public static Gen<string> Identifier() =>
            from count in Gen.Choose(5, 10)
            from chars in Gen.ArrayOf(count, Gen.Elements<char>("abcdefghijklmnopqrstuvwxyz"))
            select new string(chars);

        public static Arbitrary<BoundLabel> BoundLabel() =>
            Identifier().Select(x => new BoundLabel(x)).ToArbitrary();

        public static Arbitrary<BoundLabelStatement> BoundLabelStatement() =>
            (
                from token in Arb.Generate<SyntaxNode>()
                from x in Arb.Generate<BoundLabel>()
                select new BoundLabelStatement(token, x)
            )
            .ToArbitrary();

        public static Gen<BoundIfExpression> GenIfExpression(Type type) =>
            from condition in GenBoundExpression(Panther.CodeAnalysis.Symbols.Type.Bool)
            from thenExpr in GenBoundExpression(type)
            from elseExpr in GenBoundExpression(type)
            select new BoundIfExpression(null!, condition, thenExpr, elseExpr);

        public static Gen<BoundAssignmentExpression> GenBoundAssignmentExpression(Type type) =>
            from token in Arb.Generate<SyntaxNode>()
            from variable in GenLocalVariableSymbol(type)
            from initializer in GenBoundExpression(type)
            select new BoundAssignmentExpression(token, variable, initializer);

        public static Gen<BoundBlockExpression> GenBoundBlockExpression(Type type) =>
            from token in Arb.Generate<SyntaxNode>()
            from statements in Arb.Generate<ImmutableArray<BoundStatement>>()
            from expr in GenBoundExpression(type)
            select new BoundBlockExpression(token, statements, expr);

        public static Gen<MethodSymbol> GenFunctionSymbol(Type returnType) =>
            from name in Identifier()
            from parameters in Arb.Generate<ImmutableArray<ParameterSymbol>>()
            select (MethodSymbol)new ImportedMethodSymbol(name, parameters, returnType);

        public static Gen<LocalVariableSymbol> GenLocalVariableSymbol(Type type) =>
            from ident in Identifier()
            from readOnly in Arb.Generate<bool>()
            select new LocalVariableSymbol(ident, readOnly, type, null);

        public static Gen<BoundExpression> GenBoundLiteralExpression(Type typeSymbol)
        {
            Gen<BoundExpression> UnitGen() =>
                from token in Arb.Generate<SyntaxNode>()
                select (BoundExpression)new BoundUnitExpression(token!);

            Gen<BoundExpression> StringGen() =>
                from token in Arb.Generate<SyntaxNode>()
                from x in Arb.Generate<NonNull<string>>()
                select (BoundExpression)new BoundLiteralExpression(token, x.Item);

            Gen<BoundExpression> IntGen() =>
                from token in Arb.Generate<SyntaxNode>()
                from x in Arb.Generate<int>()
                select (BoundExpression)new BoundLiteralExpression(token, x);

            Gen<BoundExpression> BoolGen() =>
                from token in Arb.Generate<SyntaxNode>()
                from x in Arb.Generate<bool>()
                select (BoundExpression)new BoundLiteralExpression(token, x);

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
                    select System.Collections.Immutable.ImmutableArray.Create(array))
                .ToArbitrary();
        }

        public static Gen<BoundCallExpression> GenBoundCallExpression(Type type) =>
            from token in Arb.Generate<SyntaxNode>()
            from function in GenFunctionSymbol(type)
            from args in Gen.Sequence(function.Parameters.Select(p => GenBoundExpression(p.Type)))
            select new BoundCallExpression(token, function, null, args.ToImmutableArray());

        public static Gen<BoundExpression> GenBoundExpression(Type type)
        {
            return Gen.Sized(size =>
            {
                if (size <= 1)
                {
                    return GenBoundLiteralExpression(type);
                }

                if (type == Panther.CodeAnalysis.Symbols.Type.Unit)
                {
                    return Gen.Resize(size / 2, Gen.OneOf(
                        // Arb.Generate<BoundVariableExpression>().Select(x => (BoundExpression) x),
                        // Arb.Generate<BoundForExpression>().Select(x => (BoundExpression) x),
                        // Arb.Generate<BoundWhileExpression>().Select(x => (BoundExpression) x)

                        GenIfExpression(type).Select(x => (BoundExpression)x),
                        GenBoundAssignmentExpression(type).Select(x => (BoundExpression)x),
                        GenBoundBlockExpression(type).Select(x => (BoundExpression)x),
                        GenBoundCallExpression(type).Select(x => (BoundExpression)x),
                        GenBoundLiteralExpression(type)
                    ));
                }

                return Gen.Resize(size / 2, Gen.OneOf(
                    // Arb.Generate<BoundBinaryExpression>().Select(x => (BoundExpression) x),
                    // Arb.Generate<BoundConversionExpression>().Select(x => (BoundExpression) x),
                    // Arb.Generate<BoundUnaryExpression>().Select(x => (BoundExpression) x),
                    // Arb.Generate<BoundVariableExpression>().Select(x => (BoundExpression) x),

                    GenIfExpression(type).Select(x => (BoundExpression)x),
                    GenBoundBlockExpression(type).Select(x => (BoundExpression)x),
                    GenBoundCallExpression(type).Select(x => (BoundExpression)x),
                    GenBoundLiteralExpression(type)
                ));
            });
        }
    }
}