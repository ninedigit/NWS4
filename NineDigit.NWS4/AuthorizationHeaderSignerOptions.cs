using System;

namespace NineDigit.NWS4;

public class AuthorizationHeaderSignerOptions : SignerOptions
{
    public static readonly string DefaultForwardedHostHeaderName = HttpRequestHeaderNames.XForwardedHost;
        
    private string _forwardedHostHeaderName = DefaultForwardedHostHeaderName;
    
    /// <summary>
    /// Determines whether X-Forwarded-Host headers will be used during signature validation.
    /// </summary>
    public bool AllowForwardedHostHeader { get; set; }

    public string ForwardedHostHeaderName
    {
        get => _forwardedHostHeaderName;
        set
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Value can not be null or empty.", nameof(value));

            _forwardedHostHeaderName = value;
        }
    }
}