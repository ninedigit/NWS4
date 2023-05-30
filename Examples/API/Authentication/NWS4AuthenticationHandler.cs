using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using NineDigit.NWS4.AspNetCore;

namespace NineDigit.NWS4.AspNetCore.Examples.API.Authentication;

public sealed class NWS4AuthenticationHandler : NWS4AuthenticationHeaderChunkedHandler
{
    public NWS4AuthenticationHandler(
        IOptionsMonitor<NWS4AuthenticationHeaderChunkedSchemeOptions> options,
        ILoggerFactory loggerFactory,
        ISystemClock clock,
        UrlEncoder encoder
    ) : base(options, loggerFactory, clock, encoder)
    {
    }

    protected override Task<AuthenticateResult> AuthenticateAsync(AuthenticateRequest request, CancellationToken cancellationToken)
    {
        // Implement authentication logic
        return Task.FromResult(AuthenticateResult.NoResult());
    }
}