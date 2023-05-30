using Microsoft.Extensions.Logging.Abstractions;

namespace NineDigit.NWS4.Tests.Tests;

public class ChunkedSignerTests
{
    [Theory]
    [InlineData(1024, 512, 1373)]
    public void CalculateChunkedContentLength_ReturnsCorrectContentLength(long originalContentLength, long maxChunkSize, long expectedChunkedContentLength)
    {
        var chunkedContentLength = AuthorizationHeaderChunkedSigner.CalculateChunkedContentLength(originalContentLength, maxChunkSize, NullLogger.Instance);

        Assert.Equal(expectedChunkedContentLength, chunkedContentLength);
    }
}