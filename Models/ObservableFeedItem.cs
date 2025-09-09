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

using CommunityToolkit.Mvvm.ComponentModel;

namespace Skimmer.Core.Models;

public class ObservableFeedItem(FeedItem item) : ObservableObject
{
    public bool IsRead
    {
        get => item.IsRead;
        set => SetProperty(item.IsRead, value, item, (feedItem, b) => feedItem.IsRead = b);
    }

    public string Title => item.Title;
    public int FeedItemId => item.FeedItemId;

    public DateTime LastUpdatedTime => item.LastUpdatedTime;

    public string Description => item.Description;

    public string Link => item.Link.ToString();

    public int FeedId => item.FeedId;
}
