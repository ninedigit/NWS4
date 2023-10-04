using System;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NineDigit.NWS4.AspNetCore;

public class NWS4AuthenticationHeaderSchemeOptions :
    NWS4AuthenticationSchemeOptions<AuthorizationHeaderChunkedSignerOptions>
{
}

public abstract class NWS4AuthenticationHeaderHandler :
    NWS4AuthenticationHandler<AuthorizationHeaderSigner, NWS4AuthenticationHeaderSchemeOptions>
{
    public NWS4AuthenticationHeaderHandler(
        IOptionsMonitor<NWS4AuthenticationHeaderSchemeOptions> options,
        ILoggerFactory loggerFactory,
        ISystemClock clock,
        UrlEncoder encoder)
        : base(options, loggerFactory, clock, encoder)
    {
    }

    protected override AuthorizationHeaderSigner CreateSigner(NWS4AuthenticationHeaderSchemeOptions options)
    {
        var logger = this.LoggerFactory.CreateLogger<AuthorizationHeaderSigner>();
        var dateTimeProvider = new SystemClockDateTimeProvider(this.Clock);
        var signer = new AuthorizationHeaderSigner(options.Signer, dateTimeProvider, logger);

        return signer;
    }

    protected override AuthData? ReadAuthData(IHttpRequest httpRequest)
    {
        var authHeader = httpRequest.Headers.FindAuthorization();

        if (string.IsNullOrEmpty(authHeader) || !AuthorizationHeaderSigner.CanValidateSignature(authHeader))
        {
            this.Logger.LogDebug("Authorization header is not present or is not in NWS4 format");
            return null;
        }

        try
        {
            return this.Signer.AuthDataSerializer.Read(authHeader);
        }
        catch (FormatException ex)
        {
            this.Logger.LogWarning(ex, "Invalid NWS4 authentication header");
            throw new FormatException("Invalid NWS4 authentication header.");
        }
    }
}