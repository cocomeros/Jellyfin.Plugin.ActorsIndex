using Jellyfin.Plugin.ActorsIndex.Channels;
using Jellyfin.Plugin.ActorsIndex.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.ActorsIndex;

/// <summary>
/// Registers plugin services.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<ActorsIndexService>();
        serviceCollection.AddSingleton<IChannel, ActorsIndexChannel>();
    }
}
