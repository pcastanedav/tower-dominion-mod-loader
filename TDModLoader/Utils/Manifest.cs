using TDModLoader.Handlers.Utils;

namespace TDModLoader.Utils;

using System.Reflection;

public static class Manifest
{
    private static readonly Assembly Assembly = Assembly.GetExecutingAssembly();
    private static readonly string AssemblyName = Assembly.GetName().Name!;
    private static readonly Dictionary<string, Func<Stream>> LoadedResources = Assembly
        .GetManifestResourceNames()
        .Select(name => KeyValuePair.Create(name.ToLowerInvariant(), () => Assembly.GetManifestResourceStream(name)))
        .ToDictionary(t => t.Key, t => t.Value)!;
    
    public static ManifestStream GetResource(Uri path)
    {
        var resourceName = PathToResourceName(path);
        if (!LoadedResources.ContainsKey(resourceName))
            throw new FileNotFoundException($"Resource not found:{resourceName}");
        var resourceStream = LoadedResources[resourceName];
        return new ManifestStream() { Stream = resourceStream(), ContentType = Mime.GetMimeType(resourceName) };
    }
    
    private static string PathToResourceName(Uri path)
    {
        return $"{AssemblyName}.{string.Join('.', path.Segments.Skip(1).Select(s => s.Trim('/')))}".ToLowerInvariant();
    }

    public class ManifestStream
    {
        public Stream Stream { get; init; }
        public string ContentType { get; init; } = "application/octet-stream";
    }

}