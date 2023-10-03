using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NineDigit.NWS4.AspNetCore;

public class NWS4AuthenticationHeaderChunkedMessageHandler : DelegatingHandler
{
    private readonly Func<HttpRequestMessage, Credentials?> credentialsProvider;
        
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

        this.MaxRequestBodySize = maxRequestBodySize;

        this.credentialsProvider =
            credentialsProvider ?? throw new ArgumentNullException(nameof(credentialsProvider));

        this.Signer = signer ?? throw new ArgumentNullException(nameof(signer));
            
        this.InnerHandler = new HttpClientHandler();
    }

    public long? MaxRequestBodySize { get; set; }
    protected AuthorizationHeaderChunkedSigner Signer { get; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage requestMessage, CancellationToken cancellationToken)
    {
        var credentials = this.credentialsProvider(requestMessage);
        if (credentials != null)
        {
            var maxBodySize = this.MaxRequestBodySize;
            var request = new HttpRequestMessageWrapper(requestMessage);

            var contentLength = requestMessage.Content?.Headers.ContentLength;
            if (maxBodySize.HasValue && contentLength.HasValue && contentLength.Value > maxBodySize)
            {
                var chunks = await this.Signer
                    .SignRequestAsync(request, credentials.PublicKey, credentials.PrivateKey, maxBodySize.Value, cancellationToken)
                    .ConfigureAwait(false);

                requestMessage.Content = new PushStreamContent(async (stream, httpContent, transportContext) =>
                {
                    foreach (var chunk in chunks)
                        await stream.WriteAsync(chunk, cancellationToken).ConfigureAwait(false);

                    stream.Close();
                });
            }
            else
            {
                await this.Signer
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