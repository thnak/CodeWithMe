using System.Globalization;
using Blazored.Toast;
using BusinessModels.Resources;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using MudBlazor.Services;
using Web.Client.Authenticate;
using Web.Client.Services;
using Web.Client.Services.Http;
using Web.Client.Utils;

namespace Web.Client;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.Services.AddMudServices();
        builder.Services.AddBlazoredToast();
        builder.Services.AddSingleton<StateContainer>();
        builder.Services.AddAuthorizationCore();
        builder.Services.AddAuthenticationStateDeserialization();
        builder.Services.AddSingleton<AuthenticationStateProvider, PersistentAuthenticationStateProvider>();
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddScoped<ProtectedLocalStorage>();
        builder.Services.AddScoped<ProtectedSessionStorage>();
        builder.Services.AddLocalization();
        builder.Services.AddScoped(_ => new BaseHttpClientService(new HttpClient()
        {

#if DEBUG
            BaseAddress = new Uri("https://localhost:5217"),
#else
            BaseAddress = new Uri("https://thnakdevserver.ddns.net:5001"),
#endif
        }));
        builder.Services.AddScoped(_ => new HttpClient
        {
            BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
        });
        var host = builder.Build();

        var defaultCulture = AllowedCulture.SupportedCultures.Select(x => x.Name).ToArray().First();

        var js = host.Services.GetRequiredService<IJSRuntime>();
        var result = await js.GetCulture();
        var culture = CultureInfo.GetCultureInfo(result ?? defaultCulture);

        if (result == null) await js.SetCulture(defaultCulture);

        Thread.CurrentThread.CurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        await host.RunAsync();
    }
}