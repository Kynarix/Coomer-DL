using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace CoomerDownloader.Services
{
    public class CoomerApiService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://coomer.st";

        public CoomerApiService()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            };
            
            _httpClient = new HttpClient(handler);
            _httpClient.Timeout = TimeSpan.FromSeconds(60);

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept", "text/css");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
            _httpClient.DefaultRequestHeaders.Add("Referer", "https://coomer.st/");
        }

        public async Task<List<Creator>?> SearchCreators(string query)
        {
            Console.WriteLine($"[API] SearchCreators called with query: '{query}'");
            
            if (string.IsNullOrWhiteSpace(query))
            {
                Console.WriteLine("[API] Query is empty");
                return null;
            }

            var foundCreators = new List<Creator>();
            var services = new[] { "onlyfans", "fansly"};
            var username = query.Trim();

            Console.WriteLine($"[API] Searching for '{username}' in {services.Length} services");

            foreach (var service in services)
            {
                try
                {
                    var url = $"{BaseUrl}/api/v1/{service}/user/{username}/profile";
                    Console.WriteLine($"[API] Trying: {url}");
                    
                    var response = await _httpClient.GetAsync(url);
                    Console.WriteLine($"[API] {service} responded with: {response.StatusCode}");

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"[API] {service} content length: {content.Length}");
                        Console.WriteLine($"[API] {service} content first 200 chars: {content.Substring(0, Math.Min(200, content.Length))}");
                        
                        var creator = JsonConvert.DeserializeObject<Creator>(content);
                        
                        if (creator != null)
                        {
                            Console.WriteLine($"[API] Creator deserialized: id={creator.id}, name={creator.name}");
                            if (string.IsNullOrEmpty(creator.service))
                                creator.service = service;
                            
                            foundCreators.Add(creator);
                            Console.WriteLine($"[API] Found creator on {service}!");
                        }
                        else
                        {
                            Console.WriteLine($"[API] Failed to deserialize creator from {service}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[API] ERROR for {service}: {ex.Message}");
                    Console.WriteLine($"[API] Stack: {ex.StackTrace}");
                    continue;
                }
            }

            Console.WriteLine($"[API] Search complete. Total found: {foundCreators.Count}");
            return foundCreators.Count > 0 ? foundCreators : null;
        }

        public async Task<List<Post>?> GetCreatorPosts(string service, string creatorId)
        {
            try
            {
                var allPosts = new List<Post>();
                int offset = 0;
                const int pageSize = 50;
                bool hasMorePosts = true;
                
                Console.WriteLine($"[API] Starting to fetch all posts for {service}/{creatorId}");
                
                while (hasMorePosts)
                {
                    var url = offset == 0 
                        ? $"{BaseUrl}/api/v1/{service}/user/{creatorId}/posts"
                        : $"{BaseUrl}/api/v1/{service}/user/{creatorId}/posts?o={offset}";
                    
                    Console.WriteLine($"[API] Fetching page at offset {offset}: {url}");
                    
                    var response = await _httpClient.GetAsync(url);
                    Console.WriteLine($"[API] Response status: {response.StatusCode}");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var posts = JsonConvert.DeserializeObject<List<Post>>(content);
                        
                        if (posts != null && posts.Count > 0)
                        {
                            Console.WriteLine($"[API] Received {posts.Count} posts at offset {offset}");
                            allPosts.AddRange(posts);
                            
                            if (posts.Count < pageSize)
                            {
                                Console.WriteLine($"[API] Received less than {pageSize} posts, reached end");
                                hasMorePosts = false;
                            }
                            else
                            {
                                offset += pageSize;
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[API] No more posts found at offset {offset}");
                            hasMorePosts = false;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[API] Failed to get posts at offset {offset}: {response.StatusCode}");
                        hasMorePosts = false;
                    }
                }
                
                Console.WriteLine($"[API] Total posts fetched: {allPosts.Count}");
                return allPosts.Count > 0 ? allPosts : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] ERROR getting posts: {ex.Message}");
                Console.WriteLine($"[API] Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        public async Task<byte[]> DownloadFile(string url)
        {
            return await _httpClient.GetByteArrayAsync(url);
        }
    }

    public class Creator
    {
        public string? id { get; set; }
        public string? name { get; set; }
        public string? service { get; set; }
        public string? indexed { get; set; }
        public string? updated { get; set; }
        public string? public_id { get; set; }
        public string? relation_id { get; set; }
        public int? post_count { get; set; }
        public int? dm_count { get; set; }
        public int? share_count { get; set; }
        public int? chat_count { get; set; }
    }

    public class Post
    {
        public string? id { get; set; }
        public string? title { get; set; }
        public string? content { get; set; }
        public List<Attachment>? attachments { get; set; }
        
        public File? file { get; set; }
        public List<File>? files { get; set; }
        
        [JsonIgnore]
        public List<File> AllFiles
        {
            get
            {
                var result = new List<File>();
                if (file != null && !string.IsNullOrEmpty(file.name))
                    result.Add(file);
                if (files != null)
                    result.AddRange(files);
                return result;
            }
        }
    }

    public class Attachment
    {
        public string? name { get; set; }
        public string? path { get; set; }
    }

    public class File
    {
        public string? name { get; set; }
        public string? path { get; set; }
    }
}
