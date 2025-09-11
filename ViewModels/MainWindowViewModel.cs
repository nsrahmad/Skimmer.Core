// Copyright © Nisar Ahmad
//
// This program is free software:you can redistribute it and/or modify it under the terms of
// the GNU General Public License as published by the Free Software Foundation, either
// version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY
// WARRANTY, without even the implied warranty of MERCHANTABILITY or FITNESS FOR
// A PARTICULAR PURPOSE.See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with this
// program.If not, see <https://www.gnu.org/licenses/>.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Skimmer.Core.Data;
using Skimmer.Core.Models;

namespace Skimmer.Core.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IFeedManager _manager;

    public MainWindowViewModel()
    {
        _manager = new NanormFeedManager();
        IList<Feed> feeds = Task.Run(() => _manager.GetAllFeedsAsync()).GetAwaiter().GetResult();
        foreach (var feed in feeds)
        {
            Feeds.Add(new ObservableFeed(feed));
        }
        SelectedFeed = Feeds[0];
        SelectedFeedItem = SelectedFeed.FeedItems[0];
    }

    public MainWindowViewModel(IFeedManager manager)
    {
        _manager = manager;
        IList<Feed> feeds = Task.Run(() => _manager.GetAllFeedsAsync()).GetAwaiter().GetResult();
        foreach (var feed in feeds)
        {
            Feeds.Add(new ObservableFeed(feed));
        }
        SelectedFeed = Feeds[0];
        SelectedFeedItem = SelectedFeed.FeedItems[0];
    }

    [ObservableProperty] public partial ObservableFeed SelectedFeed { get; set; }

    [ObservableProperty] public partial ObservableFeedItem SelectedFeedItem { get; set; }

    [ObservableProperty] public partial string Url { get; set; } = string.Empty;

    [ObservableProperty] public partial bool IsAddDialogOpen { get; set; }

    public ObservableCollection<ObservableFeed> Feeds { get; set; } = [];

    partial void OnSelectedFeedItemChanged(ObservableFeedItem value)
    {
        if (value.IsRead)
        {
            return;
        }

        value.IsRead = true;
        _manager.MarkAsRead(value.FeedItemId);
        SelectedFeed.UnreadItems--;
    }

    [RelayCommand]
    private Task<Feed?> OnAddFeed(string link) => _manager.AddFeedAsync(link);

    [RelayCommand]
    private async Task OnAddNewFeed(string? link)
    {
        if (string.IsNullOrEmpty(link))
        {
            IsAddDialogOpen = false;
            return;
        }

        var f = await OnAddFeed(link);
        if (f != null)
        {
            Feeds.Add(new ObservableFeed(f));
        }
        IsAddDialogOpen = false;
    }

    [RelayCommand]
    private Task OnUpdateFeed(int feedId) => UpdateFeedAsync(feedId);

    [RelayCommand]
    private async Task OnDeleteFeed(int feedId)
    {
        await _manager.DeleteFeedAsync(feedId);
        Feeds.Remove(Feeds.First(x => x.FeedId == feedId));
    }

    [RelayCommand]
    private async Task OnMarkAllAsRead(int feedId)
    {
        await _manager.MarkAllAsReadAsync(feedId);
        Feeds.First(f => f.FeedId == feedId).UnreadItems = 0;
    }

    [RelayCommand]
    private async Task OnUpdateAllFeeds()
    {
        IList<Feed> feeds = await _manager.GetAllFeedsAsync();
        List<Task<List<FeedItem>?>> tasks = new(feeds.Count);
        tasks.AddRange(feeds.Select(f => UpdateFeedAsync(f.FeedId)));
        await foreach (var t in Task.WhenEach(tasks))
        {
            var items = await t;
            if (items == null)
            {
                continue;
            }

            ObservableFeed f = Feeds.First(f => f.FeedId == items[0].FeedId);
            f.UnreadItems += items.Count;
            foreach (FeedItem i in items.OrderBy(i => i.LastUpdatedTime))
            {
                f.FeedItems.Insert(0, new ObservableFeedItem(i));
            }
        }
    }

    private async Task<List<FeedItem>?> UpdateFeedAsync(int feedId)
    {
        List<FeedItem>? newItems = await _manager.UpdateFeedAsync(feedId);
        return newItems ?? null;
    }
}
