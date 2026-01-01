using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Mythetech.Framework.Desktop;
using Mythetech.Framework.Desktop.Photino;
using Mythetech.Framework.Infrastructure.Files;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Plugins.Notes;
using Mythetech.Plugins.PluginTester;
using Photino.Blazor;
using NotesManifest = Mythetech.Plugins.Notes.Manifest;
using GamesManifest = Mythetech.Plugins.Games.Manifest;

var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);

appBuilder.Services.AddLogging();
appBuilder.Services.AddMudServices();
appBuilder.Services.AddMessageBus();
appBuilder.Services.AddPhotinoServices();
appBuilder.Services.AddLinkOpenService();
appBuilder.Services.AddPluginStorage();
appBuilder.Services.AddPluginFramework();
appBuilder.Services.AddShowFileService();

appBuilder.RootComponents.Add<App>("#app");

var app = appBuilder.Build();

app.MainWindow.SetTitle("Plugin Tester");
app.MainWindow.SetUseOsDefaultSize(false);
app.MainWindow.SetSize(1920, 1080);
app.MainWindow.SetUseOsDefaultLocation(true);

app.RegisterProvider(app.Services);
app.Services.UseMessageBus(typeof(Program).Assembly, typeof(IConsumer<>).Assembly);

app.Services.UsePlugins();
app.Services.UsePlugin(typeof(NotesManifest).Assembly);
app.Services.UsePlugin(typeof(GamesManifest).Assembly);

app.Run();
