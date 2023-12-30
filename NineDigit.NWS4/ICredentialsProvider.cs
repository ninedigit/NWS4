using System.Net.Http;

namespace NineDigit.NWS4;

public interface ICredentialsProvider
{
    Credentials? GetCredentials(HttpRequestMessage requestMessage);
}