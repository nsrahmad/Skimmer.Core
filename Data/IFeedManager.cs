using Skimmer.Core.Models;

namespace Skimmer.Core.Data;

public interface IFeedManager
{
    Task InitDbAsync();
    Task<Feed?> AddFeedAsync(string link);
    Task<List<FeedItem>?> UpdateFeedAsync(int feedId);
    Task DeleteFeedAsync(int feedId);
    Task<IList<Feed>> GetAllFeedsAsync();
    Task MarkAllAsReadAsync();
    void MarkAsRead(int feedItemId);
}