using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.ActorsIndex.Services;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.ActorsIndex.Channels;

/// <summary>
/// Jellyfin Channel that exposes the actors index on the home screen.
/// Shows actors as folders; opening an actor shows the movies they appear in.
/// </summary>
public class ActorsIndexChannel : IChannel, IHasCacheKey, IRequiresMediaInfoCallback
{
    private readonly ILibraryManager _libraryManager;
    private readonly ActorsIndexService _actorsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActorsIndexChannel"/> class.
    /// </summary>
    /// <param name="libraryManager">The Jellyfin library manager.</param>
    /// <param name="actorsService">The actors index service.</param>
    public ActorsIndexChannel(ILibraryManager libraryManager, ActorsIndexService actorsService)
    {
        _libraryManager = libraryManager;
        _actorsService = actorsService;
    }

    /// <inheritdoc />
    public string Name => "Indice Attori";

    /// <inheritdoc />
    public string Description => "Naviga gli attori presenti nella tua libreria Jellyfin.";

    /// <inheritdoc />
    public string DataVersion => "15";

    /// <inheritdoc />
    public string HomePageUrl => string.Empty;

    /// <inheritdoc />
    public ChannelParentalRating ParentalRating => ChannelParentalRating.GeneralAudience;

    /// <inheritdoc />
    public string GetCacheKey(string? userId) => "actorsindex_" + userId;

    /// <inheritdoc />
    public InternalChannelFeatures GetChannelFeatures()
    {
        return new InternalChannelFeatures
        {
            ContentTypes = new List<ChannelMediaContentType> { ChannelMediaContentType.Movie },
            MediaTypes = new List<ChannelMediaType> { ChannelMediaType.Video },
            SupportsContentDownloading = false,
        };
    }

    /// <inheritdoc />
    public bool IsEnabledFor(string userId) => true;

    /// <inheritdoc />
    public IEnumerable<ImageType> GetSupportedChannelImages()
        => new[] { ImageType.Primary };

    /// <inheritdoc />
    public Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken cancellationToken)
    {
        if (type != ImageType.Primary)
        {
            return Task.FromResult(new DynamicImageResponse { HasImage = false });
        }

        var stream = GetType().Assembly
            .GetManifestResourceStream("Jellyfin.Plugin.ActorsIndex.Images.channel_icon.png");

        if (stream is null)
        {
            return Task.FromResult(new DynamicImageResponse { HasImage = false });
        }

        return Task.FromResult(new DynamicImageResponse
        {
            HasImage = true,
            Stream = stream,
            Format = ImageFormat.Png,
        });
    }

    /// <inheritdoc />
    public Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
    {
        return Task.FromResult(
            string.IsNullOrEmpty(query.FolderId)
                ? BuildActorList()
                : BuildMovieList(query.FolderId));
    }

    /// <inheritdoc />
    public Task<IEnumerable<MediaSourceInfo>> GetChannelItemMediaInfo(string id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            return Task.FromResult(Enumerable.Empty<MediaSourceInfo>());
        }

        var item = _libraryManager.GetItemById(guid);
        if (item is Video video)
        {
            return Task.FromResult<IEnumerable<MediaSourceInfo>>(video.GetMediaSources(true));
        }

        return Task.FromResult(Enumerable.Empty<MediaSourceInfo>());
    }

    // ── private helpers ────────────────────────────────────────────────────

    private ChannelItemResult BuildActorList()
    {
        // Reuse ActorsIndexService to get actors sorted by appearances descending
        dynamic result = _actorsService.GetActorsIndex();
        var actors = (IEnumerable<dynamic>)((dynamic)result).actors;

        var items = actors.Select(a =>
        {
            Guid personGuid = Guid.TryParse((string?)a.personId, out var g) ? g : Guid.Empty;
            var personItem = personGuid != Guid.Empty
                ? _libraryManager.GetItemById(personGuid) as Person
                : null;

            return new ChannelItemInfo
            {
                Id = "ca:" + personGuid.ToString("N"),
                Name = (string)a.name + " (" + (int)a.appearances + ")",
                Overview = personItem?.Overview,
                Type = ChannelItemType.Folder,
                FolderType = ChannelFolderType.Container,
                ImageUrl = personItem?.ImageInfos
                    .FirstOrDefault(i => i.Type == ImageType.Primary)?.Path,
            };
        }).ToList();

        return new ChannelItemResult { Items = items, TotalRecordCount = items.Count };
    }

    private ChannelItemResult BuildMovieList(string personIdStr)
    {
        // Strip the "ca:" prefix added in BuildActorList to avoid GUID collision.
        const string Prefix = "ca:";
        var guidStr = personIdStr.StartsWith(Prefix, StringComparison.Ordinal)
            ? personIdStr.Substring(Prefix.Length)
            : personIdStr;
        if (!Guid.TryParse(guidStr, out var personGuid))
        {
            return new ChannelItemResult { Items = new List<ChannelItemInfo>() };
        }

        var movies = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series },
            PersonIds = new[] { personGuid },
            Recursive = true,
        });

        var items = movies.Select(m => new ChannelItemInfo
        {
            Id = m.Id.ToString("N"),
            Name = m.Name ?? string.Empty,
            Type = ChannelItemType.Media,
            ContentType = ChannelMediaContentType.Movie,
            MediaType = ChannelMediaType.Video,
            ProductionYear = m.ProductionYear,
            Overview = m.Overview,
            OfficialRating = m.OfficialRating,
            CommunityRating = m.CommunityRating,
            RunTimeTicks = m.RunTimeTicks,
            PremiereDate = m.PremiereDate,
            ProviderIds = new System.Collections.Generic.Dictionary<string, string>(
                m.ProviderIds, System.StringComparer.OrdinalIgnoreCase),
            ImageUrl = m.ImageInfos
                .FirstOrDefault(i => i.Type == ImageType.Primary)?.Path,
        }).ToList();

        return new ChannelItemResult { Items = items, TotalRecordCount = items.Count };
    }
}
