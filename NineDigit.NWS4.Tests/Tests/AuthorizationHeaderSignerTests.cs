using System.Text;
using Microsoft.Net.Http.Headers;

namespace NineDigit.NWS4.AspNetCore.Tests
{
    public class AuthorizationHeaderSignerTests
    {
        // TODO: Fix issue, when request is incorrectly loaded from file. Sometimes, it's caused by invalid encoding/line ending?
        
        // [Fact]
        // public async Task Requests_RegularPost()
        // {
        //     var httpRequest = HttpRequestHelper.FromFile("Requests/RegularPost.request");
        //     var utcNow = new DateTime(2022, 10, 12, 06, 56, 16, 000, DateTimeKind.Utc);
        //     var privateKey = "9bb989cd09d43be8ca9f636785bbe8df01b8c0a3055d96bcae1aa38bb1aeeb39";
        //     var requestTimeWindow = TimeSpan.FromSeconds(300);
        //
        //     var dateTimeProvider = new DefaultDateTimeProvider(() =>
        //     {
        //         if (httpRequest.Headers.TryGet(SignerBase.XNDDate, out var values) && values?.Count() > 0)
        //             return AuthorizationHeaderSigner.ParseUtcDateTime(values.First()!);
        //
        //         throw new InvalidOperationException("Unable to get date from HTTP request.");
        //     });
        //
        //     var signer = new AuthorizationHeaderChunkedSigner(dateTimeProvider);
        //
        //     var content = await signer.ValidateSignatureAsync(httpRequest, privateKey, requestTimeWindow);
        // }

        [Fact]
        public async Task SignerForAuthorizationHeader_SignsPostRequestCorrectly()
        {
            var utcNow = new DateTime(2022, 02, 07, 10, 47, 53, 026, DateTimeKind.Utc);
            var requestTimeWindow = Timeout.InfiniteTimeSpan;

            var host = "ekasa-cloud-int.ninedigit.sk";
            var url = $"http://{host}/api/v1/registrations/receipts";
            var proxyHost = "ekasa-cloud-int.ngrok.sk";
            var httpMethod = HttpMethod.Post;
            var accessKey = "d51fbd43e205b16a806ca2399c7023b8";
            var privateKey = "1adaee8c378449453dcf40625d20d6b65b02f51aa28191df874296c1f13c8243";
            var headers = new HttpRequestHeaders
            {
                { "__tenant", "39ff67bf-0182-4903-c820-2dd75eed9d21" }
            };

            var body = @"{
    ""Key"": ""Value""
}";

            var dateTimeProvider = new DefaultDateTimeProvider(() => utcNow);
            var signerOptions = new AuthorizationHeaderSignerOptions()
            {
                AllowForwardedHostHeader = true
            };
            
            var signer = new AuthorizationHeaderSigner(signerOptions, dateTimeProvider);

            var request = new HttpRequest
            {
                RequestUri = new Uri(url),
                Method = httpMethod.ToString(),
                Headers = headers,
                Body = Encoding.UTF8.GetBytes(body)
            };

            await signer.SignRequestAsync(request, accessKey, privateKey);

            Assert.Equal(5, request.Headers.Count());
            Assert.True(request.Headers.TryGet("Authorization", out IEnumerable<string>? values));
            var value = Assert.Single(values);
            Assert.Equal("NWS4-HMAC-SHA256 Credential%3dd51fbd43e205b16a806ca2399c7023b8%2cSignedHeaders%3dhost%253bx-nd-content-sha256%253bx-nd-date%253b__tenant%2cTimestamp%3d2022-02-07T10%253a47%253a53.026Z%2cSignature%3d6a130431fc7fe63f42821426028af06ba73bbdbb5e1a65e4d54249877603bcfe", value);

            request.Headers.Remove(HeaderNames.Host);
            request.Headers.Add(HeaderNames.Host, proxyHost);
            request.Headers.Add(HttpRequestHeaderNames.XForwardedHost, host);
            
            await signer.ValidateSignatureAsync(request, privateKey, requestTimeWindow, CancellationToken.None)
                .ConfigureAwait(false);
        }
    }
}
