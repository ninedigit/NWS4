using System;

namespace NineDigit.NWS4
{
    public sealed class AuthData
    {
        public AuthData(ComputeSignatureResult result)
        {
            if (result is null)
                throw new ArgumentNullException(nameof(result));
            
            this.Scheme = result.Scheme;
            this.Credential = result.PublicKey;
            this.SignedHeaders = result.SignedHeaderNames;
            this.Timestamp = result.Timestamp;
            this.Signature = result.Signature;
        }

        public AuthData(string scheme, string credential, string signedHeaders, string timestamp, string signature)
        {
            this.Scheme = scheme;
            this.Credential = credential;
            this.SignedHeaders = signedHeaders;
            this.Timestamp = timestamp;
            this.Signature = signature;
        }

        public string Scheme { get; }
        public string Credential { get; }
        public string SignedHeaders { get; }
        /// <summary>
        /// ISO format date time stamp
        /// </summary>
        public string Timestamp { get; }
        public string Signature { get; }

        public DateTime GetUtcDateTime()
            => AuthorizationHeaderSigner.ParseUtcDateTime(Timestamp);
    }
}
