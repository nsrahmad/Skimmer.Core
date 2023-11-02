using System.Data;
using Nanorm;

namespace Skimmer.Core.Models;

public class Feed : IDataRecordMapper<Feed>
{
    public int FeedId { get; set; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public Uri? Link { get; init; }
    public int? ParentId { get; set; }

    public ICollection<FeedItem>? Items { get; set; }
    public ICollection<Feed>? Children { get; set; }
    public string? ImageUrl { get; set; }

    public static Feed Map(IDataRecord dataRecord)
    {
        return new Feed
        {
            FeedId = dataRecord.GetInt32(nameof(FeedId)),
            Title = dataRecord.GetString(nameof(Title)),
            Description = dataRecord.IsDBNull(nameof(Description)) ? null : dataRecord.GetString(nameof(Description)),
            Link = dataRecord.IsDBNull(nameof(Link)) ? null : new Uri(dataRecord.GetString(nameof(Link))),
            ParentId = dataRecord.IsDBNull(nameof(ParentId)) ? null : dataRecord.GetInt32(nameof(ParentId))
        };
    }
}