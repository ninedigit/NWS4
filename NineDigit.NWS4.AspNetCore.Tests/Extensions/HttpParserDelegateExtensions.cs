using HttpMachine;
using System;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace NineDigit.NWS4.AspNetCore
{
    internal static class HttpParserDelegateExtensions
    {
        public static HttpRequest ToHttpRequest(this HttpParserDelegate parser)
        {
            var request = parser.HttpRequestResponse;

            if (request is null)
                throw new ArgumentException("HTTP Request/Response message not present.", nameof(parser));

            if (!request.IsEndOfMessage)
                throw new InvalidOperationException("Message has not been received yet.");

            if (request.IsUnableToParseHttp)
                throw new FormatException("Unable to parse HTTP.");

            var httpRequest = new HttpRequest(request.Body)
            {
                RequestUri = new Uri(request.RequestUri),
                Method = request.Method,
                Headers = new HttpRequestHeaders(request.Headers)
            };

            if (request.Body != null)
            {
                request.Body.Position = 0;
                httpRequest.Body = request.Body.ToArray();

                var payload = Encoding.UTF8.GetString(httpRequest.Body);
            }

            return httpRequest;
        }

        //public static HttpRequestMessage ToHttpRequestMessage(this HttpParserDelegate @delegate)
        //{
        //    var request = @delegate.HttpRequestResponse;

        //    if (request is null)
        //        throw new ArgumentException("HTTP Request/Response message not present.", nameof(@delegate));

        //    if (!request.IsEndOfMessage)
        //        throw new InvalidOperationException("Message has not been received yet.");

        //    if (request.IsUnableToParseHttp)
        //        throw new FormatException("Unable to parse HTTP.");

        //    var httpMethod = new HttpMethod(request.Method);
        //    var httpRequestMessage = new HttpRequestMessage(httpMethod, request.RequestUri);

        //    foreach (var header in request.Headers)
        //        httpRequestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);

        //    if (request.Body != null)
        //    {
        //        request.Body.Position = 0;
        //        httpRequestMessage.Content = new StreamContent(request.Body);

        //        foreach (var header in request.Headers)
        //            httpRequestMessage.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        //    }

        //    return httpRequestMessage;
        //}
    }
}
