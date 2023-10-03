using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.RegularExpressions;

namespace NineDigit.NWS4;

public sealed class AuthorizationHeaderAuthDataSerializer : IAuthDataSerializer
{
    private const string AuthStringFormat = "{0} {1}";
    private const string AuthParamStringFormat = "Credential={0},SignedHeaders={1},Timestamp={2},Signature={3}";

    public AuthData? Read(IHttpRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var authorization = request.Headers.FindAuthorization();
        var authData = Deserialize(authorization);

        return authData;
    }

    [return: NotNullIfNotNull("authorization")]
    public AuthData? Read(string? authorization)
        => Deserialize(authorization);

    public void Write(IHttpRequest request, AuthData data)
    {
        var authorization = Serialize(data);
        request.Headers.SetAuthorization(authorization);
    }

    public static string Serialize(AuthData data)
    {
        if (data is null)
            throw new ArgumentNullException(nameof(data));

        var timestamp = WebUtility.UrlEncode(data.Timestamp);
        var signedHeaders = WebUtility.UrlEncode(data.SignedHeaders);
        var authorizationParam = WebUtility.UrlEncode(
            string.Format(AuthParamStringFormat,
                data.Credential,
                signedHeaders,
                timestamp,
                data.Signature));

        var authorization = string.Format(AuthStringFormat, data.Scheme, authorizationParam);
        return authorization;
    }

    public static AuthData? Deserialize(string? authorization)
    {
        if (authorization is null)
            return null;

        var authParts = ParseFormat(AuthStringFormat, authorization);

        if (authParts.Length != 2)
            throw new FormatException(nameof(authorization));

        var authParamParts = ParseFormat(
            AuthParamStringFormat, WebUtility.UrlDecode(authParts[1]));

        if (authParamParts.Length != 4)
            throw new FormatException(nameof(authorization));

        var timestamp = WebUtility.UrlDecode(authParamParts[2]);
        var signedHeaders = WebUtility.UrlDecode(authParamParts[1]);

        var data = new AuthData(
            scheme: authParts[0],
            credential: authParamParts[0],
            signedHeaders: signedHeaders,
            timestamp: timestamp,
            signature: authParamParts[3]);

        return data;
    }

    private static string[] ParseFormat(string template, string str)
    {
        var pattern = "^" + Regex.Replace(template, @"\{[0-9]+\}", "(.*?)") + "$";

        var r = new Regex(pattern);
        var m = r.Match(str);

        var ret = new List<string>();

        for (var i = 1; i < m.Groups.Count; i++)
            ret.Add(m.Groups[i].Value);

        return ret.ToArray();
    }
}