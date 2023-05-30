using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.WebApiCompatShim;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NineDigit.NWS4.AspNetCore
{
    public static class HttpContextExtensions
    {
        public static async Task<HttpRequestMessage> ToHttpRequestMessageAsync(this HttpContext context, int bufferSize = 4096, CancellationToken cancellationToken = default)
        {
            var httpRequestMessage = context.GetHttpRequestMessage();
            var requestBody = context.Request.Body;
            int receivedBytes = 0;

            if (requestBody != null)
            {
                using var memoryStream = new MemoryStream();

                var buffer = new byte[bufferSize];
                int read = 0;

                while ((read = await requestBody.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
                {
                    memoryStream.Write(buffer, 0, read);
                    receivedBytes += read;
                }

                memoryStream.Position = 0;

                var data = memoryStream.ToArray();

                context.Request.Body = new MemoryStream(data, writable: false);
                httpRequestMessage.Content = new StreamContent(new MemoryStream(data, writable: false));
            }

            return httpRequestMessage;
        }
    }
}
