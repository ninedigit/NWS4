using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NineDigit.NWS4;

public interface IAsyncCredentialsProvider
{
    Task<Credentials?> GetCredentialsAsync(
        HttpRequestMessage requestMessage,
        CancellationToken cancellationToken = default);
}