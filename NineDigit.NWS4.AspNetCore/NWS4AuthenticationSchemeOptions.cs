﻿using Microsoft.AspNetCore.Authentication;
using System;

namespace NineDigit.NWS4.AspNetCore;

public class NWS4AuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    private TimeSpan _requestTimeWindow;
        
    public NWS4AuthenticationSchemeOptions()
    {
        RequestTimeWindow = TimeSpan.FromSeconds(300);
    }

    public TimeSpan RequestTimeWindow
    {
        get => _requestTimeWindow;
        set
        {
            if (value.Ticks < 0)
                throw new ArgumentOutOfRangeException(nameof(value));

            _requestTimeWindow = value;
        }
    }

    public string Realm { get; set; }
}
    
public class NWS4AuthenticationSchemeOptions<TSignerOptions> : NWS4AuthenticationSchemeOptions
    where TSignerOptions : SignerOptions, new()
{
    public NWS4AuthenticationSchemeOptions()
    {
        Signer = new TSignerOptions();
    }
        
    public TSignerOptions Signer { get; }
}