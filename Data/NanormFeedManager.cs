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

using System.Data.Common;
using System.ServiceModel.Syndication;
using System.Xml;
using Microsoft.Data.Sqlite;
using Nanorm;
using Skimmer.Core.Models;

namespace Skimmer.Core.Data;

public class NanormFeedManager : IFeedManager
{
    private static readonly string s_dbPath;
    private static readonly string s_connectionString;
    private static readonly HttpClient s_client = new();

    static NanormFeedManager()
    {
        string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        Directory.CreateDirectory(Path.Join(path, "Skimmer"));
        s_dbPath = Path.Join(path, "Skimmer", "feeds.db");
        s_connectionString = $"Data Source={s_dbPath};Cache=Shared;Pooling=True;";
        s_client.DefaultRequestHeaders.UserAgent.ParseAdd("com.github.nsrahmad.Skimmer 0.1");
    }

    public async Task InitDbAsync()
    {
        if (File.Exists(s_dbPath))
        {
            return;
        }

        try
        {
            await using (DbConnection db = SqliteFactory.Instance.CreateConnection())
            {
                db.ConnectionString = s_connectionString;
                await db.ExecuteAsync("""
                                      CREATE TABLE Feeds (
                                                             FeedId               INTEGER NOT NULL  PRIMARY KEY AUTOINCREMENT ,
                                                             Description          TEXT NOT NULL    ,
                                                             ImageUrl             TEXT NOT NULL    ,
                                                             Link                 TEXT NOT NULL    ,
                                                             Title                TEXT NOT NULL
                                      );

                                      CREATE TABLE FeedItems (
                                                                 FeedItemId           INTEGER NOT NULL  PRIMARY KEY AUTOINCREMENT ,
                                                                 Title                TEXT NOT NULL    ,
                                                                 Description          TEXT NOT NULL    ,
                                                                 Link                 TEXT NOT NULL UNIQUE    ,
                                                                 LastUpdatedTime      TEXT NOT NULL    ,
                                                                 IsRead               INTEGER NOT NULL    ,
                                                                 FeedId               INTEGER NOT NULL    ,
                                                                 FOREIGN KEY ( FeedId ) REFERENCES Feeds( FeedId ) ON DELETE CASCADE
                                      );

                                      CREATE INDEX IX_FeedItems_FeedId ON FeedItems ( FeedId );
                                      """);
            }

            string[] urls =
            [
                "https://news.ycombinator.com/rss",
                "https://xkcd.com/rss.xml"
            ];

            Task[] tasks = new Task[urls.Length];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = AddFeedAsync(urls[i]);
            }

            await Task.WhenAll(tasks);
        }
        catch (Exception e)
        {
            await Console.Error.WriteLineAsync(e.Message);
        }
    }

    public async Task<Feed?> AddFeedAsync(string link)
    {
        await using DbConnection db = SqliteFactory.Instance.CreateConnection();
        db.ConnectionString = s_connectionString;

        using XmlReader reader = XmlReader.Create(await s_client.GetStreamAsync(link));
        SyndicationFeed? netFeed = SyndicationFeed.Load(reader);
        Feed feed = new()
        {
            Title = netFeed.Title.Text,
            Description = netFeed.Description?.Text ?? netFeed.Title.Text,
            Link = new Uri(link),
            ImageUrl = netFeed.ImageUrl == null ? string.Empty : netFeed.ImageUrl.ToString(),
            Items = netFeed.Items.Select(item => new FeedItem
                {
                    Title = item.Title.Text,
                    Description = item.Summary?.Text ?? (item.Content as TextSyndicationContent)!.Text,
                    Link = item.Links[0].Uri,
                    LastUpdatedTime = item.PublishDate.UtcDateTime
                })
                .ToList()
        };
        Feed? result = await db.QuerySingleAsync<Feed>($"""
                                                        INSERT INTO Feeds( Description, ImageUrl, Link, Title)
                                                        VALUES ({feed.Description}, {feed.ImageUrl}, {feed.Link.ToString()}, {feed.Title})
                                                        RETURNING *
                                                        """);
        if (result == null)
        {
            return null;
        }

        result.Items = new List<FeedItem>();
        foreach (FeedItem feedItem in feed.Items)
        {
            FeedItem? f = await AddFeedItem(db, feedItem, result.FeedId);
            if (f != null)
            {
                result.Items.Add(f);
            }
        }

        return result;
    }

    public async Task<List<FeedItem>?> UpdateFeedAsync(int feedId)
    {
        try
        {
            await using DbConnection db = SqliteFactory.Instance.CreateConnection();
            db.ConnectionString = s_connectionString;
            Feed? dbFeed = await db.QuerySingleAsync<Feed>($"SELECT * FROM Feeds WHERE FeedId = {feedId}");
            using XmlReader reader = XmlReader.Create(await s_client.GetStreamAsync(dbFeed!.Link));
            SyndicationFeed? netFeed = SyndicationFeed.Load(reader);
            List<FeedItem> feedItems = new();
            foreach (SyndicationItem? item in netFeed.Items)
            {
                FeedItem? feedItem = await AddFeedItem(db,
                    new FeedItem
                    {
                        FeedId = feedId,
                        Title = item.Title.Text,
                        Description = item.Summary?.Text ?? (item.Content as TextSyndicationContent)!.Text,
                        Link = item.Links[0].Uri,
                        LastUpdatedTime = item.PublishDate.UtcDateTime
                    }, feedId);
                if (feedItem != null)
                {
                    feedItems.Add(feedItem);
                }
            }

            return feedItems.Count > 0 ? feedItems : null;
        }
        catch (Exception e)
        {
            await Console.Error.WriteLineAsync(e.Message);
            return null;
        }
    }

    public async Task DeleteFeedAsync(int feedId)
    {
        await using DbConnection db = SqliteFactory.Instance.CreateConnection();
        db.ConnectionString = s_connectionString;
        await db.ExecuteAsync($"DELETE FROM Feeds WHERE Feeds.FeedId = {feedId}");
    }

    public async Task<IList<Feed>> GetAllFeedsAsync()
    {
        await using DbConnection db = SqliteFactory.Instance.CreateConnection();
        db.ConnectionString = s_connectionString;
        List<Feed> allFeeds = await db.QueryAsync<Feed>("SELECT * FROM Feeds").ToListAsync();

        foreach (Feed feed in allFeeds)
        {
            feed.Items =
                await db.QueryAsync<FeedItem>(
                    $"""
                     select * from FeedItems
                     where FeedId = {feed.FeedId}
                     order by LastUpdatedTime DESC
                     """).ToListAsync();
        }

        return allFeeds;
    }

    public async Task MarkAllAsReadAsync(int feedId)
    {
        await using DbConnection db = SqliteFactory.Instance.CreateConnection();
        db.ConnectionString = s_connectionString;
        await db.ExecuteAsync($"UPDATE FeedItems SET IsRead = 1 WHERE IsRead = 0 AND FeedId = {feedId};");
    }

    public async void MarkAsRead(int feedItemId)
    {
        try
        {
            await using DbConnection db = SqliteFactory.Instance.CreateConnection();
            db.ConnectionString = s_connectionString;
            await db.QuerySingleAsync<FeedItem>($"""
                                                 Update FeedItems
                                                 Set IsRead = 1
                                                 WHERE FeedItems.FeedItemId = {feedItemId} AND IsRead = 0
                                                 RETURNING *
                                                 """);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    private static async Task<FeedItem?> AddFeedItem(DbConnection db, FeedItem feedItem, int feedId) =>
        await db.QuerySingleAsync<FeedItem>($"""
                                             INSERT OR IGNORE INTO FeedItems(Title, Description, Link, LastUpdatedTime, IsRead, FeedId)
                                             VALUES ({feedItem.Title}, {feedItem.Description}, {feedItem.Link.ToString()},
                                                     {feedItem.LastUpdatedTime}, {feedItem.IsRead}, {feedId})
                                             RETURNING *
                                             """);
}
