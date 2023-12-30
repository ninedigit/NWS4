using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NineDigit.NWS4;

internal class AsyncCredentialsProvider : IAsyncCredentialsProvider
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<Credentials?>> _credentialsProvider;
    
    public AsyncCredentialsProvider(Func<HttpRequestMessage, Credentials?> credentialsProvider)
    {
        if (credentialsProvider is null)
            throw new ArgumentNullException(nameof(credentialsProvider));
        
        _credentialsProvider = (m, _) => Task.FromResult(credentialsProvider(m));
    }
    
    public AsyncCredentialsProvider(ICredentialsProvider credentialsProvider)
    {
        if (credentialsProvider is null)
            throw new ArgumentNullException(nameof(credentialsProvider));
        
        _credentialsProvider = (m, _) => Task.FromResult(credentialsProvider.GetCredentials(m));
    }
    
    public Task<Credentials?> GetCredentialsAsync(
        HttpRequestMessage requestMessage,
        CancellationToken cancellationToken = default)
    {
        if (requestMessage is null)
            throw new ArgumentNullException(nameof(requestMessage));
        
        return _credentialsProvider(requestMessage, cancellationToken);
    }
}