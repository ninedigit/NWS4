using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;

namespace NineDigit.NWS4.AspNetCore;

public abstract class NWS4AuthenticationHandler<TSigner, TOptions> : AuthenticationHandler<TOptions>
    where TSigner : Signer
    where TOptions : NWS4AuthenticationSchemeOptions, new()
{
    private TSigner? _signer;
    private readonly object _signerSyncRoot = new();
        
    public NWS4AuthenticationHandler(
        IOptionsMonitor<TOptions> options,
        ILoggerFactory loggerFactory,
        ISystemClock clock,
        UrlEncoder encoder
    ) : base(options, loggerFactory, encoder, clock)
    {
        LoggerFactory = loggerFactory;
    }

    protected TSigner Signer
        => GetOrCreateSigner();
        
    protected ILoggerFactory LoggerFactory { get; }

    protected abstract TSigner CreateSigner(TOptions options);
    protected abstract AuthData? ReadAuthData(IHttpRequest request);

    private TSigner GetOrCreateSigner()
        => GetOrCreateSigner(Options);
        
    private TSigner GetOrCreateSigner(TOptions options)
    {
        lock (_signerSyncRoot)
        {
            if (_signer is null)
                _signer = CreateSigner(options);

            return _signer;
        }
    }
        
    protected sealed override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        Logger.LogDebug("Attempting to authenticate using NWS4");

        IHttpRequest httpRequest = new AspNetCoreHttpRequestWrapper(Context.Request);
        AuthData? authData;

        try
        {
            authData = ReadAuthData(httpRequest);
        }
        catch (Exception ex)
        {
            return AuthenticateResult.Fail(ex.Message);
        }

        if (authData is null)
            return AuthenticateResult.NoResult();

        using var authenticateRequest = new AuthenticateRequest(Signer, httpRequest, authData, Options);
        AuthenticateResult result;

        try
        {
            var sw = Stopwatch.StartNew();

            result = await this
                .AuthenticateAsync(authenticateRequest, Context.RequestAborted)
                .ConfigureAwait(false);

            sw.Stop();

            Logger.LogInformation("Authentication using NWS4 took {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);

            if (result.Succeeded)
            {
                var body = await authenticateRequest.ReadBodyAsync(Context.RequestAborted).ConfigureAwait(false);

                if (body != null)
                {
                    var bodyStream = new MemoryStream(body);

                    Context.Request.Body = bodyStream;
                    Context.Request.ContentLength = bodyStream.Length;
                }
            }
        }
        catch (SignatureExpiredException ex)
        {
            Logger.LogWarning(ex, "NWS4 signature expired");
            return AuthenticateResult.NoResult();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Authentication failed for NWS4");
            result = AuthenticateResult.Fail(ex);
        }

        return result;
    }

    protected abstract Task<AuthenticateResult> AuthenticateAsync(AuthenticateRequest request, CancellationToken cancellationToken);

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        var scheme = NWS4AuthenticationDefaults.AuthenticationScheme;

        Response.Headers["WWW-Authenticate"] = $"{scheme} realm=\"{Options.Realm}\", charset=\"UTF-8\"";
        return base.HandleChallengeAsync(properties);
    }
}