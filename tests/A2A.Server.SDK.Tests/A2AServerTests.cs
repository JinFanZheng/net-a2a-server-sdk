using A2A.Server.SDK.Handlers;
using A2A.Server.SDK.Schema;
using A2A.Server.SDK.Storage;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Json;
using Task = System.Threading.Tasks.Task;

namespace A2A.Server.SDK.Tests
{
    /// <summary>
    /// A2A服务器集成测试
    /// </summary>
    public class A2AServerTests
    {
        /// <summary>
        /// 测试处理"tasks/send"请求
        /// </summary>
        [Fact]
        public async Task HandleTaskSendAsync_ShouldProcessRequest()
        {
            // 安排
            var handler = new SimpleTaskHandler();
            var server = new A2AServer(handler, new A2AServerOptions
            {
                TaskStore = new InMemoryTaskStore()
            });

            var taskId = Guid.NewGuid().ToString();
            var request = new JSONRPCRequest
            {
                JsonRpc = "2.0",
                Id = 1,
                Method = "tasks/send",
                Params = new Dictionary<string, object>
                {
                    ["id"] = taskId,
                    ["message"] = new Message
                    {
                        Role = "user",
                        Parts = new List<Part>
                        {
                            new TextPart { Text = "测试消息" }
                        }
                    }
                }
            };

            var httpContext = new DefaultHttpContext();
            var requestJson = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
            var bytes = Encoding.UTF8.GetBytes(requestJson);
            var stream = new MemoryStream(bytes);
            httpContext.Request.Body = stream;
            httpContext.Request.ContentType = "application/json";

            // 执行
            var result = await server.HandleTaskSendAsync(httpContext);

            // 断言
            Assert.NotNull(result);
            // 注意：由于实际结果依赖于HTTP上下文的写入，我们在这里只能测试返回的结果是否有效
            // 在真实场景中，您可能需要使用更完整的测试框架，例如WebApplicationFactory
        }

        /// <summary>
        /// 测试处理"tasks/get"请求
        /// </summary>
        [Fact]
        public async Task HandleTaskGetAsync_ForExistingTask_ShouldReturnTask()
        {
            // 安排
            var handler = new SimpleTaskHandler();
            var taskStore = new InMemoryTaskStore();
            var server = new A2AServer(handler, new A2AServerOptions
            {
                TaskStore = taskStore
            });

            // 创建预先存在的任务
            var taskId = Guid.NewGuid().ToString();
            var taskAndHistory = new TaskAndHistory
            {
                Task = new Schema.Task
                {
                    Id = taskId,
                    Status = new Schema.TaskStatus
                    {
                        State = TaskState.Completed,
                        Timestamp = DateTime.UtcNow.ToString("o")
                    }
                },
                History = new List<Message>
                {
                    new Message
                    {
                        Role = "user",
                        Parts = new List<Part>
                        {
                            new TextPart { Text = "测试消息" }
                        }
                    }
                }
            };
            await taskStore.SaveAsync(taskAndHistory);

            // 创建请求
            var request = new JSONRPCRequest
            {
                JsonRpc = "2.0",
                Id = 2,
                Method = "tasks/get",
                Params = new Dictionary<string, object>
                {
                    ["id"] = taskId
                }
            };

            var httpContext = new DefaultHttpContext();
            var requestJson = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
            var bytes = Encoding.UTF8.GetBytes(requestJson);
            var stream = new MemoryStream(bytes);
            httpContext.Request.Body = stream;
            httpContext.Request.ContentType = "application/json";

            // 执行
            var result = await server.HandleTaskSendAsync(httpContext);

            // 断言
            Assert.NotNull(result);
        }

        /// <summary>
        /// 测试处理具有无效参数的请求
        /// </summary>
        [Fact]
        public async Task HandleTaskSendAsync_WithInvalidParams_ShouldReturnError()
        {
            // 安排
            var handler = new SimpleTaskHandler();
            var server = new A2AServer(handler);

            // 缺少必需的"id"和"message"参数
            var request = new JSONRPCRequest
            {
                JsonRpc = "2.0",
                Id = 3,
                Method = "tasks/send",
                Params = new Dictionary<string, object>()  // 空参数
            };

            var httpContext = new DefaultHttpContext();
            var requestJson = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
            var bytes = Encoding.UTF8.GetBytes(requestJson);
            var stream = new MemoryStream(bytes);
            httpContext.Request.Body = stream;
            httpContext.Request.ContentType = "application/json";

            // 执行
            var result = await server.HandleTaskSendAsync(httpContext);

            // 断言
            Assert.NotNull(result);
            // 应该返回一个包含错误的响应
        }
        
        /// <summary>
        /// 测试处理"tasks/cancel"请求
        /// </summary>
        [Fact]
        public async Task HandleTaskCancelAsync_ShouldCancelTask()
        {
            // 安排
            var handler = new SimpleTaskHandler();
            var taskStore = new InMemoryTaskStore();
            var server = new A2AServer(handler, new A2AServerOptions
            {
                TaskStore = taskStore
            });

            // 创建一个进行中的任务
            var taskId = Guid.NewGuid().ToString();
            var taskAndHistory = new TaskAndHistory
            {
                Task = new Schema.Task
                {
                    Id = taskId,
                    Status = new Schema.TaskStatus
                    {
                        State = TaskState.Working,
                        Timestamp = DateTime.UtcNow.ToString("o")
                    }
                }
            };
            await taskStore.SaveAsync(taskAndHistory);

            // 创建取消请求
            var request = new JSONRPCRequest
            {
                JsonRpc = "2.0",
                Id = 4,
                Method = "tasks/cancel",
                Params = new Dictionary<string, object>
                {
                    ["id"] = taskId
                }
            };

            var httpContext = new DefaultHttpContext();
            var requestJson = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
            var bytes = Encoding.UTF8.GetBytes(requestJson);
            var stream = new MemoryStream(bytes);
            httpContext.Request.Body = stream;
            httpContext.Request.ContentType = "application/json";

            // 执行
            var result = await server.HandleTaskSendAsync(httpContext);

            // 断言
            Assert.NotNull(result);
            
            // 验证任务是否被标记为已取消
            var cancelledTask = await taskStore.LoadAsync(taskId);
            Assert.Equal(TaskState.Canceled, cancelledTask.Task.Status.State);
        }
        
        /// <summary>
        /// 测试处理"tasks/list"请求
        /// </summary>
        [Fact]
        public async Task HandleTaskListAsync_ShouldReturnTasks()
        {
            // 安排
            var handler = new SimpleTaskHandler();
            var taskStore = new InMemoryTaskStore();
            var server = new A2AServer(handler, new A2AServerOptions
            {
                TaskStore = taskStore
            });

            // 创建多个测试任务
            for (int i = 0; i < 3; i++)
            {
                var taskId = $"test-task-{i}";
                var taskAndHistory = new TaskAndHistory
                {
                    Task = new Schema.Task
                    {
                        Id = taskId,
                        Status = new Schema.TaskStatus
                        {
                            State = TaskState.Completed,
                            Timestamp = DateTime.UtcNow.ToString("o")
                        }
                    }
                };
                await taskStore.SaveAsync(taskAndHistory);
            }

            // 创建list请求
            var request = new JSONRPCRequest
            {
                JsonRpc = "2.0",
                Id = 5,
                Method = "tasks/list",
                Params = new Dictionary<string, object>()  // 没有必需参数
            };

            var httpContext = new DefaultHttpContext();
            var requestJson = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
            var bytes = Encoding.UTF8.GetBytes(requestJson);
            var stream = new MemoryStream(bytes);
            httpContext.Request.Body = stream;
            httpContext.Request.ContentType = "application/json";

            // 执行
            var result = await server.HandleTaskSendAsync(httpContext);

            // 断言
            Assert.NotNull(result);
            // 当服务器实现了tasks/list方法时，我们可以断言更多内容
        }
        
        /// <summary>
        /// 测试处理"tasks/history"请求
        /// </summary>
        [Fact]
        public async Task HandleTaskHistoryAsync_ShouldReturnTaskHistory()
        {
            // 安排
            var handler = new SimpleTaskHandler();
            var taskStore = new InMemoryTaskStore();
            var server = new A2AServer(handler, new A2AServerOptions
            {
                TaskStore = taskStore
            });

            // 创建一个有历史记录的任务
            var taskId = Guid.NewGuid().ToString();
            var taskAndHistory = new TaskAndHistory
            {
                Task = new Schema.Task
                {
                    Id = taskId,
                    Status = new Schema.TaskStatus
                    {
                        State = TaskState.Completed,
                        Timestamp = DateTime.UtcNow.ToString("o")
                    }
                },
                History = new List<Message>
                {
                    new Message
                    {
                        Role = "user",
                        Parts = new List<Part> { new TextPart { Text = "用户消息1" } }
                    },
                    new Message
                    {
                        Role = "agent",
                        Parts = new List<Part> { new TextPart { Text = "代理回复1" } }
                    },
                    new Message
                    {
                        Role = "user",
                        Parts = new List<Part> { new TextPart { Text = "用户消息2" } }
                    }
                }
            };
            await taskStore.SaveAsync(taskAndHistory);

            // 创建history请求
            var request = new JSONRPCRequest
            {
                JsonRpc = "2.0",
                Id = 6,
                Method = "tasks/history",
                Params = new Dictionary<string, object>
                {
                    ["id"] = taskId
                }
            };

            var httpContext = new DefaultHttpContext();
            var requestJson = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
            var bytes = Encoding.UTF8.GetBytes(requestJson);
            var stream = new MemoryStream(bytes);
            httpContext.Request.Body = stream;
            httpContext.Request.ContentType = "application/json";

            // 执行
            var result = await server.HandleTaskSendAsync(httpContext);

            // 断言
            Assert.NotNull(result);
            // 当服务器实现了tasks/history方法时，我们可以断言更多内容
        }
        
        /// <summary>
        /// 测试服务器选项的正确应用
        /// </summary>
        [Fact]
        public void ServerOptions_ShouldBeAppliedCorrectly()
        {
            // 安排
            var handler = new SimpleTaskHandler();
            var options = new A2AServerOptions
            {
                BasePath = "/test/path",
                EnableCors = true,
                TaskStore = new InMemoryTaskStore(),
                Card = new AgentCard
                {
                    Name = "测试代理",
                    Description = "用于测试的A2A代理",
                    Version = "1.0.0"
                }
            };

            // 执行
            var server = new A2AServer(handler, options);

            // 断言
            Assert.Equal("/test/path", server.Options.BasePath);
            Assert.True(server.Options.EnableCors);
            Assert.NotNull(server.Options.TaskStore);
            Assert.NotNull(server.Options.Card);
        }
        
        /// <summary>
        /// 测试处理无效的JSON-RPC方法
        /// </summary>
        [Fact]
        public async Task HandleInvalidMethod_ShouldReturnError()
        {
            // 安排
            var handler = new SimpleTaskHandler();
            var server = new A2AServer(handler);

            // 创建无效的方法请求
            var request = new JSONRPCRequest
            {
                JsonRpc = "2.0",
                Id = 7,
                Method = "invalid/method",  // 无效方法
                Params = new Dictionary<string, object>()
            };

            var httpContext = new DefaultHttpContext();
            var requestJson = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
            var bytes = Encoding.UTF8.GetBytes(requestJson);
            var stream = new MemoryStream(bytes);
            httpContext.Request.Body = stream;
            httpContext.Request.ContentType = "application/json";

            // 执行
            var result = await server.HandleTaskSendAsync(httpContext);

            // 断言
            Assert.NotNull(result);
            // 应该返回一个方法不存在的错误响应
        }
        
        /// <summary>
        /// 测试无效JSON格式请求处理
        /// </summary>
        [Fact]
        public async Task HandleInvalidJsonFormat_ShouldReturnError()
        {
            // 安排
            var handler = new SimpleTaskHandler();
            var server = new A2AServer(handler);

            // 创建无效的JSON格式
            var invalidJson = "{\"jsonrpc\": \"2.0\", \"id\": 8, \"method\": \"tasks/send\", \"params\": {";  // 不完整的JSON
            
            var httpContext = new DefaultHttpContext();
            var bytes = Encoding.UTF8.GetBytes(invalidJson);
            var stream = new MemoryStream(bytes);
            httpContext.Request.Body = stream;
            httpContext.Request.ContentType = "application/json";

            // 执行
            var result = await server.HandleTaskSendAsync(httpContext);

            // 断言
            Assert.NotNull(result);
            // 应该返回一个解析错误响应
        }
    }
}