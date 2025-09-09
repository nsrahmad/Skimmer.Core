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

namespace Skimmer.Core.Models;

public partial class ObservableFeed : ObservableObject
{
    private readonly Feed _feed;

    public ObservableFeed(Feed feed)
    {
        _feed = feed;
        UnreadItems = GetUnreadItems(feed);

        ICollection<ObservableFeedItem> items = GetFeedItems(feed);
        ICollection<ObservableFeed> children = GetFeedChildren(feed);

        FeedItems = new ObservableCollection<ObservableFeedItem>(items);
        Children = new ObservableCollection<ObservableFeed>(children);
        return;

        int GetUnreadItems(Feed feed1)
        {
            return feed1.Children.Count != 0
                ? feed1.Children.Sum(f => f.Items!.Count(i => !i.IsRead))
                : feed1.Items!.Count(i => !i.IsRead);
        }

        ICollection<ObservableFeedItem> GetFeedItems(Feed feed2)
        {
            return feed2.Items != null
                ? feed2.Items!.Select(i => new ObservableFeedItem(i)).ToList()
                : feed2.Children.SelectMany(f => f.Items!.Select(i => new ObservableFeedItem(i))).ToList();
        }

        ICollection<ObservableFeed> GetFeedChildren(Feed feed3)
        {
            return feed3.Children.Select(f => new ObservableFeed(f)).ToList();
        }
    }

    [ObservableProperty] public partial ObservableCollection<ObservableFeed>? Children { get; set; }

    [ObservableProperty] public partial ObservableCollection<ObservableFeedItem> FeedItems { get; set; }

    [ObservableProperty] public partial int UnreadItems { get; set; }

    public int FeedId => _feed.FeedId;

    public string Title => _feed.Title;

    public int ParentId => _feed.ParentId;

    public string ImageUrl => _feed.ImageUrl;

    public string Description => _feed.Description;
}
