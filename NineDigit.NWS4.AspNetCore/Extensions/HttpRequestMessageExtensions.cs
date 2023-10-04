using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace NineDigit.NWS4.AspNetCore;

internal static class HttpRequestMessageExtensions
{
    public static async Task<byte[]?> TryReadContentAsByteArrayAsync(this HttpRequestMessage self)
    {
        if (self == null)
            throw new ArgumentNullException(nameof(self));

        byte[]? bodyContent = null;

        var requestContent = self.Content;
        if (requestContent != null)
        {
            using var memoryStream = new MemoryStream();
            var stream = await requestContent.ReadAsStreamAsync().ConfigureAwait(false);

            stream.CopyTo(memoryStream);

            memoryStream.Position = 0;
            bodyContent = memoryStream.ToArray();

            var readOnlyStream = new MemoryStream(bodyContent, writable: false);
            var streamContent = new StreamContent(readOnlyStream);

            foreach (var item in requestContent.Headers)
                streamContent.Headers.TryAddWithoutValidation(item.Key, item.Value);

            self.Content = streamContent;
        }

        return bodyContent;
    }
}