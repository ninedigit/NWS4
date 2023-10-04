using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NineDigit.NWS4.AspNetCore;

public class NWS4AuthenticationHeaderMessageHandler : DelegatingHandler
{
    private readonly Func<HttpRequestMessage, Credentials?> credentialsProvider;
        
    public NWS4AuthenticationHeaderMessageHandler(
        AuthorizationHeaderSigner signer,
        Func<HttpRequestMessage, Credentials?> credentialsProvider)
    {
        this.credentialsProvider =
            credentialsProvider ?? throw new ArgumentNullException(nameof(credentialsProvider));

        this.Signer = signer ?? throw new ArgumentNullException(nameof(signer));
            
        this.InnerHandler = new HttpClientHandler();
    }

    protected AuthorizationHeaderSigner Signer { get; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage requestMessage, CancellationToken cancellationToken)
    {
        var credentials = this.credentialsProvider(requestMessage);
        if (credentials != null)
        {
            var request = new HttpRequestMessageWrapper(requestMessage);

            await this.Signer
                .SignRequestAsync(request, credentials.PublicKey, credentials.PrivateKey, cancellationToken)
                .ConfigureAwait(false);
        }

        var result = await base.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);

        return result;
    }
}