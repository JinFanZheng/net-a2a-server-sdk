using A2A.Server.SDK;
using A2A.Server.SDK.Schema;
using Task = System.Threading.Tasks.Task;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace MovieAgent
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // 解析命令行参数
            var isTestMode = Array.Exists(args, arg => arg == "--test");
            var serverUrl = GetArgumentValue(args, "--server-url", "http://localhost:41241/");
            
            if (isTestMode)
            {
                Console.WriteLine($"以测试客户端模式运行，服务器URL: {serverUrl}");
                await TestClient.RunTests(serverUrl);
                return;
            }
            
            // 配置
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            // 设置服务
            var serviceProvider = new ServiceCollection()
                .AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.AddConfiguration(configuration.GetSection("Logging"));
                })
                .AddSingleton(configuration)
                .AddSingleton<TmdbService>(provider => 
                {
                    var logger = provider.GetRequiredService<ILogger<TmdbService>>();
                    var apiKey = configuration["TMDB:ApiKey"] ?? 
                                 throw new InvalidOperationException("未找到TMDB API密钥，请在配置中设置TMDB:ApiKey");
                    return new TmdbService(apiKey, logger);
                })
                .AddSingleton<OpenAIService>(provider => 
                {
                    var logger = provider.GetRequiredService<ILogger<OpenAIService>>();
                    var apiKey = configuration["OpenAI:ApiKey"] ?? 
                                 throw new InvalidOperationException("未找到OpenAI API密钥，请在配置中设置OpenAI:ApiKey");
                    var baseUrl = configuration["OpenAI:BaseUrl"] ??
                                 throw new InvalidOperationException("未找到OpenAI BaseUrl，请在配置中设置OpenAI:BaseUrl");
                    return new OpenAIService(apiKey, baseUrl, logger);
                })
                .AddSingleton<MovieTaskHandler>()
                .BuildServiceProvider();

            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            var taskHandler = serviceProvider.GetRequiredService<MovieTaskHandler>();

            try
            {
                logger.LogInformation("启动电影代理服务器...");
                
                // 创建A2A服务器
                var options = new A2AServerOptions
                {
                    // 配置服务器选项
                    BasePath = "/",
                    EnableCors = true,
                    Card = new AgentCard
                    {
                        Name = "电影专家助手",
                        Description = "提供电影和演员信息的智能助手，由OpenAI增强",
                        Version = "1.0.0",
                        Url = $"http://localhost:{configuration["Server:Port"] ?? "3000"}",
                        Provider = new AgentProvider
                        {
                            Organization = "A2A",
                            Url = "https://a2a.com"
                        },
                        Capabilities = new AgentCapabilities
                        {
                            Streaming = true,
                            PushNotifications = true,
                            StateTransitionHistory = true,
                        },
                        Skills = new List<AgentSkill>
                        {
                            new AgentSkill
                            {
                                Id = "search_movies",
                                Name = "搜索电影",
                                Description = "搜索电影信息",
                            },
                            new AgentSkill
                            {
                                Id = "search_people",
                                Name = "搜索演员",
                                Description = "搜索演员信息",
                            }
                        }
                    }
                };

                // 创建A2A服务器
                var server = new A2AServer(taskHandler, options, logger);
                
                // 启动一个ASP.NET Core应用程序
                var builder = WebApplication.CreateBuilder(args);
                
                // 配置JSON序列化
                builder.Services.AddControllers().AddNewtonsoftJson(options => {
                    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                    options.SerializerSettings.Converters.Add(new StringEnumConverter());
                    options.SerializerSettings.TypeNameHandling = TypeNameHandling.Auto;
                });
                
                // 配置服务
                if (options.EnableCors)
                {
                    builder.Services.AddCors(corsOptions =>
                    {
                        corsOptions.AddPolicy("A2APolicy", builder =>
                        {
                            builder.AllowAnyOrigin()
                                  .AllowAnyMethod()
                                  .AllowAnyHeader();
                        });
                    });
                }
                
                var app = builder.Build();
                
                // 配置中间件
                if (options.EnableCors)
                {
                    app.UseCors("A2APolicy");
                }
                
                // 映射A2A路由
                app.MapPost(options.BasePath.TrimEnd('/') + "/", async (HttpContext context) =>
                {
                    await server.HandleTaskSendAsync(context);
                });
                
                // 映射/.well-known/agent.json路由，返回Card信息
                app.MapGet("/.well-known/agent.json", () => options.Card);

                app.MapGet("/agent-card", () => options.Card);
                
                // 启动应用程序
                int port = int.Parse(configuration["Server:Port"] ?? "3000");
                logger.LogInformation("电影代理服务器已启动，监听端口：{Port}", port);
                logger.LogInformation("OpenAI LLM 已连接: {BaseUrl}", configuration["OpenAI:BaseUrl"]);
                logger.LogInformation("使用系统指令: {Instructions}", GetSystemInstructions());
                logger.LogInformation("按Ctrl+C停止服务器");
                
                app.Run($"http://localhost:{port}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "服务器运行时发生错误");
            }
        }

        private static string GetArgumentValue(string[] args, string argName, string defaultValue = null)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == argName)
                {
                    return args[i + 1];
                }
            }
            return defaultValue;
        }

        private static string GetSystemInstructions()
        {
            return @"你是一个由OpenAI增强的电影专家助手，能够提供电影和演员的详细信息以及专业的电影分析。

你可以使用以下功能：
1. 搜索电影信息 - 使用search_movies命令
   参数：电影名称关键词
2. 搜索演员信息 - 使用search_people命令
   参数：演员名称关键词

你应该：
- 当用户询问电影信息时，使用search_movies命令搜索相关电影
- 当用户询问演员信息时，使用search_people命令搜索相关演员
- 整理搜索结果，以清晰易读的方式呈现给用户
- 提供电影的基本信息，包括标题、简介、上映日期、评分等
- 提供演员的基本信息，包括姓名、知名作品、所属部门等
- 利用OpenAI提供专业的电影分析和见解
- 使用中文回答用户的问题
- 如果搜索结果为空，告知用户未找到相关信息，并提供进一步的建议

示例1：
用户：请告诉我关于电影《盗梦空间》的信息。
助手：[使用search_movies搜索'盗梦空间'并整理返回信息，加上OpenAI的专业分析]

示例2：
用户：莱昂纳多·迪卡普里奥主演了哪些电影？
助手：[使用search_people搜索'莱昂纳多·迪卡普里奥'并整理返回信息，加上OpenAI的专业见解]";
        }
    }
}
