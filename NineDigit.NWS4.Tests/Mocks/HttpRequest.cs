namespace NineDigit.NWS4.AspNetCore
{
    public class HttpRequest : IHttpRequest
    {
        public HttpRequest()
        {
            this.Method = HttpMethod.Get.Method;
            this.Headers = new HttpRequestHeaders();
        }

        public HttpRequest(Stream stream)
            : this()
        {
            var position = stream.Position;

            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            stream.Position = position;
        }

        public Uri? RequestUri { get; set; }

        public string Method { get; set; }

        public IHttpRequestHeaders Headers { get; set; }

        public byte[]? Body { get; set; }

        public Task<byte[]?> ReadBodyAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(this.Body);
    }
}
