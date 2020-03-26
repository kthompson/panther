using System;
using System.Linq;
using FsCheck;
using Panther.CodeAnalysis.Syntax;

namespace Panther.Tests.CodeAnalysis.Syntax
{
    public class TokenGenerators
    {
        public static Arbitrary<BinaryOperatorSyntaxKind> BinaryOperatorSyntaxKind() =>
            Gen.Elements(SyntaxFacts.GetBinaryOperatorKinds())
                .Select(x => new BinaryOperatorSyntaxKind(x))
                .ToArbitrary();

        public static Arbitrary<UnaryOperatorSyntaxKind> UnaryOperatorSyntaxKind() =>
            Gen.Elements(SyntaxFacts.GetUnaryOperatorKinds())
                .Select(x => new UnaryOperatorSyntaxKind(x))
                .ToArbitrary();

        public static Arbitrary<TokenTestData> TokenTestData() => Gen
            .OneOf(
                Arb.Generate<NonSeparatorTokenTestData>().Select(x => (TokenTestData)x),
                Arb.Generate<SeparatorTokenTestData>().Select(x => (TokenTestData)x)
            )
            .ToArbitrary();

        public static Arbitrary<NonSeparatorTokenTestData> NonSeparatorTokenTestData() =>
            Gen.Elements(
                new[]
                {
                    new NonSeparatorTokenTestData(SyntaxKind.IdentifierToken, "a"),
                    new NonSeparatorTokenTestData(SyntaxKind.IdentifierToken, "abc"),
                    new NonSeparatorTokenTestData(SyntaxKind.NumberToken, "1"),
                    new NonSeparatorTokenTestData(SyntaxKind.NumberToken, "123"),
                    new NonSeparatorTokenTestData(SyntaxKind.NumberToken, "0"),
                }.Concat(
                    Enum.GetValues(typeof(SyntaxKind)).Cast<SyntaxKind>()
                        .Select(k => new NonSeparatorTokenTestData(k, SyntaxFacts.GetText(k)))
                        .Where(td => td.Text != null))
            ).ToArbitrary();

        public static Arbitrary<TokenPairTestData> TokenPairTestData()
        {
            var gen = from t1 in Arb.Generate<NonSeparatorTokenTestData>()
                      from t2 in Arb.Generate<NonSeparatorTokenTestData>()
                      where !TokensCoalesce(t1.Kind, t2.Kind)
                      select new TokenPairTestData(t1, t2);

            return gen.ToArbitrary();
        }

        public static Arbitrary<SeparatorTokenTestData> SeparatorTokenTestData()
        {
            return Gen.Elements(
                new SeparatorTokenTestData(SyntaxKind.WhitespaceToken, " "),
                new SeparatorTokenTestData(SyntaxKind.WhitespaceToken, "\t")
            ).ToArbitrary();
        }

        private static readonly SyntaxKind[] CoalescingKinds = new[]
        {
            SyntaxKind.NumberToken, SyntaxKind.EqualsToken, SyntaxKind.IdentifierToken
        };

        private static bool TokensCoalesce(SyntaxKind kind1, SyntaxKind kind2)
        {
            if (kind1 == kind2 && CoalescingKinds.Contains(kind1))
                return true;

            var kind1Keyword = kind1.ToString().EndsWith("Keyword");
            var kind2Keyword = kind2.ToString().EndsWith("Keyword");

            if (kind1Keyword && (kind2Keyword || kind2 == SyntaxKind.IdentifierToken))
                return true;

            if (kind1 == SyntaxKind.IdentifierToken && kind2Keyword)
                return true;

            // '!' + '==' => '!=' + '='
            if (kind1 == SyntaxKind.BangToken && kind2 == SyntaxKind.EqualsEqualsToken)
                return true;

            // '!' + '=' => '!='
            if (kind1 == SyntaxKind.BangToken && kind2 == SyntaxKind.EqualsToken)
                return true;

            // '=' + '==' => '==' + '='
            if (kind1 == SyntaxKind.EqualsToken && kind2 == SyntaxKind.EqualsEqualsToken)
                return true;

            // '<' + '=' => '<='
            // '<' + '==' => '<=' + '='
            if (kind1 == SyntaxKind.LessThanToken && (kind2 == SyntaxKind.EqualsToken || kind2 == SyntaxKind.EqualsEqualsToken))
                return true;

            // '>' + '=' => '>='
            // '>' + '==' => '>=' + '='
            if (kind1 == SyntaxKind.GreaterThanToken && (kind2 == SyntaxKind.EqualsToken || kind2 == SyntaxKind.EqualsEqualsToken))
                return true;

            return false;
        }
    }
}