using TDModLoader.Handlers.Utils;

namespace TDModLoader;

using Handlers.Http;

public static class Program
{
    public static async Task Main() {
        var server = new Server();
        await server.StartAsync();
    }
    
}