using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NineDigit.NWS4.AspNetCore.Tests
{
    public class ChunkedSignerTests
    {
        //[Fact]
        //public async Task Test()
        //{
        //    var httpRequest = HttpRequestHelper.FromFile("Requests/BigChunkedPost.request");
        //    var utcNow = new DateTime(2022, 10, 12, 06, 56, 16, 000, DateTimeKind.Utc);
        //    var privateKey = "9bb989cd09d43be8ca9f636785bbe8df01b8c0a3055d96bcae1aa38bb1aeeb39";
        //    var requestTimeWindow = TimeSpan.FromSeconds(300);

        //    var dateTimeProvider = new DefaultDateTimeProvider(() => utcNow);
        //    var signer = new AuthorizationHeaderChunkedSigner(dateTimeProvider);

        //    var content = await signer.ValidateSignatureAsync(httpRequest, privateKey, requestTimeWindow);
        //}

        [Fact]
        public async Task ChunkedSigner_SignsRequestCorrectly()
        {
            var blockSize = 65536;
            var utcNow = new DateTime(2022, 02, 07, 10, 47, 53, 026, DateTimeKind.Utc);

            var url = "http://ekasa-cloud-int.ninedigit.sk/api/v1/registrations/receipts";
            var httpMethod = HttpMethods.Post;
            var accessKey = "d51fbd43e205b16a806ca2399c7023b8";
            var privateKey = "1adaee8c378449453dcf40625d20d6b65b02f51aa28191df874296c1f13c8243";
            var headers = new HttpRequestHeaders()
            {
                { "__tenant", "39ff67bf-0182-4903-c820-2dd75eed9d21" }
            };

            var body = new string('?', 1000000);

            var dateTimeProvider = new DefaultDateTimeProvider(() => utcNow);
            var signer = new AuthorizationHeaderChunkedSigner(dateTimeProvider);

            var request = new HttpRequest()
            {
                RequestUri = new Uri(url),
                Method = httpMethod.ToString(),
                Headers = headers,
                Body = Encoding.UTF8.GetBytes(body)
            };

            var rawChunks = await signer.SignRequestAsync(request, accessKey, privateKey, blockSize);

            request.Body = rawChunks.ToArray();

            var bodyBytes = await signer.ValidateSignatureAsync(request, privateKey, TimeSpan.FromSeconds(300));
            var receivedBody = bodyBytes != null ? Encoding.UTF8.GetString(bodyBytes) : null;

            Assert.Equal(9, request.Headers.Count());
            Assert.True(request.Headers.TryGet("Authorization", out IEnumerable<string?>? values));
            
            var value = Assert.Single(values);
            
            Assert.Equal("NWS4-HMAC-SHA256 Credential%3Dd51fbd43e205b16a806ca2399c7023b8%2CSignedHeaders%3Dcontent-encoding%253Bcontent-length%253Bcontent-type%253Bhost%253Bx-nd-content-sha256%253Bx-nd-date%253Bx-nws-decoded-content-length%253B__tenant%2CTimestamp%3D2022-02-07T10%253A47%253A53.026Z%2CSignature%3Da9a63825ec5c79e8350e4db3aad1faf16cf5a95b6b0887776216c0787fe74547", value);
            Assert.Equal(body, receivedBody);
        }
    }
}
