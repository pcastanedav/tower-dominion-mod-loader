using MelonLoader;
using TDModLoader.Handlers.Http;

namespace TDModLoader;

public class Loader: MelonMod
{
    public override void OnInitializeMelon()
    {
        base.OnInitializeMelon();
        var server = new Server();
        _ = server.StartAsync();
        MelonLogger.Msg("Welcome to TDModLoader 2");
    }
}