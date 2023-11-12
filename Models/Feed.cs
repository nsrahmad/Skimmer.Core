using System.Data;

using Nanorm;

namespace Skimmer.Core.Models;

public class Feed : IDataRecordMapper<Feed>
{  
    public static Feed GetFeed(string title, string description, Uri link, int parentId, ICollection<FeedItem>? items, string imageUrl)
    {
        return new Feed()
        {
            Title = title,
            Description = description,
            Link = link,
            ParentId = parentId,
            Items = items,
            ImageUrl = imageUrl,
        };
    }

    public static Feed GetFeedDirectory(string title, int parentId, string imageUrl, ICollection<Feed> children)
    {
        return new Feed()
        {
            Title = title,
            Description = String.Empty,
            Link = new Uri("/"),
            ParentId = parentId,
            Items = null,
            Children = children,
            ImageUrl = imageUrl,
        };
    }

    public int FeedId { get; set; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required Uri Link { get; init; }
    public int ParentId { get; set; } = 1;

    public ICollection<FeedItem>? Items { get; set; }
    public ICollection<Feed>? Children { get; set; }
    public required string ImageUrl { get; set; }

    public static Feed Map(IDataRecord dataRecord)
    {
        return new Feed
        {
            FeedId = dataRecord.GetInt32(nameof(FeedId)),
            Title = dataRecord.GetString(nameof(Title)),
            Description = dataRecord.GetString(nameof(Description)),
            Link = dataRecord.GetString(nameof(Link)).Equals(String.Empty) ? new Uri("/", UriKind.Relative) : new Uri(dataRecord.GetString(nameof(Link))),
            ImageUrl = dataRecord.GetString(nameof(ImageUrl)),
            ParentId = dataRecord.GetInt32(nameof(ParentId))
        };
    }
}