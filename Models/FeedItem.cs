using System.Data;
using Nanorm;

namespace Skimmer.Core.Models;

public class FeedItem : IDataRecordMapper<FeedItem>
{
    public int FeedItemId { get; set; }

    public required string Title { get; init; }
    public required string Description { get; init; }
    public required Uri Link { get; init; }
    public DateTime LastUpdatedTime { get; init; }
    public bool IsRead { get; set; }

    public int FeedId { get; init; }

    public static FeedItem Map(IDataRecord dataRecord)
    {
        return new FeedItem
        {
            FeedItemId = dataRecord.GetInt32(nameof(FeedItemId)),
            Title = dataRecord.GetString(nameof(Title)),
            Description = dataRecord.GetString(nameof(Description)),
            Link = new Uri(dataRecord.GetString(nameof(Link))),
            LastUpdatedTime = dataRecord.GetDateTime(nameof(LastUpdatedTime)),
            IsRead = dataRecord.GetBoolean(nameof(IsRead)),
            FeedId = dataRecord.GetInt32(nameof(FeedId))
        };
    }
}