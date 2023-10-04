using Microsoft.AspNetCore.Authentication;
using NineDigit.NWS4.AspNetCore;
using System;

namespace Microsoft.Extensions.DependencyInjection;

public static class NWS4AuthenticationExtensions
{
    public static AuthenticationBuilder AddNWS4UsingAuthorizationHeader<THandler>(
        this AuthenticationBuilder builder
    ) where THandler : NWS4AuthenticationHeaderHandler
        => AddNWS4UsingAuthorizationHeader<THandler>(builder, _ => { });

    public static AuthenticationBuilder AddNWS4UsingAuthorizationHeader<THandler>(
        this AuthenticationBuilder builder,
        Action<NWS4AuthenticationHeaderSchemeOptions> configureOptions
    ) where THandler : NWS4AuthenticationHeaderHandler
        => AddNWS4<THandler, NWS4AuthenticationHeaderSchemeOptions>(builder, configureOptions);
    
    //
    
    public static AuthenticationBuilder AddNWS4UsingAuthorizationHeaderChunked<THandler>(
        this AuthenticationBuilder builder
    ) where THandler : NWS4AuthenticationHeaderChunkedHandler
        => AddNWS4UsingAuthorizationHeaderChunked<THandler>(builder, _ => { });

    public static AuthenticationBuilder AddNWS4UsingAuthorizationHeaderChunked<THandler>(
        this AuthenticationBuilder builder,
        Action<NWS4AuthenticationHeaderChunkedSchemeOptions> configureOptions
    ) where THandler : NWS4AuthenticationHeaderChunkedHandler
        => AddNWS4<THandler, NWS4AuthenticationHeaderChunkedSchemeOptions>(builder, configureOptions);
    
    //
    
    public static AuthenticationBuilder AddNWS4<THandler, TOptions>(
        this AuthenticationBuilder builder
    )
        where THandler : AuthenticationHandler<TOptions>
        where TOptions : NWS4AuthenticationSchemeOptions, new()
        => AddNWS4<THandler, TOptions>(builder, _ => { });

    public static AuthenticationBuilder AddNWS4<THandler, TOptions>(
        this AuthenticationBuilder builder,
        Action<TOptions> configureOptions
    )
        where THandler : AuthenticationHandler<TOptions>
        where TOptions : NWS4AuthenticationSchemeOptions, new()
    {
        builder.Services.PostConfigure<TOptions>(PostConfigure);

        return builder.AddScheme<TOptions, THandler>(
            NWS4AuthenticationDefaults.AuthenticationScheme, configureOptions);
    }
    
    //////////
    
    [Obsolete("Use AddNWS4UsingAuthorizationHeaderChunked instead.")]
    public static AuthenticationBuilder AddNWS4Authentication<THandler>(
        this AuthenticationBuilder builder
    ) where THandler : NWS4AuthenticationHeaderChunkedHandler
        => AddNWS4Authentication<THandler>(builder, _ => { });

    [Obsolete("Use AddNWS4UsingAuthorizationHeaderChunked instead.")]
    public static AuthenticationBuilder AddNWS4Authentication<THandler>(
        this AuthenticationBuilder builder,
        Action<NWS4AuthenticationHeaderChunkedSchemeOptions> configureOptions
    ) where THandler : NWS4AuthenticationHeaderChunkedHandler
        => AddNWS4Authentication<THandler, NWS4AuthenticationHeaderChunkedSchemeOptions>(builder, configureOptions);
        
    //
    
    [Obsolete("Use AddNWS4 instead.")]
    public static AuthenticationBuilder AddNWS4Authentication<THandler, TOptions>(
        this AuthenticationBuilder builder
    )
        where THandler : AuthenticationHandler<TOptions>
        where TOptions : NWS4AuthenticationSchemeOptions, new()
        => AddNWS4Authentication<THandler, TOptions>(builder, _ => { });

    [Obsolete("Use AddNWS4 instead.")]
    public static AuthenticationBuilder AddNWS4Authentication<THandler, TOptions>(
        this AuthenticationBuilder builder,
        Action<TOptions> configureOptions
    )
        where THandler : AuthenticationHandler<TOptions>
        where TOptions : NWS4AuthenticationSchemeOptions, new()
        => AddNWS4<THandler, TOptions>(builder, configureOptions);

    private static void PostConfigure(NWS4AuthenticationSchemeOptions options)
    {
        //if (string.IsNullOrEmpty(options.Realm))
        //    throw new InvalidOperationException("Realm must be provided in options.");
    }
}