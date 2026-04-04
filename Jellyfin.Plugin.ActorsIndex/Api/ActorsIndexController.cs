using System;
using System.Globalization;
using System.IO;
using Jellyfin.Plugin.ActorsIndex.Configuration;
using Jellyfin.Plugin.ActorsIndex.Services;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.ActorsIndex.Api;

/// <summary>
/// Simple API for Actors Index.
/// </summary>
[ApiController]
[Route("ActorsIndex")]
public class ActorsIndexController : ControllerBase
{
    private const string InjectionTag = "\n    <!-- ActorsIndex --><script src=\"/ActorsIndex/ui-button.js\"></script><!-- /ActorsIndex -->";

    private static readonly string[] WebRootCandidates =
    [
        "/usr/share/jellyfin/web",
        "/usr/lib/jellyfin/web",
        "/opt/jellyfin/web",
    ];

    private readonly ActorsIndexService _actorsIndexService;
    private readonly ILibraryManager _libraryManager;
    private readonly IProviderManager _providerManager;
    private readonly IFileSystem _fileSystem;
    private readonly IApplicationPaths _appPaths;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActorsIndexController"/> class.
    /// </summary>
    /// <param name="actorsIndexService">The actors index service.</param>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="providerManager">The provider manager.</param>
    /// <param name="fileSystem">The file system.</param>
    /// <param name="appPaths">The application paths.</param>
    public ActorsIndexController(
        ActorsIndexService actorsIndexService,
        ILibraryManager libraryManager,
        IProviderManager providerManager,
        IFileSystem fileSystem,
        IApplicationPaths appPaths)
    {
        _actorsIndexService = actorsIndexService;
        _libraryManager = libraryManager;
        _providerManager = providerManager;
        _fileSystem = fileSystem;
        _appPaths = appPaths;
    }

    /// <summary>
    /// Returns a simple ping response.
    /// </summary>
    /// <returns>A simple JSON payload.</returns>
    [HttpGet("ping")]
    public ActionResult<object> Ping()
    {
        return Ok(new
        {
            status = "ok",
            plugin = "Actors Index"
        });
    }

    /// <summary>
    /// Returns the current plugin configuration.
    /// </summary>
    /// <returns>The current configuration values.</returns>
    [HttpGet("config")]
    public ActionResult<object> GetConfig()
    {
        PluginConfiguration config = Plugin.Instance?.Configuration ?? new PluginConfiguration();

        return Ok(new
        {
            enabled = config.Enabled,
            minimumAppearances = config.MinimumAppearances,
            showRoleName = config.ShowRoleName
        });
    }

    /// <summary>
    /// Returns the current service status.
    /// </summary>
    /// <returns>A test payload from the service.</returns>
    [HttpGet("service-status")]
    public ActionResult<object> GetServiceStatus()
    {
        return Ok(_actorsIndexService.GetStatus());
    }

    /// <summary>
    /// Returns the actors index with appearance counts.
    /// </summary>
    /// <returns>Sorted list of actors with item occurrences.</returns>
    [HttpGet("actors-index")]
    public ActionResult<object> GetActorsIndex()
    {
        return Ok(_actorsIndexService.GetActorsIndex());
    }

    /// <summary>
    /// Returns simple library statistics.
    /// </summary>
    /// <returns>Library counts.</returns>
    [HttpGet("library-stats")]
    public ActionResult<object> GetLibraryStats()
    {
        return Ok(_actorsIndexService.GetLibraryStats());
    }

    /// <summary>
    /// Deletes all channel items from the DB and clears the disk cache so they are
    /// recreated from scratch on the next channel browse (picking up refreshed images).
    /// </summary>
    /// <returns>A summary of the operations performed.</returns>
    [HttpPost("refresh-channel")]
    public ActionResult<object> RefreshChannel()
    {
        // 1. Compute the channel internal ID
        var channelId = _libraryManager.GetNewItemId("Channel Indice Attori", typeof(Channel));

        // 2. Delete ALL channel items from the DB so they are recreated fresh.
        //    ImageUrl is only written once (on first creation); deletion is the only way
        //    to force Jellyfin to re-read the current Person/Movie image paths.
        var items = _libraryManager.GetItemList(new MediaBrowser.Controller.Entities.InternalItemsQuery
        {
            ChannelIds = new[] { channelId },
        });

        var deleteOpts = new MediaBrowser.Controller.Library.DeleteOptions
        {
            DeleteFileLocation = false,
        };

        foreach (var item in items)
        {
            _libraryManager.DeleteItem(item, deleteOpts);
        }

        // 3. Delete the disk cache so Jellyfin re-calls GetChannelItems immediately.
        var cacheDir = Path.Combine(
            _appPaths.CachePath,
            "channels",
            channelId.ToString("N", CultureInfo.InvariantCulture));
        var cacheCleared = false;
        if (Directory.Exists(cacheDir))
        {
            Directory.Delete(cacheDir, recursive: true);
            cacheCleared = true;
        }

        return Ok(new
        {
            cacheCleared,
            itemsDeleted = items.Count,
            channelId = channelId.ToString("N", CultureInfo.InvariantCulture),
        });
    }

    /// <summary>
    /// Returns a Jellyfin plugin repository manifest for this plugin.
    /// Register the URL of this endpoint as a custom repository in Jellyfin
    /// to suppress the "PluginLoadRepoError" warning on the plugin details page.
    /// </summary>
    /// <returns>A valid Jellyfin plugin repository manifest.</returns>
    [HttpGet("repository")]
    public ActionResult GetRepository()
    {
        var plugin = Plugin.Instance;
        var version = plugin?.Version.ToString() ?? "1.0.0.0";
        var guid = plugin?.Id.ToString() ?? "17d0c3ac-c0d1-48d7-ba4e-57cb9dac7fa2";

        var manifest = new[]
        {
            new
            {
                category = "General",
                description = "Scansiona la libreria Jellyfin e costruisce un indice di tutti gli attori con foto, ricerca e paginazione.",
                guid,
                imageUrl = (string?)null,
                name = "Actors Index",
                overview = "Sfoglia tutti gli attori della tua libreria con conteggio apparizioni.",
                owner = "cocom",
                versions = new[]
                {
                    new
                    {
                        checksum = (string?)null,
                        changelog = "1.0.0 - Versione iniziale.",
                        targetAbi = "10.11.0.0",
                        sourceUrl = (string?)null,
                        timestamp = "2026-04-04T00:00:00Z",
                        version
                    }
                }
            }
        };

        return new JsonResult(manifest);
    }

    // ── UI injection ───────────────────────────────────────────────────────

    private static string? FindIndexHtml()
    {
        foreach (var dir in WebRootCandidates)
        {
            var p = Path.Combine(dir, "index.html");
            if (System.IO.File.Exists(p))
            {
                return p;
            }
        }

        return null;
    }

    /// <summary>
    /// Patches Jellyfin's index.html to inject the actors-index floating button.
    /// </summary>
    /// <returns>Operation result.</returns>
    [HttpPost("inject-ui")]
    public ActionResult InjectUi()
    {
        var indexPath = FindIndexHtml();
        if (indexPath is null)
        {
            return NotFound(new { error = "index.html non trovato. Cercato in: " + string.Join(", ", WebRootCandidates) });
        }

        var html = System.IO.File.ReadAllText(indexPath);
        if (html.Contains("<!-- ActorsIndex -->", StringComparison.Ordinal))
        {
            return Ok(new { status = "already_injected", path = indexPath });
        }

        var patched = html.Replace("</body>", InjectionTag + "\n</body>", StringComparison.Ordinal);
        if (string.Equals(patched, html, StringComparison.Ordinal))
        {
            return StatusCode(500, new { error = "Tag </body> non trovato in index.html" });
        }

        System.IO.File.WriteAllText(indexPath, patched);
        return Ok(new { status = "ok", path = indexPath });
    }

    /// <summary>
    /// Removes the injected script tag from Jellyfin's index.html.
    /// </summary>
    /// <returns>Operation result.</returns>
    [HttpPost("remove-ui")]
    public ActionResult RemoveUi()
    {
        var indexPath = FindIndexHtml();
        if (indexPath is null)
        {
            return NotFound(new { error = "index.html non trovato" });
        }

        var html = System.IO.File.ReadAllText(indexPath);
        if (!html.Contains("<!-- ActorsIndex -->", StringComparison.Ordinal))
        {
            return Ok(new { status = "not_injected" });
        }

        var patched = html.Replace(InjectionTag + "\n", string.Empty, StringComparison.Ordinal);
        System.IO.File.WriteAllText(indexPath, patched);
        return Ok(new { status = "ok", path = indexPath });
    }

    /// <summary>
    /// Serves the floating-button JavaScript injected into Jellyfin's web UI.
    /// </summary>
    /// <returns>JavaScript content.</returns>
    [HttpGet("ui-button.js")]
    [AllowAnonymous]
    public ContentResult GetUiButtonJs()
    {
        var js = @"(function(){
  'use strict';
  var ID='ai-fab';
  function nav(){
    var p='/configurationpage?name=ActorsBrowse';
    if(window.Emby&&window.Emby.Page&&window.Emby.Page.show){window.Emby.Page.show(p);}
    else{window.location.hash=p;}
  }
  function add(){
    if(document.getElementById(ID))return;
    var b=document.createElement('button');
    b.id=ID;
    b.title='Indice Attori';
    b.innerHTML='&#127914;';
    b.style.cssText='position:fixed;bottom:28px;right:28px;width:56px;height:56px;border-radius:50%;background:#0097e6;border:none;color:#fff;font-size:1.5em;cursor:pointer;z-index:9999;box-shadow:0 4px 18px rgba(0,0,0,0.5);display:flex;align-items:center;justify-content:center;';
    b.addEventListener('click',nav);
    document.body.appendChild(b);
  }
  if(document.body){add();}else{document.addEventListener('DOMContentLoaded',add);}
  new MutationObserver(add).observe(document.documentElement,{childList:true,subtree:false});
})();";
        return Content(js, "application/javascript");
    }

    /// <summary>
    /// Queues a full image metadata refresh for all Person items in the library.
    /// Use this to fix actor cards showing wrong images (e.g. movie posters).
    /// </summary>
    /// <returns>A summary of the operation.</returns>
    [HttpPost("refresh-people")]
    public ActionResult<object> RefreshPeople()
    {
        var people = _libraryManager.GetItemList(new MediaBrowser.Controller.Entities.InternalItemsQuery
        {
            IncludeItemTypes = new[] { Jellyfin.Data.Enums.BaseItemKind.Person },
            Recursive = true,
        });

        var refreshOpts = new MetadataRefreshOptions(new DirectoryService(_fileSystem))
        {
            MetadataRefreshMode = MetadataRefreshMode.FullRefresh,
            ImageRefreshMode = MetadataRefreshMode.FullRefresh,
            ReplaceAllImages = true,
            ReplaceAllMetadata = true,
        };

        foreach (var person in people)
        {
            _providerManager.QueueRefresh(person.Id, refreshOpts, RefreshPriority.Normal);
        }

        return Ok(new
        {
            peopleQueued = people.Count,
        });
    }
}
