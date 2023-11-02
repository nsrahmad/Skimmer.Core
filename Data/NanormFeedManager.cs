using System.Data.Common;
using System.ServiceModel.Syndication;
using System.Xml;
using Microsoft.Data.Sqlite;
using Nanorm;
using Skimmer.Core.Models;

namespace Skimmer.Core.Data;

public class NanormFeedManager : IFeedManager
{
    private static readonly string DbPath;
    private static readonly HttpClient Client = new();

    static NanormFeedManager()
    {
        var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        Directory.CreateDirectory(Path.Join(path, "Skimmer"));
        DbPath = Path.Join(path, "Skimmer", "feeds.db");
        Client.DefaultRequestHeaders.UserAgent.ParseAdd("com.github.nsrahmad.Skimmer 0.1");
    }

    public async Task InitDbAsync()
    {
        if (File.Exists(DbPath)) return;

        await using var db = SqliteFactory.Instance.CreateConnection();
        db.ConnectionString = $"Data Source={DbPath};Cache=Shared";
        await db.ExecuteAsync("""
                              CREATE TABLE Feeds (
                                                     FeedId               INTEGER NOT NULL  PRIMARY KEY AUTOINCREMENT ,
                                                     Description          TEXT     ,
                                                     ImageUrl             TEXT     ,
                                                     Link                 TEXT     ,
                                                     Title                TEXT NOT NULL    ,
                                                     ParentId             INTEGER
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
        var tasks = new List<Task<Feed?>>(5)
        {
            AddFeedAsync("https://www.reddit.com/r/dotnet/.rss"),
            AddFeedAsync("https://www.reddit.com/r/csharp/.rss"),
            AddFeedAsync("https://www.osnews.com/files/recent.xml"),
            AddFeedAsync("https://news.ycombinator.com/rss"),
            AddFeedAsync("https://xkcd.com/rss.xml")
        };

        while (tasks.Count != 0)
        {
            var t = await Task.WhenAny(tasks);
            Console.WriteLine((await t)!.Title);
            tasks.Remove(t);
        }
    }

    public async Task<Feed?> AddFeedAsync(string link)
    {
        await using var db = SqliteFactory.Instance.CreateConnection();
        db.ConnectionString = $"Data Source={DbPath};Cache=Shared";
        using var reader = XmlReader.Create(await Client.GetStreamAsync(link));
        var netFeed = SyndicationFeed.Load(reader);
        var feed = new Feed
        {
            Title = netFeed.Title.Text,
            Description = netFeed.Description.Text,
            Link = new Uri(link),
            ImageUrl = netFeed.ImageUrl != null
                ? netFeed.ImageUrl.ToString()
                : "avares://Skimmer.Avalonia/Assets/rss.png",
            Items = netFeed.Items.Select(item => new FeedItem
            {
                Title = item.Title.Text,
                Description = item.Summary?.Text ?? (item.Content as TextSyndicationContent)!.Text,
                Link = item.Links[0].Uri,
                LastUpdatedTime = item.PublishDate.UtcDateTime
            }).ToArray()
        };
        var result = await db.QuerySingleAsync<Feed>($"""
                                                      INSERT INTO Feeds( Description, ImageUrl, Link, Title, ParentId )
                                                      VALUES ({feed.Description}, {feed.ImageUrl}, {feed.Link.ToString()}, {feed.Title}, {DBNull.Value})
                                                      RETURNING *
                                                      """);
        result!.Items = new List<FeedItem>();
        foreach (var feedItem in feed.Items) result.Items.Add((await AddFeedItem(db, feedItem, result.FeedId))!);
        return result;
    }

    public async Task<List<FeedItem>?> UpdateFeedAsync(int feedId)
    {
        await using var db = SqliteFactory.Instance.CreateConnection();
        db.ConnectionString = $"Data Source={DbPath};Cache=Shared";
        var dbFeed = await db.QuerySingleAsync<Feed>($"SELECT * FROM Feeds WHERE FeedId = {feedId}");
        using var reader = XmlReader.Create(await Client.GetStreamAsync(dbFeed!.Link));
        var netFeed = SyndicationFeed.Load(reader);
        var feedItems = new List<FeedItem>();
        foreach (var item in netFeed.Items)
        {
            var feedItem = await AddFeedItem(db, new FeedItem
            {
                FeedId = feedId,
                Title = item.Title.Text,
                Description = item.Summary?.Text ?? (item.Content as TextSyndicationContent)!.Text,
                Link = item.Links[0].Uri,
                LastUpdatedTime = item.PublishDate.UtcDateTime
            }, feedId);
            if (feedItem != null) feedItems.Add(feedItem);
        }

        return feedItems.Count > 0 ? feedItems : null;
    }

    public async Task DeleteFeedAsync(int feedId)
    {
        await using var db = SqliteFactory.Instance.CreateConnection();
        db.ConnectionString = $"Data Source={DbPath};Cache=Shared";
        await db.ExecuteAsync($"DELETE FROM Feeds WHERE Feeds.FeedId = {feedId}");
    }

    public async Task<IList<Feed>> GetAllFeedsAsync()
    {
        await using var db = SqliteFactory.Instance.CreateConnection();
        db.ConnectionString = $"Data Source={DbPath};Cache=Shared";
        var feeds = await db.QueryAsync<Feed>("SELECT * FROM Feeds").ToListAsync();

        foreach (var feed in feeds)
            feed.Items =
                await db.QueryAsync<FeedItem>(
                    $"""
                     select * from FeedItems
                     where FeedId = {feed.FeedId}
                     order by LastUpdatedTime desc
                     """).ToListAsync();

        return feeds;
    }

    public async Task MarkAllAsReadAsync()
    {
        await using var db = SqliteFactory.Instance.CreateConnection();
        db.ConnectionString = $"Data Source={DbPath};Cache=Shared";
        await db.ExecuteAsync("UPDATE FeedItems SET IsRead = 1 WHERE IsRead = 0");
    }

    public async void MarkAsRead(int feedItemId)
    {
        await using var db = SqliteFactory.Instance.CreateConnection();
        db.ConnectionString = $"Data Source={DbPath};Cache=Shared";
        await db.QuerySingleAsync<FeedItem>($"""
                                             Update FeedItems
                                             Set IsRead = 1
                                             WHERE FeedItems.FeedItemId = {feedItemId} AND IsRead = 0
                                             RETURNING *
                                             """);
    }

    private static async Task<FeedItem?> AddFeedItem(DbConnection db, FeedItem feedItem, int feedId)
    {
        return (await db.QuerySingleAsync<FeedItem>($"""
                                                     INSERT OR IGNORE INTO FeedItems(Title, Description, Link, LastUpdatedTime, IsRead, FeedId)
                                                     VALUES ({feedItem.Title}, {feedItem.Description}, {feedItem.Link.ToString()},
                                                             {feedItem.LastUpdatedTime}, {feedItem.IsRead}, {feedId})
                                                     RETURNING *
                                                     """))!;
    }
}