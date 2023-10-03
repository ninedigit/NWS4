namespace NineDigit.NWS4;

public interface IAuthDataSerializer
{
    public AuthData? Read(IHttpRequest request);
    public void Write(IHttpRequest request, AuthData data);
}