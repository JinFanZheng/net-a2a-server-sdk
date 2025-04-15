using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MovieAgent
{
    public class OpenAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly ILogger<OpenAIService> _logger;

        public OpenAIService(string apiKey, string baseUrl, ILogger<OpenAIService> logger)
        {
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        /// <summary>
        /// 使用OpenAI的LLM生成回答
        /// </summary>
        public async Task<string> GenerateResponseAsync(string prompt, string? systemPrompt = null, double temperature = 0.7)
        {
            try
            {
                _logger.LogInformation("向OpenAI发送请求");
                
                var messages = new List<object>();
                
                if (!string.IsNullOrEmpty(systemPrompt))
                {
                    messages.Add(new { role = "system", content = systemPrompt });
                }
                
                messages.Add(new { role = "user", content = prompt });
                
                var requestBody = new
                {
                    model = "gpt-3.5-turbo",
                    messages = messages,
                    temperature = temperature,
                    max_tokens = 1000
                };
                
                var jsonContent = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                var url = $"{_baseUrl.TrimEnd('/')}/chat/completions";
                var response = await _httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonResponse = JObject.Parse(responseContent);
                
                _logger.LogInformation("OpenAI请求成功");
                
                // 从响应中提取文本内容
                var responseText = jsonResponse["choices"]?[0]?["message"]?["content"]?.ToString() ?? string.Empty;
                return responseText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "调用OpenAI时发生错误");
                return $"抱歉，无法连接到AI服务。错误信息: {ex.Message}";
            }
        }
        
        /// <summary>
        /// 使用OpenAI丰富电影搜索结果
        /// </summary>
        public async Task<string> EnrichMovieResultsAsync(string movieResults)
        {
            var prompt = $@"下面是一些电影搜索结果，请基于这些信息，以电影专家的身份对这些电影进行更深入的分析和推荐：

{movieResults}

请提供：
1. 对这些电影的简要分析（风格、导演特点、创新点等）
2. 这些电影的历史地位或影响
3. 适合什么样的观众
4. 如果喜欢这些电影，可能也会喜欢的其他电影推荐";

            return await GenerateResponseAsync(prompt, "你是一位专业的电影评论专家，对电影历史、风格和导演特点有深入了解。");
        }
        
        /// <summary>
        /// 使用OpenAI丰富人物搜索结果
        /// </summary>
        public async Task<string> EnrichPeopleResultsAsync(string peopleResults)
        {
            var prompt = $@"下面是一些演员/导演的搜索结果，请基于这些信息，以电影专家的身份对这些人物进行更深入的分析：

{peopleResults}

请提供：
1. 对这些人物的演艺风格或导演风格的分析
2. 他们职业生涯的亮点和成就
3. 他们在电影史上的地位
4. 推荐这些人物最具代表性或最值得观看的作品";

            return await GenerateResponseAsync(prompt, "你是一位专业的电影评论专家，对演员表演风格和导演艺术风格有深入了解。");
        }
    }
}