using Panther.CodeAnalysis.Text;
using Xunit;

namespace Panther.Tests.CodeAnalysis.Text;

public class SourceTextTests
{
    [Theory]
    [InlineData(".", 1)]
    [InlineData(".\r\n", 2)]
    [InlineData(".\r\n\r\n", 3)]
    public void SourceTextIncludesLastLine(string text, int expectedLineCount)
    {
        var sourceText = SourceFile.From(text);
        Assert.Equal(expectedLineCount, sourceText.LineCount);
    }
}
