#:package Mythetech.Framework@0.1.14
#:property PublishAot=false

using System.Reflection;
using System.Text;
using System.Text.Json;
using Mythetech.Framework.Infrastructure.Plugins;

var solutionRoot = FindSolutionRoot();
if (solutionRoot == null)
{
    Console.Error.WriteLine("ERROR: Could not find solution root");
    Environment.Exit(1);
}

var cdnBase = Environment.GetEnvironmentVariable("PUBLIC_STORAGE_CDN_PREFIX");
if (string.IsNullOrEmpty(cdnBase))
{
    Console.Error.WriteLine("ERROR: PUBLIC_STORAGE_CDN_PREFIX environment variable is required");
    Environment.Exit(1);
}

var plugins = new List<PluginInfo>();

var srcDir = Path.Combine(solutionRoot, "src");
if (!Directory.Exists(srcDir))
{
    Console.Error.WriteLine($"ERROR: Source directory not found: {srcDir}");
    Environment.Exit(1);
}

var pluginDirs = Directory.GetDirectories(srcDir, "Mythetech.Plugins.*");
Console.WriteLine($"Found {pluginDirs.Length} plugin directory(ies)");
foreach (var pluginDir in pluginDirs)
{
    var projectName = Path.GetFileName(pluginDir);
    Console.WriteLine($"Processing: {projectName}");

    var configuration = Environment.GetEnvironmentVariable("PLUGIN_BUILD_CONFIGURATION")
        ?? (Directory.Exists(Path.Combine(pluginDir, "bin", "Release")) ? "Release" : "Debug");
    Console.WriteLine($"  Configuration: {configuration}");

    var dllPath = Path.Combine(pluginDir, "bin", configuration, "net10.0", $"{projectName}.dll");
    Console.WriteLine($"  Looking for DLL: {dllPath}");

    if (!File.Exists(dllPath))
    {
        var altPath = Path.Combine(pluginDir, "bin", configuration == "Debug" ? "Release" : "Debug", "net10.0", $"{projectName}.dll");
        if (File.Exists(altPath))
        {
            dllPath = altPath;
            Console.WriteLine($"NOTE: Using {Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(altPath)))} build for {projectName}");
        }
        else
        {
            Console.WriteLine($"WARNING: Plugin DLL not found: {dllPath}");
            Console.WriteLine($"  Run 'dotnet build {projectName}' first");
            continue;
        }
    }

    try
    {
        var assembly = Assembly.LoadFrom(dllPath);
        Console.WriteLine($"  Loaded assembly: {assembly.FullName}");
        var allTypes = assembly.GetTypes();
        Console.WriteLine($"  Found {allTypes.Length} type(s) in assembly");
        var manifestType = allTypes
            .FirstOrDefault(t => t.Name == "Manifest" && typeof(IPluginManifest).IsAssignableFrom(t));

        if (manifestType == null)
        {
            Console.WriteLine($"WARNING: Could not find Manifest class in {projectName}");
            var manifestTypes = allTypes.Where(t => t.Name == "Manifest").ToList();
            if (manifestTypes.Any())
            {
                Console.WriteLine($"  Found {manifestTypes.Count} type(s) named 'Manifest' but none implement IPluginManifest");
                foreach (var mt in manifestTypes)
                {
                    Console.WriteLine($"    - {mt.FullName} (implements IPluginManifest: {typeof(IPluginManifest).IsAssignableFrom(mt)})");
                }
            }
            continue;
        }

        var manifest = Activator.CreateInstance(manifestType) as IPluginManifest;
        if (manifest == null)
        {
            Console.WriteLine($"WARNING: Could not instantiate Manifest in {projectName}");
            continue;
        }

        var shortName = manifest.Id.Split('.').Last().ToLowerInvariant();
        var packageName = manifest.Id.Replace(".", "-");
        var zipName = $"{packageName}-latest.zip";
        var uri = $"{cdnBase.TrimEnd('/')}/release/plugins/{shortName}/{zipName}";

        plugins.Add(new PluginInfo
        {
            id = manifest.Id,
            name = manifest.Name,
            version = manifest.Version.ToString(),
            uri = uri
        });

        Console.WriteLine($"✓ Found plugin: {manifest.Name} ({manifest.Version})");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR: Failed to load {projectName}: {ex.Message}");
        Console.WriteLine($"  Exception type: {ex.GetType().FullName}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"  Inner exception: {ex.InnerException.Message}");
        }
        Console.WriteLine($"  Stack trace: {ex.StackTrace}");
    }
}

var json = new
{
    plugins = plugins.OrderBy(p => p.id).ToArray()
};

var jsonText = JsonSerializer.Serialize(json, new JsonSerializerOptions
{
    WriteIndented = true
});

var outputPath = Path.Combine(solutionRoot, "mythetech-plugin-registry.json");
File.WriteAllText(outputPath, jsonText, Encoding.UTF8);

Console.WriteLine($"✓ Generated plugins.json with {plugins.Count} plugin(s)");
Console.WriteLine($"  Output: {outputPath}");

string? FindSolutionRoot()
{
    var currentDir = Directory.GetCurrentDirectory();
    var directory = new DirectoryInfo(currentDir);

    while (directory != null)
    {
        if (directory.GetFiles("*.sln").Any())
        {
            return directory.FullName;
        }
        directory = directory.Parent;
    }

    return null;
}

class PluginInfo
{
    public string id { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
    public string version { get; set; } = string.Empty;
    public string uri { get; set; } = string.Empty;
}

