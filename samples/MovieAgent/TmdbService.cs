using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;

namespace MovieAgent
{
    public class TmdbService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<TmdbService> _logger;
        private const string BaseUrl = "https://api.themoviedb.org/3";

        public TmdbService(string apiKey, ILogger<TmdbService> logger)
        {
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// 搜索电影
        /// </summary>
        public async Task<object> SearchMoviesAsync(string query, int page = 1)
        {
            try
            {
                _logger.LogInformation("搜索电影: {Query}", query);
                
                var url = $"{BaseUrl}/search/movie?api_key={_apiKey}&query={Uri.EscapeDataString(query)}&page={page}&language=zh-CN";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                var results = JObject.Parse(content);
                
                return FormatMovieResults(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索电影时发生错误: {Query}", query);
                throw;
            }
        }

        /// <summary>
        /// 搜索人物（演员、导演等）
        /// </summary>
        public async Task<object> SearchPeopleAsync(string query, int page = 1)
        {
            try
            {
                _logger.LogInformation("搜索人物: {Query}", query);
                
                var url = $"{BaseUrl}/search/person?api_key={_apiKey}&query={Uri.EscapeDataString(query)}&page={page}&language=zh-CN";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                var results = JObject.Parse(content);
                
                return FormatPersonResults(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索人物时发生错误: {Query}", query);
                throw;
            }
        }

        /// <summary>
        /// 格式化电影搜索结果
        /// </summary>
        private object FormatMovieResults(JObject rawResults)
        {
            var results = new List<object>();
            var totalResults = rawResults["total_results"]?.Value<int>() ?? 0;
            var totalPages = rawResults["total_pages"]?.Value<int>() ?? 0;
            
            if (totalResults == 0)
            {
                return new { movies = results, total_results = 0, total_pages = 0, message = "未找到相关电影" };
            }
            
            foreach (var movie in rawResults["results"])
            {
                var posterPath = movie["poster_path"]?.ToString();
                var posterUrl = string.IsNullOrEmpty(posterPath) 
                    ? null 
                    : $"https://image.tmdb.org/t/p/w500{posterPath}";
                
                results.Add(new
                {
                    id = movie["id"]?.Value<int>(),
                    title = movie["title"]?.ToString(),
                    original_title = movie["original_title"]?.ToString(),
                    overview = movie["overview"]?.ToString(),
                    release_date = movie["release_date"]?.ToString(),
                    vote_average = movie["vote_average"]?.Value<double>(),
                    poster_url = posterUrl
                });
            }
            
            return new 
            { 
                movies = results, 
                total_results = totalResults, 
                total_pages = totalPages 
            };
        }

        /// <summary>
        /// 格式化人物搜索结果
        /// </summary>
        private object FormatPersonResults(JObject rawResults)
        {
            var results = new List<object>();
            var totalResults = rawResults["total_results"]?.Value<int>() ?? 0;
            var totalPages = rawResults["total_pages"]?.Value<int>() ?? 0;
            
            if (totalResults == 0)
            {
                return new { people = results, total_results = 0, total_pages = 0, message = "未找到相关人物" };
            }
            
            foreach (var person in rawResults["results"])
            {
                var profilePath = person["profile_path"]?.ToString();
                var profileUrl = string.IsNullOrEmpty(profilePath) 
                    ? null 
                    : $"https://image.tmdb.org/t/p/w500{profilePath}";
                
                var knownFor = new List<object>();
                foreach (var work in person["known_for"] ?? new JArray())
                {
                    var mediaType = work["media_type"]?.ToString();
                    if (mediaType == "movie")
                    {
                        knownFor.Add(new
                        {
                            id = work["id"]?.Value<int>(),
                            title = work["title"]?.ToString(),
                            media_type = "电影"
                        });
                    }
                    else if (mediaType == "tv")
                    {
                        knownFor.Add(new
                        {
                            id = work["id"]?.Value<int>(),
                            title = work["name"]?.ToString(),
                            media_type = "电视剧"
                        });
                    }
                }
                
                results.Add(new
                {
                    id = person["id"]?.Value<int>(),
                    name = person["name"]?.ToString(),
                    known_for_department = person["known_for_department"]?.ToString(),
                    profile_url = profileUrl,
                    known_for = knownFor
                });
            }
            
            return new 
            { 
                people = results, 
                total_results = totalResults, 
                total_pages = totalPages 
            };
        }
    }
} 