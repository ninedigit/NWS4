using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;

namespace NineDigit.NWS4.AspNetCore
{
    public abstract class NWS4AuthenticationHandler<TSigner, TOptions> : AuthenticationHandler<TOptions>
        where TSigner : Signer
        where TOptions : NWS4AuthenticationSchemeOptions, new()
    {
        private TSigner? signer;
        private readonly object signerSyncRoot = new();
        
        public NWS4AuthenticationHandler(
            IOptionsMonitor<TOptions> options,
            ILoggerFactory loggerFactory,
            ISystemClock clock,
            UrlEncoder encoder
        ) : base(options, loggerFactory, encoder, clock)
        {
            this.LoggerFactory = loggerFactory;
        }

        protected TSigner Signer
            => this.GetOrCreateSigner();
        
        protected ILoggerFactory LoggerFactory { get; }

        protected abstract TSigner CreateSigner(TOptions options);
        protected abstract AuthData? ReadAuthData(IHttpRequest request);

        private TSigner GetOrCreateSigner()
            => this.GetOrCreateSigner(this.Options);
        
        private TSigner GetOrCreateSigner(TOptions options)
        {
            lock (this.signerSyncRoot)
            {
                if (this.signer is null)
                    this.signer = this.CreateSigner(options);

                return this.signer;
            }
        }
        
        protected sealed override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            this.Logger.LogDebug("Attempting to authenticate using NWS4.");

            IHttpRequest httpRequest = new AspNetCoreHttpRequestWrapper(this.Context.Request);
            AuthData? authData;

            try
            {
                authData = this.ReadAuthData(httpRequest);
            }
            catch (Exception ex)
            {
                return AuthenticateResult.Fail(ex.Message);
            }

            if (authData is null)
                return AuthenticateResult.NoResult();

            using var authenticateRequest = new AuthenticateRequest(this.Signer, httpRequest, authData, this.Options);
            AuthenticateResult result;

            try
            {
                var sw = Stopwatch.StartNew();

                result = await this
                    .AuthenticateAsync(authenticateRequest, this.Context.RequestAborted)
                    .ConfigureAwait(false);

                sw.Stop();

                this.Logger.LogInformation("Authentication using NWS4 took {elapsedMilliseconds}ms.", sw.ElapsedMilliseconds);

                if (result.Succeeded)
                {
                    var body = await authenticateRequest
                        .ReadBodyAsync(this.Context.RequestAborted)
                        .ConfigureAwait(false);

                    var bodyStream = body != null ? new MemoryStream(body) : null;

                    this.Context.Request.Body = bodyStream;
                    this.Context.Request.ContentLength = bodyStream?.Length;
                }
            }
            catch (SignatureExpiredException ex)
            {
                this.Logger.LogWarning(ex, "NWS4 signature expired.");
                return AuthenticateResult.NoResult();
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Authentication failed for NWS4.");
                result = AuthenticateResult.Fail(ex);
            }

            return result;
        }

        [return: NotNull]
        protected abstract Task<AuthenticateResult> AuthenticateAsync(AuthenticateRequest request, CancellationToken cancellationToken);

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            var scheme = NWS4AuthenticationDefaults.AuthenticationScheme;

            Response.Headers["WWW-Authenticate"] = $"{scheme} realm=\"{this.Options.Realm}\", charset=\"UTF-8\"";
            return base.HandleChallengeAsync(properties);
        }

        protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
            => base.HandleForbiddenAsync(properties);
    }
}
