namespace Panther.Tests.CodeAnalysis.Syntax
{
    public class TokenPairTestData
    {
        public TokenTestData Token1 { get; }
        public TokenTestData Token2 { get; }

        public TokenPairTestData(NonSeparatorTokenTestData token1, NonSeparatorTokenTestData token2)
        {
            Token1 = token1;
            Token2 = token2;
        }
    }
}