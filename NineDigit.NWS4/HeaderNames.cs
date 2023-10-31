namespace NineDigit.NWS4;

/*
 * Note: This class is a subset of HeaderNames class of
 * https://github.com/dotnet/aspnetcore/blob/main/src/Http/Headers/src/HeaderNames.cs and was created to avoid
 * referencing the Microsoft.AspNetCore.App.
 */
internal sealed class HeaderNames
{
    /// <summary>Gets the <c>Authorization</c> HTTP header name.</summary>
    public static readonly string Authorization = "Authorization";
    
    /// <summary>Gets the <c>Content-Encoding</c> HTTP header name.</summary>
    public static readonly string ContentEncoding = "Content-Encoding";
    
    /// <summary>Gets the <c>Content-Length</c> HTTP header name.</summary>
    public static readonly string ContentLength = "Content-Length";
    
    /// <summary>Gets the <c>Content-Type</c> HTTP header name.</summary>
    public static readonly string ContentType = "Content-Type";
    
    /// <summary>Gets the <c>Host</c> HTTP header name.</summary>
    public static readonly string Host = "Host";
}