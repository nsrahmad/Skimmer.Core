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
        UnreadItems = feed.Items.Count(i => !i.IsRead);

        FeedItems = new ObservableCollection<ObservableFeedItem>(
            feed.Items.Select(f => new ObservableFeedItem(f)).ToList()
            );
    }

    [ObservableProperty] public partial ObservableCollection<ObservableFeedItem> FeedItems { get; set; }

    [ObservableProperty] public partial int UnreadItems { get; set; }

    public int FeedId => _feed.FeedId;

    public string Title => _feed.Title;

    public string ImageUrl => _feed.ImageUrl == string.Empty
        ? "/Assets/rss.png"
        : _feed.ImageUrl;

    public string Description => _feed.Description;
}
