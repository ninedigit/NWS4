using System.Net.Http.Headers;

namespace NineDigit.NWS4.Tests.Tests;

public class HttpRequestMessageHeadersWrapperTests
{
    [Fact]
    public void Xunit()
    {
        var userAgent =
            "Microsoft SignalR/6.0 (6.0.0+ae1a6cbe225b99c0bf38b7e31bf60cb653b73a52; macOS; .NET; .NET 6.0.25)";
        var xValues = new string[] { "Value1", "Value2" };
        
        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        
        httpRequestMessage.Headers.UserAgent.TryParseAdd(userAgent);
        httpRequestMessage.Headers.TryAddWithoutValidation("X-Value", xValues);
        
        var wrapper = new HttpRequestMessageHeadersWrapper(httpRequestMessage.Headers);
        var headers = wrapper.ToArray();
        
        Assert.Equal(2, wrapper.Count());

        Assert.Equal(headers[0].Key, "User-Agent");
        Assert.Equal(headers[0].Value, userAgent);
        
        Assert.Equal(headers[1].Key, "X-Value");
        Assert.Equal(headers[1].Value, $"{xValues[0]}, {xValues[1]}");
    }
}