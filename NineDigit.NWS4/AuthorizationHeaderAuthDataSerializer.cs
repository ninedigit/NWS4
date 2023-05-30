using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Web;

namespace NineDigit.NWS4
{
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

            var timestamp = HttpUtility.UrlEncode(data.Timestamp);
            var signedHeaders = HttpUtility.UrlEncode(data.SignedHeaders);
            var authorizationParam = HttpUtility.UrlEncode(
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
                AuthParamStringFormat, HttpUtility.UrlDecode(authParts[1]));

            if (authParamParts.Length != 4)
                throw new FormatException(nameof(authorization));

            var timestamp = HttpUtility.UrlDecode(authParamParts[2]);
            var signedHeaders = HttpUtility.UrlDecode(authParamParts[1]);

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
            string pattern = "^" + Regex.Replace(template, @"\{[0-9]+\}", "(.*?)") + "$";

            Regex r = new Regex(pattern);
            Match m = r.Match(str);

            List<string> ret = new List<string>();

            for (int i = 1; i < m.Groups.Count; i++)
                ret.Add(m.Groups[i].Value);

            return ret.ToArray();
        }
    }
}
