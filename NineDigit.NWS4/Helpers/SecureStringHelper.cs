using System;
using System.Runtime.InteropServices;
using System.Security;

namespace NineDigit.NWS4;

public static class SecureStringHelper
{
    public static SecureString CreateSecureString(string value)
    {
        var secureString = new SecureString();

        foreach (var c in value)
            secureString.AppendChar(c);

        secureString.MakeReadOnly();

        return secureString;
    }
    
    public static string? CreateUnsecureString(SecureString self)
    {
        var valuePtr = IntPtr.Zero;

        try
        {
            valuePtr = Marshal.SecureStringToGlobalAllocUnicode(self);
            return Marshal.PtrToStringUni(valuePtr);
        }
        finally
        {
            Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
        }
    }
}