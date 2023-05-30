using HttpMachine;
using System;
using System.IO;

namespace NineDigit.NWS4.AspNetCore
{
    internal static class HttpRequestHelper
    {
        internal sealed class CustomHttpParserDelegate : HttpParserDelegate
        {
        }

        public static HttpRequest FromFile(string path)
        {
            var contentBytes = File.ReadAllBytes(path);

            using var handler = new CustomHttpParserDelegate();
            using var parser = new HttpCombinedParser(handler);

            var parsedBytes = parser.Execute(contentBytes);

            if (parsedBytes != contentBytes.Length)
                throw new Exception("Unable to read HTTP data.");

            var httpRequest = handler.ToHttpRequest();

            return httpRequest;
        }
    }
}
