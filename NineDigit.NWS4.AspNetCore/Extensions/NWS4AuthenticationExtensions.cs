using Microsoft.AspNetCore.Authentication;
using NineDigit.NWS4.AspNetCore;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class NWS4AuthenticationExtensions
    {
        public static AuthenticationBuilder AddNWS4Authentication<THandler>(
            this AuthenticationBuilder builder
        ) where THandler : NWS4AuthenticationHeaderChunkedHandler
            => AddNWS4Authentication<THandler>(builder, _ => { });

        public static AuthenticationBuilder AddNWS4Authentication<THandler>(
            this AuthenticationBuilder builder,
            Action<NWS4AuthenticationHeaderChunkedSchemeOptions> configureOptions
        ) where THandler : NWS4AuthenticationHeaderChunkedHandler
            => AddNWS4Authentication<THandler, NWS4AuthenticationHeaderChunkedSchemeOptions>(builder, configureOptions);
        
        //
        
        public static AuthenticationBuilder AddNWS4Authentication<THandler, TOptions>(
            this AuthenticationBuilder builder
        )
            where THandler : AuthenticationHandler<TOptions>
            where TOptions : NWS4AuthenticationSchemeOptions, new()
            => AddNWS4Authentication<THandler, TOptions>(builder, _ => { });

        public static AuthenticationBuilder AddNWS4Authentication<THandler, TOptions>(
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

        private static void PostConfigure(NWS4AuthenticationSchemeOptions options)
        {
            //if (string.IsNullOrEmpty(options.Realm))
            //    throw new InvalidOperationException("Realm must be provided in options.");
        }
    }
}
