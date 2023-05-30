using NineDigit.NWS4.AspNetCore;
using NineDigit.NWS4.AspNetCore.Examples.API.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(builder =>
    {
        builder.DefaultScheme = NWS4AuthenticationDefaults.AuthenticationScheme;
    })
    .AddNWS4Authentication<NWS4AuthenticationHandler>(opts =>
    {
        opts.Signer.AllowForwardedHostHeader = true;
    });

builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();