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

    public static FeedItem Map(IDataRecord dataRecord) =>
        new()
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
