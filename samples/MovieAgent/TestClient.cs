using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MovieAgent
{
    public class TestClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _serverUrl;

        public TestClient(string serverUrl)
        {
            _serverUrl = serverUrl;
            _httpClient = new HttpClient();
        }

        public async Task SendSearchMovieRequest(string query)
        {
            try
            {
                Console.WriteLine($"发送电影搜索请求: {query}");
                
                var request = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "tasks/send",
                    @params = new
                    {
                        id = $"movie-{Guid.NewGuid()}",
                        message = new
                        {
                            role = "user",
                            parts = new[]
                            {
                                new
                                {
                                    type = "text",
                                    text = $"search_movies {query}"
                                }
                            }
                        }
                    }
                };

                var json = JsonConvert.SerializeObject(request);
                Console.WriteLine($"请求内容: {json}");

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(_serverUrl, content);

                var responseText = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"状态码: {response.StatusCode}");
                Console.WriteLine($"响应内容: {responseText}");

                // 尝试美化输出
                try
                {
                    var jobj = JObject.Parse(responseText);
                    Console.WriteLine($"格式化响应:\n{jobj.ToString(Formatting.Indented)}");
                }
                catch
                {
                    Console.WriteLine("无法格式化响应");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送请求时出错: {ex.Message}");
            }
        }

        public async Task SendSearchPeopleRequest(string query)
        {
            try
            {
                Console.WriteLine($"发送人物搜索请求: {query}");
                
                var request = new
                {
                    jsonrpc = "2.0",
                    id = 2,
                    method = "tasks/send",
                    @params = new
                    {
                        id = $"people-{Guid.NewGuid()}",
                        message = new
                        {
                            role = "user",
                            parts = new[]
                            {
                                new
                                {
                                    type = "text",
                                    text = $"search_people {query}"
                                }
                            }
                        }
                    }
                };

                var json = JsonConvert.SerializeObject(request);
                Console.WriteLine($"请求内容: {json}");

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(_serverUrl, content);

                var responseText = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"状态码: {response.StatusCode}");
                Console.WriteLine($"响应内容: {responseText}");

                // 尝试美化输出
                try
                {
                    var jobj = JObject.Parse(responseText);
                    Console.WriteLine($"格式化响应:\n{jobj.ToString(Formatting.Indented)}");
                }
                catch
                {
                    Console.WriteLine("无法格式化响应");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送请求时出错: {ex.Message}");
            }
        }

        public static async Task RunTests(string serverUrl)
        {
            var client = new TestClient(serverUrl);
            
            // 测试电影搜索
            await client.SendSearchMovieRequest("盗梦空间");
            
            // 暂停一下
            await Task.Delay(1000);
            
            // 测试人物搜索
            await client.SendSearchPeopleRequest("莱昂纳多");
        }
    }
} 