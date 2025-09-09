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
using Skimmer.Avalonia.ViewModels;
using Skimmer.Core.Data;
using Skimmer.Core.Models;
using Skimmer.Core.Nanorm;

namespace Skimmer.Core.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IFeedManager _manager;

    public MainWindowViewModel()
    {
        _manager = new NanormFeedManager();
        IList<Feed> feeds = Task.Run(() => _manager.GetAllFeedsAsync()).GetAwaiter().GetResult();
        Feeds.Add(new ObservableFeed(feeds[0]));
        SelectedFeed = Feeds[0];
        SelectedFeedItem = SelectedFeed.FeedItems[0];
    }

    public MainWindowViewModel(IFeedManager manager)
    {
        _manager = manager;
        IList<Feed> feeds = Task.Run(() => _manager.GetAllFeedsAsync()).GetAwaiter().GetResult();
        Feeds.Add(new ObservableFeed(feeds[0]));
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
        if (SelectedFeed.Children is { Count: not 0 })
        {
            SelectedFeed.Children.First( f => f.FeedId == value.FeedId).UnreadItems--;
            SelectedFeed.UnreadItems--;
        }
        else
        {
            Feeds.First(f => f.FeedId == SelectedFeed.ParentId).UnreadItems--;
            SelectedFeed.UnreadItems = SelectedFeed.FeedItems.Count(item => !item.IsRead);
        }
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

        Feed? f = await OnAddFeed(link);
        Feeds[0].Children!.Add(new ObservableFeed(f!)); // Feeds[0] is the "All feeds" Category
        IsAddDialogOpen = false;
    }

    [RelayCommand]
    private Task OnUpdateFeed(int feedId) => UpdateFeedAsync(feedId);

    [RelayCommand]
    private async Task OnDeleteFeed(int feedId)
    {
        await _manager.DeleteFeedAsync(feedId);
        ObservableFeed f = Feeds[0].Children!.First(x => x.FeedId == feedId);
        Feeds[0].Children!.Remove(f);
    }

    [RelayCommand]
    private async Task OnMarkAllAsRead(int feedId)
    {
        // TODO: marking Top level feed messes up the count, as it is not handled correctly in db code.

        await _manager.MarkAllAsReadAsync(feedId);

        // is it a toplevel feed?
        foreach (ObservableFeed f in Feeds)
        {
            if (f.FeedId == feedId)
            {
                // Every Top-level feed must have at least one child.
                foreach (ObservableFeed fChild in f.Children!)
                {
                    await OnMarkAllAsRead(fChild.FeedId);
                }
                return;
            }
        }

        foreach (ObservableFeed f in Feeds)
        {
            var feed = f.Children!.First((feed) => feed.FeedId == feedId);
            f.UnreadItems -= feed.UnreadItems;
            feed.UnreadItems = 0;
            if (feed.Children!.Count <= 0)
            {
                continue;
            }

            foreach (ObservableFeed feedChild in feed.Children)
            {
                await OnMarkAllAsRead(feedChild.FeedId);
            }
        }
    }

    [RelayCommand]
    private async Task OnUpdateAllFeeds()
    {
        IList<Feed> dirs = await _manager.GetAllFeedsAsync();
        List<Task> tasks = new(dirs.Sum(dir => dir.Children.Count));
        foreach (Feed dir in dirs)
        {
            tasks.AddRange(dir.Children.Select(f => UpdateFeedAsync(f.FeedId)));
        }

        await Task.WhenAll(tasks);
    }

    private async Task UpdateFeedAsync(int feedId)
    {
        List<FeedItem>? newItems = await _manager.UpdateFeedAsync(feedId);
        if (newItems != null)
        {
            ObservableFeed f = Feeds[0].Children!.First(f => f.FeedId == feedId);
            foreach (FeedItem i in newItems.OrderBy(i => i.LastUpdatedTime))
            {
                f.FeedItems.Insert(0, new ObservableFeedItem(i));
                f.UnreadItems++;
                Feeds[0].UnreadItems++;
            }
        }
    }
}
