using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.WebAssembly;
using Mythetech.Plugins.WebAssemblyHost;
using NotesManifest = Mythetech.Plugins.Notes.Manifest;
using GamesManifest = Mythetech.Plugins.Games.Manifest;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddHttpClient();
builder.Services.AddMudServices();
builder.Services.AddWebAssemblyServices();
builder.Services.AddMessageBus();
builder.Services.AddPluginFramework();
builder.Services.AddRuntimeEnvironment();

var host = builder.Build();

host.Services.UseMessageBus();
host.Services.UsePluginFramework();

await host.Services.UsePluginAsync(typeof(NotesManifest).Assembly);
await host.Services.UsePluginAsync(typeof(GamesManifest).Assembly);

await host.RunAsync();
