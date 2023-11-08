using System.Data;

using Nanorm;

namespace Skimmer.Core.Models;

public class Feed : IDataRecordMapper<Feed>
{
    public int FeedId { get; set; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required Uri Link { get; init; }
    public int? ParentId { get; set; }

    public ICollection<FeedItem>? Items { get; set; }
    public required string ImageUrl { get; set; }

    public static Feed Map(IDataRecord dataRecord)
    {
        return new Feed
        {
            FeedId = dataRecord.GetInt32(nameof(FeedId)),
            Title = dataRecord.GetString(nameof(Title)),
            Description = dataRecord.GetString(nameof(Description)),
            Link = new Uri(dataRecord.GetString(nameof(Link))),
            ImageUrl = dataRecord.GetString(nameof(ImageUrl)),
            ParentId = dataRecord.IsDBNull(nameof(ParentId)) ? null : dataRecord.GetInt32(nameof(ParentId))
        };
    }
}