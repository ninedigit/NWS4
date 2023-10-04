using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NineDigit.NWS4.AspNetCore;

internal static class HttpRequestExtensions
{
    public static Task<byte[]> PeekBodyAsync(this Microsoft.AspNetCore.Http.HttpRequest request, CancellationToken cancellationToken = default)
        => request.PeekBodyAsync(bufferLength: 8192, cancellationToken);

    public static async Task<byte[]> PeekBodyAsync(this Microsoft.AspNetCore.Http.HttpRequest request, int bufferLength, CancellationToken cancellationToken = default)
    {
        try
        {
            request.EnableBuffering();

            using var stream = new MemoryStream();
            var buffer = new byte[request.ContentLength ?? bufferLength];
            int bytesRead;

            while ((bytesRead = await request.Body.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
                stream.Write(buffer, 0, bytesRead);

            var result = stream.ToArray();
            return result;
        }
        finally
        {
            request.Body.Position = 0;
        }
    }
}