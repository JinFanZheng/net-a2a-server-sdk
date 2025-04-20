using System.Text.RegularExpressions;
using A2A.Server.SDK.Handlers;
using A2A.Server.SDK.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaskStatus = A2A.Server.SDK.Schema.TaskStatus;

namespace MovieAgent
{
    public class MovieTaskHandler : ITaskHandler
    {
        private readonly TmdbService _tmdbService;
        private readonly OpenAIService _openAIService;
        private readonly ILogger<MovieTaskHandler> _logger;
        
        public MovieTaskHandler(TmdbService tmdbService, OpenAIService openAIService, ILogger<MovieTaskHandler> logger)
        {
            _tmdbService = tmdbService ?? throw new ArgumentNullException(nameof(tmdbService));
            _openAIService = openAIService ?? throw new ArgumentNullException(nameof(openAIService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 处理任务的异步方法
        /// </summary>
        public async IAsyncEnumerable<TaskYieldUpdate> HandleTaskAsync(TaskContext context)
        {
            _logger.LogInformation("处理任务: {TaskId}, 消息: {Message}", 
                context.Task.Id, context.UserMessage.Parts?[0]?.ToString() ?? "无消息");
            
            // 处理任务状态
            yield return new TaskYieldUpdate
            {
                StatusUpdate = new TaskStatus
                {
                    State = TaskState.Working,
                    Message = new Message
                    {
                        Role = "agent",
                        Parts = new List<Part> 
                        { 
                            new TextPart { Text = "我正在查询相关信息..." } 
                        }
                    }
                }
            };

            // 获取用户消息文本
            string userMessage = GetUserMessageText(context);
            
            // 检查用户消息中是否包含电影搜索请求
            if (IsMovieSearchRequest(userMessage))
            {
                string movieQuery = ExtractMovieQuery(userMessage);
                
                if (!string.IsNullOrEmpty(movieQuery))
                {
                    yield return await HandleMovieSearchAsync(movieQuery);
                }
                else
                {
                    yield return CreateFailureResponse("我无法确定您要查询的电影名称，请提供更明确的电影名称。");
                }
            }
            // 检查用户消息中是否包含人物搜索请求
            else if (IsPeopleSearchRequest(userMessage))
            {
                string peopleQuery = ExtractPeopleQuery(userMessage);
                
                if (!string.IsNullOrEmpty(peopleQuery))
                {
                    yield return await HandlePeopleSearchAsync(peopleQuery);
                }
                else
                {
                    yield return CreateFailureResponse("我无法确定您要查询的人物名称，请提供更明确的演员或导演名称。");
                }
            }
            // 如果是明确的搜索命令
            else if (userMessage.StartsWith("search_movies", StringComparison.OrdinalIgnoreCase))
            {
                string query = userMessage.Substring("search_movies".Length).Trim();
                if (!string.IsNullOrEmpty(query))
                {
                    yield return await HandleMovieSearchAsync(query);
                }
                else
                {
                    yield return CreateFailureResponse("请提供要搜索的电影名称。");
                }
            }
            else if (userMessage.StartsWith("search_people", StringComparison.OrdinalIgnoreCase))
            {
                string query = userMessage.Substring("search_people".Length).Trim();
                if (!string.IsNullOrEmpty(query))
                {
                    yield return await HandlePeopleSearchAsync(query);
                }
                else
                {
                    yield return CreateFailureResponse("请提供要搜索的人物名称。");
                }
            }
            else
            {
                // 无法确定意图，返回帮助信息
                yield return CreateHelpResponse();
            }
        }
        
        /// <summary>
        /// 获取用户消息文本内容
        /// </summary>
        private string GetUserMessageText(TaskContext context)
        {
            if (context.UserMessage?.Parts == null || context.UserMessage.Parts.Count == 0)
            {
                return string.Empty;
            }
            
            // 查找文本部分
            foreach (var part in context.UserMessage.Parts)
            {
                if (part is TextPart textPart)
                {
                    return textPart.Text ?? string.Empty;
                }
            }
            
            return string.Empty;
        }
        
        /// <summary>
        /// 判断是否为电影搜索请求
        /// </summary>
        private bool IsMovieSearchRequest(string message)
        {
            return Regex.IsMatch(message, @"电影|影片|片名|片子|作品|《.*》", RegexOptions.IgnoreCase);
        }
        
        /// <summary>
        /// 提取电影查询关键词
        /// </summary>
        private string ExtractMovieQuery(string message)
        {
            // 提取《》中的内容
            var match = Regex.Match(message, @"《(.*?)》");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            
            // 尝试提取"电影XXX"模式
            match = Regex.Match(message, @"电影[：:]\s*(.+?)(?:\s|$|？|\?|。|\.)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            
            // 如果没有明确标记，返回空字符串
            return string.Empty;
        }
        
        /// <summary>
        /// 判断是否为人物搜索请求
        /// </summary>
        private bool IsPeopleSearchRequest(string message)
        {
            return Regex.IsMatch(message, @"演员|导演|明星|艺人|人物|谁演的|谁导的|参演", RegexOptions.IgnoreCase);
        }
        
        /// <summary>
        /// 提取人物查询关键词
        /// </summary>
        private string ExtractPeopleQuery(string message)
        {
            // 提取人名
            foreach (var pattern in new[] {
                @"演员[：:]\s*(.+?)(?:\s|$|？|\?|。|\.)",
                @"导演[：:]\s*(.+?)(?:\s|$|？|\?|。|\.)",
                @"(.+?)(?:演的|导的|主演|饰演)",
                @"(.+?)(?:的电影|的作品|参演的|导演的)"})
            {
                var match = Regex.Match(message, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
            
            return string.Empty;
        }
        
        /// <summary>
        /// 处理电影搜索
        /// </summary>
        private async Task<TaskYieldUpdate> HandleMovieSearchAsync(string query)
        {
            try
            {
                _logger.LogInformation("搜索电影: {Query}", query);
                
                var result = await _tmdbService.SearchMoviesAsync(query);
                _logger.LogInformation("搜索电影成功: {Query}, 结果类型: {ResultType}", query, result?.GetType().Name);
                
                string resultStr = FormatMovieSearchResults(result);
                _logger.LogInformation("格式化电影搜索结果成功");

                // 使用OpenAI增强电影搜索结果
                try
                {
                    _logger.LogInformation("使用OpenAI增强电影搜索结果");
                    string enrichedResult = await _openAIService.EnrichMovieResultsAsync(resultStr);
                    
                    if (!string.IsNullOrEmpty(enrichedResult))
                    {
                        resultStr += "\n\n## 电影专家分析\n" + enrichedResult;
                        _logger.LogInformation("OpenAI增强成功");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "OpenAI增强失败，将使用原始结果");
                }
                
                return new TaskYieldUpdate
                {
                    StatusUpdate = new TaskStatus
                    {
                        State = TaskState.Completed,
                        Message = new Message
                        {
                            Role = "agent",
                            Parts = new List<Part> { new TextPart { Text = resultStr } }
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索电影时出错: {Query}, 错误: {Error}", query, ex.Message);
                
                return CreateFailureResponse($"搜索电影时发生错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理人物搜索
        /// </summary>
        private async Task<TaskYieldUpdate> HandlePeopleSearchAsync(string query)
        {
            try
            {
                _logger.LogInformation("搜索人物: {Query}", query);
                
                var result = await _tmdbService.SearchPeopleAsync(query);
                _logger.LogInformation("搜索人物成功: {Query}, 结果类型: {ResultType}", query, result?.GetType().Name);
                
                string resultStr = FormatPeopleSearchResults(result);
                _logger.LogInformation("格式化人物搜索结果成功");

                // 使用OpenAI增强人物搜索结果
                try
                {
                    _logger.LogInformation("使用OpenAI增强人物搜索结果");
                    string enrichedResult = await _openAIService.EnrichPeopleResultsAsync(resultStr);
                    
                    if (!string.IsNullOrEmpty(enrichedResult))
                    {
                        resultStr += "\n\n## 电影专家分析\n" + enrichedResult;
                        _logger.LogInformation("OpenAI增强成功");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "OpenAI增强失败，将使用原始结果");
                }
                
                return new TaskYieldUpdate
                {
                    StatusUpdate = new TaskStatus
                    {
                        State = TaskState.Completed,
                        Message = new Message
                        {
                            Role = "agent",
                            Parts = new List<Part> { new TextPart { Text = resultStr } }
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索人物时出错: {Query}, 错误: {Error}", query, ex.Message);
                
                return CreateFailureResponse($"搜索人物时发生错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 格式化电影搜索结果
        /// </summary>
        private string FormatMovieResults(JObject rawResults)
        {
            var results = new List<object>();
            var totalResults = rawResults["total_results"]?.Value<int>() ?? 0;
            var totalPages = rawResults["total_pages"]?.Value<int>() ?? 0;
            
            if (totalResults == 0)
            {
                return JsonConvert.SerializeObject(new { movies = results, total_results = 0, total_pages = 0, message = "未找到相关电影" });
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
            
            return JsonConvert.SerializeObject(new 
            { 
                movies = results, 
                total_results = totalResults, 
                total_pages = totalPages 
            });
        }
        
        /// <summary>
        /// 格式化电影搜索结果
        /// </summary>
        private string FormatMovieSearchResults(object results)
        {
            dynamic data = JObject.Parse(JsonConvert.SerializeObject(results));
            
            if (data.movies == null || data.movies.Count == 0)
            {
                return "很抱歉，未找到相关电影。请尝试使用不同的关键词搜索。";
            }
            
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("以下是搜索结果：\n");
            
            for (int i = 0; i < Math.Min(3, data.movies.Count); i++)
            {
                var movie = data.movies[i];
                
                sb.AppendLine($"## {movie.title}");
                if (movie.original_title != null && movie.original_title.ToString() != movie.title.ToString())
                {
                    sb.AppendLine($"原名：{movie.original_title}");
                }
                
                if (movie.release_date != null)
                {
                    sb.AppendLine($"上映日期：{movie.release_date}");
                }
                
                if (movie.vote_average != null)
                {
                    sb.AppendLine($"评分：{movie.vote_average}/10");
                }
                
                sb.AppendLine($"简介：{(string.IsNullOrEmpty((string)movie.overview) ? "暂无简介" : movie.overview)}");
                
                if (i < Math.Min(3, data.movies.Count) - 1)
                {
                    sb.AppendLine("\n---\n");
                }
            }
            
            if (data.total_results > 3)
            {
                sb.AppendLine($"\n共找到 {data.total_results} 部相关电影，仅显示前3部。");
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// 格式化人物搜索结果
        /// </summary>
        private string FormatPeopleSearchResults(object results)
        {
            dynamic data = JObject.Parse(JsonConvert.SerializeObject(results));
            
            if (data.people == null || data.people.Count == 0)
            {
                return "很抱歉，未找到相关人物。请尝试使用不同的关键词搜索。";
            }
            
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("以下是搜索结果：\n");
            
            for (int i = 0; i < Math.Min(3, data.people.Count); i++)
            {
                var person = data.people[i];
                
                sb.AppendLine($"## {person.name}");
                
                if (person.known_for_department != null)
                {
                    string department = person.known_for_department.ToString();
                    string translatedDepartment = department switch
                    {
                        "Acting" => "演员",
                        "Directing" => "导演",
                        "Writing" => "编剧",
                        "Production" => "制片人",
                        _ => department
                    };
                    
                    sb.AppendLine($"主要职业：{translatedDepartment}");
                }
                
                if (person.known_for != null && person.known_for.Count > 0)
                {
                    sb.AppendLine("代表作品：");
                    for (int j = 0; j < Math.Min(5, person.known_for.Count); j++)
                    {
                        var work = person.known_for[j];
                        sb.AppendLine($"- {work.title} ({work.media_type})");
                    }
                }
                
                if (i < Math.Min(3, data.people.Count) - 1)
                {
                    sb.AppendLine("\n---\n");
                }
            }
            
            if (data.total_results > 3)
            {
                sb.AppendLine($"\n共找到 {data.total_results} 名相关人物，仅显示前3名。");
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// 创建失败响应
        /// </summary>
        private TaskYieldUpdate CreateFailureResponse(string message)
        {
            return new TaskYieldUpdate
            {
                StatusUpdate = new TaskStatus
                {
                    State = TaskState.Failed,
                    Message = new Message
                    {
                        Role = "agent",
                        Parts = new List<Part> { new TextPart { Text = message } }
                    }
                }
            };
        }
        
        /// <summary>
        /// 创建帮助响应
        /// </summary>
        private TaskYieldUpdate CreateHelpResponse()
        {
            string helpMessage = @"我是一个由OpenAI增强的电影信息助手，可以帮您查询电影和演员的相关信息，并提供专业的电影分析。

您可以这样询问我：
1. 关于电影的问题，例如：
   - 请告诉我关于电影《盗梦空间》的信息
   - 介绍一下电影《阿凡达》
   - 《泰坦尼克号》是什么时候上映的？

2. 关于演员的问题，例如：
   - 莱昂纳多·迪卡普里奥主演了哪些电影？
   - 告诉我关于汤姆·克鲁斯的信息
   - 谁是《黑客帝国》的主演？

请告诉我您想了解什么电影或演员的信息？";

            return new TaskYieldUpdate
            {
                StatusUpdate = new TaskStatus
                {
                    State = TaskState.Completed,
                    Message = new Message
                    {
                        Role = "agent",
                        Parts = new List<Part> { new TextPart { Text = helpMessage } }
                    }
                }
            };
        }
    }
}