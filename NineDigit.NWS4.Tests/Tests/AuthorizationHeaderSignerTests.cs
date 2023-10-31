using System.Text;

namespace NineDigit.NWS4.AspNetCore.Tests
{
    public class AuthorizationHeaderSignerTests
    {
        [Fact]
        public async Task PHPExamples_GetAll()
        {
            var utcNow = Signer.ParseDateTime("2023-05-30T13:17:01.992Z");
            var httpMethod = HttpMethod.Post;
            var url = "http://example.com:8080/api/user?i=1$ref=1&validate";
            var accessKey = "d948ec22e47790caacce234b792a0f117d85c365";
            var privateKey = "5070adfb2bedf1c0c97d5c8cfa8c794d513249b802ea10596a175d88828abc19";
            var headers = new HttpRequestHeaders
            {
                { "Content-Type", "application/json" },
                { "Accept", "application/json" }
            };
            var body = @"{""name"":""John""}";

            var dateTimeProvider = new DefaultDateTimeProvider(() => utcNow);
            var signer = new AuthorizationHeaderSigner(dateTimeProvider);

            var request = new HttpRequest
            {
                RequestUri = new Uri(url),
                Method = httpMethod.ToString(),
                Headers = headers,
                Body = Encoding.UTF8.GetBytes(body)
            };

            var computeSignatureResult = await signer.ComputeSignatureAsync(request, accessKey, privateKey);
            var signature = computeSignatureResult.Signature;
            
            var authData = new AuthData(computeSignatureResult);
            signer.AuthDataSerializer.Write(request, authData);

            var auth = request.Headers.FindAuthorization();
            
            Assert.Equal("NWS4-HMAC-SHA256 Credential%3Dd948ec22e47790caacce234b792a0f117d85c365%2CSignedHeaders%3Daccept%253Bcontent-type%253Bhost%253Bx-nd-content-sha256%253Bx-nd-date%2CTimestamp%3D2023-05-30T13%253A17%253A01.992Z%2CSignature%3D87c7a2057285f0db8dc3b7c95c15fe5476f5ab44b73b3e15990d8b1a837d4ddb", auth);
        }
        
        [Fact]
        public async Task Ticket_7928()
        {
            var utcNow = Signer.ParseDateTime("2023-05-30T07:37:59.390Z");
            var requestTimeWindow = Timeout.InfiniteTimeSpan;
            var httpMethod = HttpMethod.Put;
            var url = "http://213.160.191.53:4000/api/plus/GQR5xWLN";
            var proxyHost = "dr-sam.eu.ngrok.io";
            var accessKey = "d948ec22e47790caacce234b792a0f117d85c365";
            var privateKey = "5070adfb2bedf1c0c97d5c8cfa8c794d513249b802ea10596a175d88828abc19";
            var headers = new HttpRequestHeaders
            {
                { "User-Agent", "GuzzleHttp/7" },
                { "Accept-Encoding", "gzip" },
                { "Content-Type", "application/json" }
            };

            var body = @"{""ArticleCategoryLabel"":""PLK"",""Code"":8,""Id"":""GQR5xWLN"",""IsActive"":true,""IsDiscountAllowed"":true,""IsPriceFixed"":false,""Name"":""Kozmetick\u00e1 ta\u0161ti\u010dka Folk \""CIERNA IV\"" Strieborn\u00e1"",""Description"":""Kozmetick\u00e1 ta\u0161ti\u010dka Folk \""CIERNA IV\"" Strieborn\u00e1"",""Codes"":[""KTF9IV""],""RetailPrice"":{""Amount"":30,""CurrencyLabel"":""EUR""},""StockName"":""Polo\u017eky"",""StockQuantity"":{""Amount"":0,""Unit"":""ks""},""StockValue"":{""Amount"":0,""CurrencyLabel"":""EUR""},""Type"":""StockItem"",""Unit"":""ks"",""VatCategory"":1}";

            var dateTimeProvider = new DefaultDateTimeProvider(() => utcNow);
            var signerOptions = new AuthorizationHeaderSignerOptions
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

            Assert.Equal(7, request.Headers.Count());
            Assert.True(request.Headers.TryGet("Authorization", out IEnumerable<string>? values));
            var value = Assert.Single(values);
            Assert.Equal("NWS4-HMAC-SHA256 Credential%3Dd948ec22e47790caacce234b792a0f117d85c365%2CSignedHeaders%3Daccept-encoding%253Bcontent-type%253Bhost%253Buser-agent%253Bx-nd-content-sha256%253Bx-nd-date%2CTimestamp%3D2023-05-30T07%253A37%253A59.390Z%2CSignature%3D6dc1d647dcbd3c9180fcc5f59d110bdabc94493e1a92ed36deaeddd8e1d512c3", value);

            request.Headers.Remove(HeaderNames.Host);
            request.Headers.Add(HeaderNames.Host, proxyHost);
            request.Headers.Add(HttpRequestHeaderNames.XForwardedHost, $"{request.RequestUri.Host}:{request.RequestUri.Port}");
            
            await signer.ValidateSignatureAsync(request, privateKey, requestTimeWindow, CancellationToken.None)
                .ConfigureAwait(false);
        }
        
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
            Assert.Equal("NWS4-HMAC-SHA256 Credential%3Dd51fbd43e205b16a806ca2399c7023b8%2CSignedHeaders%3Dhost%253Bx-nd-content-sha256%253Bx-nd-date%253B__tenant%2CTimestamp%3D2022-02-07T10%253A47%253A53.026Z%2CSignature%3D6a130431fc7fe63f42821426028af06ba73bbdbb5e1a65e4d54249877603bcfe", value);

            request.Headers.Remove(HeaderNames.Host);
            request.Headers.Add(HeaderNames.Host, proxyHost);
            request.Headers.Add(HttpRequestHeaderNames.XForwardedHost, host);
            
            await signer.ValidateSignatureAsync(request, privateKey, requestTimeWindow, CancellationToken.None)
                .ConfigureAwait(false);
        }
    }
}
