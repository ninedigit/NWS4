using System;
using System.Globalization;
using System.Net.Mime;

namespace NineDigit.NWS4;

internal static class HttpRequestExtensions
{
    public static bool IsChunked(this IHttpRequest request)
    {
        var xNdContentSha256 = request.Headers.GetValuesOrNull(Signer.XNDContentSHA256);
        var contentEncodings = request.Headers.GetValuesOrNull(HeaderNames.ContentEncoding);
        var contentType = request.Headers.GetValuesOrNull(HeaderNames.ContentType);
        var decodedContentLength = request.Headers.GetValuesOrNull(Signer.XNDDecodedContentLength);

        var isChunked = xNdContentSha256 != null && contentEncodings != null && contentType != null &&
                        StringComparer.Ordinal.Equals(xNdContentSha256, AuthorizationHeaderChunkedSigner.StreamingBodySha256) &&
                        contentEncodings.Contains(AuthorizationHeaderChunkedSigner.ContentEncodingNwsChunked, StringComparison.Ordinal) &&
                        StringComparer.Ordinal.Equals(contentType, MediaTypeNames.Text.Plain) &&
                        long.TryParse(decodedContentLength, NumberStyles.Integer, CultureInfo.InvariantCulture, out long contentLength);

        return isChunked;
    }
}