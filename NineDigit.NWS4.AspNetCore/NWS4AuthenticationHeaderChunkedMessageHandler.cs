using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NineDigit.NWS4.AspNetCore;

public class NWS4AuthenticationHeaderChunkedMessageHandler : DelegatingHandler
{
    private readonly Func<HttpRequestMessage, Credentials?> _credentialsProvider;
        
    public NWS4AuthenticationHeaderChunkedMessageHandler(
        AuthorizationHeaderChunkedSigner signer,
        Func<HttpRequestMessage, Credentials?> credentialsProvider
    ) : this(maxRequestBodySize: default, signer, credentialsProvider)
    {
    }

    public NWS4AuthenticationHeaderChunkedMessageHandler(
        long? maxRequestBodySize,
        AuthorizationHeaderChunkedSigner signer,
        Func<HttpRequestMessage, Credentials?> credentialsProvider)
    {
        if (maxRequestBodySize.HasValue && maxRequestBodySize.Value < AuthorizationHeaderChunkedSigner.MinBlockSize)
            throw new ArgumentOutOfRangeException(nameof(maxRequestBodySize));

        MaxRequestBodySize = maxRequestBodySize;
        _credentialsProvider = credentialsProvider ?? throw new ArgumentNullException(nameof(credentialsProvider));
        Signer = signer ?? throw new ArgumentNullException(nameof(signer));
        InnerHandler = new HttpClientHandler();
    }

    public long? MaxRequestBodySize { get; set; }
    protected AuthorizationHeaderChunkedSigner Signer { get; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage requestMessage, CancellationToken cancellationToken)
    {
        var credentials = _credentialsProvider(requestMessage);
        if (credentials != null)
        {
            var maxBodySize = MaxRequestBodySize;
            var request = new HttpRequestMessageWrapper(requestMessage);

            var contentLength = requestMessage.Content?.Headers.ContentLength;
            if (maxBodySize.HasValue && contentLength.HasValue && contentLength.Value > maxBodySize)
            {
                var chunks = await Signer
                    .SignRequestAsync(request, credentials.PublicKey, credentials.PrivateKey, maxBodySize.Value, cancellationToken)
                    .ConfigureAwait(false);

                if (chunks.Any())
                {
                    requestMessage.Content = new PushStreamContent(async (stream, httpContent, transportContext) =>
                    {
                        foreach (var chunk in chunks)
                        {
#if NETSTANDARD2_0
                            var buffer = chunk.ToArray();
                            await stream.WriteAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
#else
                            await stream.WriteAsync(chunk, cancellationToken).ConfigureAwait(false);
#endif
                        }

                        stream.Close();
                    });
                }
            }
            else
            {
                await Signer
                    .SignRequestAsync(request, credentials.PublicKey, credentials.PrivateKey, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        var result = await base
            .SendAsync(requestMessage, cancellationToken)
            .ConfigureAwait(false);

        return result;
    }
}