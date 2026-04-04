using System.Linq;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.ActorsIndex.Services;

/// <summary>
/// Provides Actors Index data access.
/// </summary>
public class ActorsIndexService
{
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActorsIndexService"/> class.
    /// </summary>
    /// <param name="libraryManager">The Jellyfin library manager.</param>
    public ActorsIndexService(ILibraryManager libraryManager)
    {
        _libraryManager = libraryManager;
    }

    /// <summary>
    /// Returns whether the service is ready.
    /// </summary>
    /// <returns>A test payload.</returns>
    public object GetStatus()
    {
        return new
        {
            status = "ok",
            libraryManagerAvailable = _libraryManager is not null
        };
    }

    /// <summary>
    /// Returns the actors index: each actor with their appearance count and related items.
    /// </summary>
    /// <returns>A sorted list of actors with occurrence counts.</returns>
    public object GetActorsIndex()
    {
        var config = Plugin.Instance?.Configuration ?? new Configuration.PluginConfiguration();

        // Get all movies and series from the library
        var items = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series },
            Recursive = true
        });

        // Build actor-to-items mapping by querying people for each item
        var actorDict = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<(System.Guid ItemId, string ItemName, string? Role)>>(
            System.StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            foreach (var person in _libraryManager.GetPeople(item))
            {
                if (person.Type != PersonKind.Actor || person.Name is null)
                {
                    continue;
                }

                if (!actorDict.TryGetValue(person.Name, out var appearances))
                {
                    appearances = new System.Collections.Generic.List<(System.Guid, string, string?)>();
                    actorDict[person.Name] = appearances;
                }

                appearances.Add((item.Id, item.Name, person.Role));
            }
        }

        // Resolve person item IDs for thumbnail images (one bulk query)
        var personIdByName = _libraryManager
            .GetItemList(new InternalItemsQuery { IncludeItemTypes = new[] { BaseItemKind.Person }, Recursive = true })
            .Where(p => p.Name is not null)
            .GroupBy(p => p.Name!, System.StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id.ToString(), System.StringComparer.OrdinalIgnoreCase);

        var actors = actorDict
            .Select(kvp =>
            {
                personIdByName.TryGetValue(kvp.Key, out var personId);
                return new
                {
                    name = kvp.Key,
                    appearances = kvp.Value.Count,
                    personId,
                    items = kvp.Value
                        .Select(x => new { itemId = x.ItemId, itemName = x.ItemName, role = x.Role })
                        .ToArray()
                };
            })
            .Where(a => a.appearances >= config.MinimumAppearances)
            .OrderByDescending(a => a.appearances)
            .ThenBy(a => a.name)
            .ToArray();

        return new
        {
            status = "ok",
            totalActors = actors.Length,
            actors
        };
    }

    /// <summary>
    /// Returns simple library statistics.
    /// </summary>
    /// <returns>Counts of root children in the Jellyfin library.</returns>
    public object GetLibraryStats()
    {
        var rootFolder = _libraryManager.RootFolder;
        var directChildren = rootFolder.Children.ToList();

        int libraryCount = directChildren.Count(item => item.IsFolder);
        int movieCount = directChildren.Count(item => item is Movie);
        int seriesCount = directChildren.Count(item => item is Series);

        return new
        {
            status = "ok",
            rootFolderName = rootFolder.Name,
            totalDirectChildren = directChildren.Count,
            libraryFolders = libraryCount,
            movieCount,
            seriesCount,
            sampleChildren = directChildren.Take(5).Select(i => new
            {
                i.Id,
                i.Name,
                itemType = i.GetType().Name
            }).ToArray()
        };
    }
}
