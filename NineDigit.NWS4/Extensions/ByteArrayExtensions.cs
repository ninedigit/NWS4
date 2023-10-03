using System;
using System.Text;

namespace NineDigit.NWS4;

public enum Casing
{
    Upper,
    Lower
}

public static class ByteArrayExtensions
{
    /// <summary>
    /// Helper to format a byte array into string.
    /// </summary>
    /// <param name="data">The data blob to process</param>
    /// <param name="casing">Determines result string casing.</param>
    /// <returns>String representation of the data.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> is null.</exception>
    public static string ToHexString(this byte[] data, Casing casing = Casing.Lower)
    {
        if (data is null)
            throw new ArgumentNullException(nameof(data));

        var format = casing == Casing.Lower ? "x2" : "X2";
        var stringBuilder = new StringBuilder(data.Length * 2);

        for (var i = 0; i < data.Length; i++)
            stringBuilder.Append(data[i].ToString(format));

        var result = stringBuilder.ToString();
        return result;
    }
}