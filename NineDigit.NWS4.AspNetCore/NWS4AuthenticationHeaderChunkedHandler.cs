using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Text.Encodings.Web;

namespace NineDigit.NWS4.AspNetCore
{
    public class NWS4AuthenticationHeaderChunkedSchemeOptions :
        NWS4AuthenticationSchemeOptions<AuthorizationHeaderChunkedSignerOptions>
    {
    }

    public abstract class NWS4AuthenticationHeaderChunkedHandler :
        NWS4AuthenticationHandler<AuthorizationHeaderChunkedSigner, NWS4AuthenticationHeaderChunkedSchemeOptions>
    {
        public NWS4AuthenticationHeaderChunkedHandler(
            IOptionsMonitor<NWS4AuthenticationHeaderChunkedSchemeOptions> options,
            ILoggerFactory loggerFactory,
            ISystemClock clock,
            UrlEncoder encoder)
            : base(options, loggerFactory, clock, encoder)
        {
        }

        protected override AuthorizationHeaderChunkedSigner CreateSigner(
            NWS4AuthenticationHeaderChunkedSchemeOptions options)
        {
            var logger = this.LoggerFactory.CreateLogger<AuthorizationHeaderChunkedSigner>();
            var dateTimeProvider = new SystemClockDateTimeProvider(this.Clock);
            var signer = new AuthorizationHeaderChunkedSigner(options.Signer, dateTimeProvider, logger);

            return signer;
        }

        protected override AuthData? ReadAuthData(IHttpRequest httpRequest)
        {
            var authHeader = httpRequest.Headers.FindAuthorization();

            if (string.IsNullOrEmpty(authHeader) || !AuthorizationHeaderSigner.CanValidateSignature(authHeader))
            {
                this.Logger.LogDebug("Authorization header is not present or is not in NWS4 format.");
                return null;
            }

            try
            {
                return this.Signer.AuthDataSerializer.Read(authHeader);
            }
            catch (FormatException ex)
            {
                this.Logger.LogWarning(ex, "Invalid NWS4 authentication header.");
                throw new FormatException("Invalid NWS4 authentication header.");
            }
        }
    }
}
