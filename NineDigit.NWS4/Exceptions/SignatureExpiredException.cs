using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace NineDigit.NWS4;

[Serializable]
public class SignatureExpiredException : Exception
{
    public SignatureExpiredException()
        : this(GetMessage())
    {
    }
    
    public SignatureExpiredException(string message)
        : this(message, innerException: null)
    {
    }

    public SignatureExpiredException(string message, Exception? innerException = null)
        : this(message, currentDateTime: null, signatureDateTime: null, validityInterval: null, innerException)
    {
    }

    public SignatureExpiredException(
        DateTime? currentDateTime,
        DateTime? signatureDateTime,
        TimeSpan? validityInterval)
        : this(GetMessage(currentDateTime, signatureDateTime, validityInterval), currentDateTime, signatureDateTime, validityInterval)
    {
    }

    public SignatureExpiredException(
        string message,
        DateTime? currentDateTime,
        DateTime? signatureDateTime,
        TimeSpan? validityInterval)
        : this(message, currentDateTime, signatureDateTime, validityInterval, innerException: null)
    {
    }

    public SignatureExpiredException(
        string message,
        DateTime? currentDateTime,
        DateTime? signatureDateTime,
        TimeSpan? validityInterval,
        Exception? innerException)
        : base(message, innerException)
    {
        CurrentDateTime = currentDateTime;
        SignatureDateTime = signatureDateTime;
        ValidityInterval = validityInterval;
    }

    protected SignatureExpiredException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
    
    public DateTime? CurrentDateTime { get; }
    public DateTime? SignatureDateTime { get; }
    public TimeSpan? ValidityInterval { get; }
    
    private static string GetMessage(
        DateTime? currentDateTime = null, DateTime? signatureDateTime = null, TimeSpan? validityInterval = null)
    {
        var values = new List<string>(3);
        
        if (currentDateTime.HasValue)
            values.Add($"Current time: {currentDateTime.Value}");
        
        if (signatureDateTime.HasValue)
            values.Add($"Signature time: {signatureDateTime.Value}");
        
        if (validityInterval.HasValue)
            values.Add($"Validity interval: {validityInterval}");
        
        var message = "Signature has expired.";

        if (values.Count > 0)
            message += " " + string.Join(", ", values) + ".";
        
        return message;
    }
}