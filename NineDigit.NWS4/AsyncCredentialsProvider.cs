using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NineDigit.NWS4;

public class AsyncCredentialsProvider : IAsyncCredentialsProvider
{
    protected Func<HttpRequestMessage, CancellationToken, Task<Credentials?>> CredentialsProvider { get; }
    
    public AsyncCredentialsProvider(Func<HttpRequestMessage, Credentials?> credentialsProvider)
    {
        if (credentialsProvider is null)
            throw new ArgumentNullException(nameof(credentialsProvider));
        
        CredentialsProvider = (m, _) => Task.FromResult(credentialsProvider(m));
    }
    
    public AsyncCredentialsProvider(ICredentialsProvider credentialsProvider)
    {
        if (credentialsProvider is null)
            throw new ArgumentNullException(nameof(credentialsProvider));
        
        CredentialsProvider = (m, _) => Task.FromResult(credentialsProvider.GetCredentials(m));
    }
    
    public virtual Task<Credentials?> GetCredentialsAsync(
        HttpRequestMessage requestMessage,
        CancellationToken cancellationToken = default)
    {
        if (requestMessage is null)
            throw new ArgumentNullException(nameof(requestMessage));
        
        return CredentialsProvider(requestMessage, cancellationToken);
    }
}