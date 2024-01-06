using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;

class Program
{
    private static readonly Stopwatch Stopwatch = new Stopwatch();
    

    // Hacker News API URL
    private const string HackerNewsApiUrl = "https://hacker-news.firebaseio.com/v0/";

    // Cache for storing API request results
    private static readonly Dictionary<int, CachedStory> Cache = new Dictionary<int, CachedStory>();

    // Cache expiration time in minutes
    public static readonly int CacheExpirationMinutes = 5;

    static async Task Main(string[] args)
    {
        Stopwatch.Start();
        // Prompt the user for the number of top stories (n)
        Console.Write("Enter the number of top stories (n): ");
        if (!int.TryParse(Console.ReadLine(), out int n) || n <= 0)
        {
            Console.WriteLine("Invalid input. Please enter a positive integer.");
            return;
        }

        // Retrieve and print the top stories
        var stories = await GetSortBestStoryIdsAsync();
        var bestStories = GetBestStories(stories, n);
        Console.WriteLine('[');
        foreach (Story story in bestStories)
        {
            PrintData(story);
        }
        Console.WriteLine(']');

        Stopwatch.Stop();
        Console.WriteLine($"Total execution time: {Stopwatch.ElapsedMilliseconds} ms");
    }

    // Retrieve and sort the best stories
    private static async Task<List<KeyValuePair<int, Story>>> GetSortBestStoryIdsAsync()
    {
        using (var httpClient = new HttpClient())
        {
            // Get story IDs
            var responseIds = await httpClient.GetStringAsync($"{HackerNewsApiUrl}beststories.json");
            List<int> results = ParseIntArray(responseIds);
            List<KeyValuePair<int, Story>> keyValuePairs = new List<KeyValuePair<int, Story>>();

            // Get story data by ID and add to cache
            foreach (int id in results)
            {
                Story story = await GetOrAddToCache(id);
                keyValuePairs.Add(new KeyValuePair<int, Story>(id, story));
            }

            // Sort by score in descending order
            keyValuePairs.Sort((x, y) => y.Value.Score.CompareTo(x.Value.Score));
            return keyValuePairs;
        }
    }

    // Retrieve a story from the cache or API and add to cache
    private static async Task<Story> GetOrAddToCache(int id)
    {
        if (Cache.TryGetValue(id, out var cachedStory) && !cachedStory.IsExpired())
        {
            return cachedStory.Story;
        }

        Story story = await GetStoryByIdStory(id);
        Cache[id] = new CachedStory(story, DateTime.UtcNow);
        return story;
    }

    // Get the top n best stories
    private static List<Story> GetBestStories(List<KeyValuePair<int, Story>> stories, int n)
    {
        List<Story> bestStories = new List<Story>();
        foreach (var kvp in stories.Take(n))
        {
            bestStories.Add(kvp.Value);
        }
        return bestStories;
    }

    // Parse a comma-separated string of integers into a list
    private static List<int> ParseIntArray(string input)
    {
        string[] numberStrings = input.Trim('[', ']')
                                      .Split(',');
        List<int> result = new List<int>();

        foreach (var numberString in numberStrings)
        {
            if (int.TryParse(numberString, out int number))
            {
                result.Add(number);
            }
            else
            {
                Console.WriteLine($"Error: {numberString}");
            }
        }
        return result;
    }

    // Get story details by ID from Hacker News API
    private static async Task<Story> GetStoryByIdStory(int id)
    {
        Story story = new Story();

        var httpClient = new HttpClient();
        var res = await httpClient.GetStringAsync($"{HackerNewsApiUrl}item/{id}.json");

        // Use regular expressions to extract information from the API response
        Regex regex_title = new Regex(@"title"":""([^""]+)");
        MatchCollection matches_title = regex_title.Matches(res);
        Regex regex_by = new Regex(@"by"":""([^""]+)");
        MatchCollection matchesBy = regex_by.Matches(res);
        Regex regex_score = new Regex(@"score"":(\d+)");
        MatchCollection matches_score = regex_score.Matches(res);
        Regex regex_time = new Regex(@"time"":(\d+)");
        MatchCollection matches_time = regex_time.Matches(res);
        Regex regex_url = new Regex(@"url"":""([^""]+)");
        MatchCollection matches_url = regex_url.Matches(res);
        Regex regex_descendants = new Regex(@"descendants"":(\d+)");
        MatchCollection matches_descendants = regex_descendants.Matches(res);

        // Populate the Story object with data from the API response
        foreach (Match match in matches_title)
        {
            story.Title = match.Groups[1].Value;
        }
        foreach (Match match in matchesBy)
        {
            story.PostedBy = match.Groups[1].Value;
        }
        foreach (Match match in matches_score)
        {
            int score = int.Parse(match.Groups[1].Value);
            story.Score = score;
        }
        foreach (Match match in matches_time)
        {
            long unixTime = long.Parse(match.Groups[1].Value);
            story.Time = DateTimeOffset.FromUnixTimeSeconds(unixTime).UtcDateTime;
        }
        foreach (Match match in matches_url)
        {
            story.Url = match.Groups[1].Value;
        }
        foreach (Match match in matches_descendants)
        {
            int descendants = int.Parse(match.Groups[1].Value);
            story.CommentCount = descendants;
        }
        return story;
    }

    // Print story data to the console
    private static void PrintData(Story story)
    {
        Console.WriteLine("  {");
        Console.WriteLine($"    title: {story.Title},");
        Console.WriteLine($"    url: {story.Url},");
        Console.WriteLine($"    postedBy: {story.PostedBy},");
        Console.WriteLine($"    time: {story.Time},");
        Console.WriteLine($"    score: {story.Score},");
        Console.WriteLine($"    commentCount: {story.CommentCount},");
        Console.WriteLine("  }");
    }
}

// Class to store story data
public class Story
{
    public string Title { get; set; }
    public string Url { get; set; }
    public string PostedBy { get; set; }
    public DateTime Time { get; set; }
    public int Score { get; set; }
    public int CommentCount { get; set; }
}

/*
 * The cache class is used to store and retrieve previously fetched story details by their IDs.This helps in:

 * Reducing API Requests:

 * If a story is found in the cache and is still valid, it's returned without making an additional API request.
 * This minimizes redundant calls to the Hacker News API, improving efficiency and reducing the load on the server.
 * Improved Performance:

 * Caching enhances performance for large requests by storing and reusing data.
 * It helps efficiently handle a substantial number of story details, optimizing the overall application responsiveness.
*/
public class CachedStory
{
    public Story Story { get; }
    public DateTime CachedAt { get; }

    public CachedStory(Story story, DateTime cachedAt)
    {
        Story = story;
        CachedAt = cachedAt;
    }

    // Check if the cache has expired
    public bool IsExpired()
    {
        return (DateTime.UtcNow - CachedAt).TotalMinutes > Program.CacheExpirationMinutes;
    }
}
