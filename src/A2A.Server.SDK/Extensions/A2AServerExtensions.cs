using A2A.Server.SDK.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace A2A.Server.SDK.Extensions
{
    /// <summary>
    /// ASP.NET Core扩展方法，用于集成A2A服务器
    /// </summary>
    public static class A2AServerExtensions
    {
        /// <summary>
        /// 将A2A服务器添加到服务容器
        /// </summary>
        /// <param name="services">服务容器</param>
        /// <param name="configureOptions">配置选项的可选操作</param>
        /// <returns>服务容器，用于链式调用</returns>
        public static IServiceCollection AddA2AServer(
            this IServiceCollection services,
            Action<A2AServerOptions>? configureOptions = null)
        {
            // 注册选项
            var options = new A2AServerOptions();
            configureOptions?.Invoke(options);

            // 如果启用了CORS，注册CORS服务
            if (options.EnableCors)
            {
                services.AddCors(corsOptions =>
                {
                    corsOptions.AddPolicy("A2APolicy", builder =>
                    {
                        builder.AllowAnyOrigin()
                               .AllowAnyMethod()
                               .AllowAnyHeader();
                    });
                });
            }

            // 注册A2A服务器选项作为单例
            services.AddSingleton(options);

            return services;
        }

        /// <summary>
        /// 将A2A服务器端点映射到应用程序管道
        /// </summary>
        /// <param name="endpoints">端点路由构建器</param>
        /// <param name="taskHandler">任务处理器实现</param>
        /// <param name="options">可选的A2A服务器选项</param>
        /// <param name="logger">可选的日志记录器</param>
        /// <returns>端点路由构建器，用于链式调用</returns>
        public static IEndpointRouteBuilder MapA2AEndpoints(
            this IEndpointRouteBuilder endpoints,
            ITaskHandler taskHandler,
            A2AServerOptions? options = null,
            ILogger? logger = null)
        {
            options ??= endpoints.ServiceProvider.GetService<A2AServerOptions>() ?? new A2AServerOptions();
            
            // 创建A2AServer实例，使用修改后的构造函数参数顺序，确保参数类型匹配
            var server = new A2AServer(taskHandler, options, logger); // 这里先传入taskHandler，再传入options

            string basePath = options.BasePath.TrimEnd('/');

            // 映射JSON-RPC端点
            endpoints.MapPost($"{basePath}", async context =>
            {
                var result = await server.HandleTaskSendAsync(context);
                await ExecuteResultAsync(result, context);
            });

            // 映射Agent Card端点
            if (server.Card != null)
            {
                endpoints.MapGet($"{basePath}/.well-known/agent.json", async context =>
                {
                    var result = server.HandleAgentCard();
                    await ExecuteResultAsync(result, context);
                });
            }

            return endpoints;
        }
        
        private static async System.Threading.Tasks.Task ExecuteResultAsync(IActionResult result, HttpContext context)
        {
            if (result is JsonResult jsonResult)
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = jsonResult.StatusCode ?? StatusCodes.Status200OK;
                await context.Response.WriteAsJsonAsync(jsonResult.Value);
            }
            else if (result is ContentResult contentResult)
            {
                context.Response.ContentType = contentResult.ContentType ?? "text/plain";
                context.Response.StatusCode = contentResult.StatusCode ?? StatusCodes.Status200OK;
                await context.Response.WriteAsync(contentResult.Content ?? string.Empty);
            }
            else if (result is StatusCodeResult statusCodeResult)
            {
                context.Response.StatusCode = statusCodeResult.StatusCode;
            }
            else if (result is EmptyResult)
            {
                // 已经通过其他方式写入响应
            }
            else
            {
                // 默认情况下，尝试执行结果
                var actionContext = new ActionContext
                {
                    HttpContext = context
                };
                await result.ExecuteResultAsync(actionContext);
            }
        }
    }
} 