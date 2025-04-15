using A2A.Server.SDK.Errors;
using A2A.Server.SDK.Handlers;
using A2A.Server.SDK.Schema;
using A2A.Server.SDK.Storage;
using A2A.Server.SDK.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System;
using System.Collections.Generic;
using System.Threading;
using Task = System.Threading.Tasks.Task;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace A2A.Server.SDK
{
    /// <summary>
    /// A2A服务器配置选项
    /// </summary>
    public class A2AServerOptions
    {
        /// <summary>
        /// 任务存储实现。默认为InMemoryTaskStore。
        /// </summary>
        public ITaskStore? TaskStore { get; set; }

        /// <summary>
        /// Agent卡片，提供有关此服务的信息
        /// </summary>
        public dynamic? Card { get; set; }

        /// <summary>
        /// A2A端点的基本路径。默认为"/"。
        /// </summary>
        public string BasePath { get; set; } = "/";

        /// <summary>
        /// 是否启用CORS。默认为true（允许所有来源）。
        /// </summary>
        public bool EnableCors { get; set; } = true;
    }

    /// <summary>
    /// 实现一个符合A2A规范的服务器
    /// </summary>
    public class A2AServer
    {
        private readonly ITaskHandler _taskHandler;
        private readonly ITaskStore _taskStore;
        private readonly string _basePath;
        private readonly bool _enableCors;
        private readonly ILogger? _logger;
        private readonly ConcurrentDictionary<string, bool> _activeCancellations = new();
        private readonly JsonSerializerSettings _jsonSettings;

        public A2AServerOptions? Options{ get; set; }

        /// <summary>
        /// Agent卡片，包含服务描述
        /// </summary>
        public dynamic? Card { get; }

        /// <summary>
        /// 初始化A2A服务器的新实例
        /// </summary>
        /// <param name="taskHandler">处理任务的处理器</param>
        /// <param name="options">服务器配置选项</param>
        /// <param name="logger">可选的日志记录器</param>
        public A2AServer(ITaskHandler taskHandler, A2AServerOptions? options = null, ILogger? logger = null)
        {
            _taskHandler = taskHandler ?? throw new ArgumentNullException(nameof(taskHandler));
            _taskStore = options?.TaskStore ?? new InMemoryTaskStore();
            _basePath = options?.BasePath ?? "/";
            _enableCors = options?.EnableCors ?? true;
            Card = options?.Card;
            _logger = logger;

            // 确保基本路径以斜杠开始和结束，如果不是仅"/"
            if (_basePath != "/")
            {
                _basePath = $"/{_basePath.Trim('/')}/";
            }

            _jsonSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                Converters = { new StringEnumConverter(), new PartConverter() },
                TypeNameHandling = TypeNameHandling.Auto
            };
            Options = options;
        }

        /// <summary>
        /// 应用更新（状态或成品）到任务和历史记录（不可变方式）
        /// </summary>
        private TaskAndHistory ApplyUpdateToTaskAndHistory(
            TaskAndHistory current,
            TaskYieldUpdate update)
        {
            var newTask = new Schema.Task // 浅拷贝任务
            {
                Id = current.Task.Id,
                SessionId = current.Task.SessionId,
                Status = current.Task.Status,
                Artifacts = current.Task.Artifacts != null ? new List<Artifact>(current.Task.Artifacts) : null,
                Metadata = current.Task.Metadata != null ? new Dictionary<string, object>(current.Task.Metadata) : null
            };
            
            var newHistory = new List<Message>(current.History); // 浅拷贝历史记录

            if (update.IsStatusUpdate() && update.StatusUpdate != null)
            {
                // 合并状态更新
                newTask.Status = new Schema.TaskStatus
                {
                    State = update.StatusUpdate.State,
                    Message = update.StatusUpdate.Message,
                    Timestamp = DateTimeUtils.GetCurrentTimestamp() // 始终更新时间戳
                };

                // 如果更新包含代理消息，将其添加到历史记录
                if (update.StatusUpdate.Message?.Role == "agent")
                {
                    newHistory.Add(update.StatusUpdate.Message);
                }
            }
            else if (update.IsArtifactUpdate() && update.ArtifactUpdate != null)
            {
                // 处理成品更新
                if (newTask.Artifacts == null)
                {
                    newTask.Artifacts = new List<Artifact>();
                }

                var artifact = update.ArtifactUpdate;
                int existingIndex = artifact.Index.GetValueOrDefault(-1); // 如果提供了索引，则使用它
                bool replaced = false;

                if (existingIndex >= 0 && existingIndex < newTask.Artifacts.Count)
                {
                    var existingArtifact = newTask.Artifacts[existingIndex];
                    if (artifact.Append == true)
                    {
                        // 创建深拷贝以进行修改，避免修改原始对象
                        var appendedArtifact = new Artifact
                        {
                            Name = existingArtifact.Name,
                            Description = existingArtifact.Description,
                            Index = existingArtifact.Index,
                            Append = existingArtifact.Append,
                            LastChunk = artifact.LastChunk ?? existingArtifact.LastChunk,
                            Metadata = existingArtifact.Metadata != null
                                ? new Dictionary<string, object>(existingArtifact.Metadata)
                                : null,
                            Parts = new List<Part>(existingArtifact.Parts)
                        };

                        // 附加新部分
                        foreach (var part in artifact.Parts)
                        {
                            appendedArtifact.Parts.Add(part);
                        }

                        // 如果有元数据，合并
                        if (artifact.Metadata != null)
                        {
                            if (appendedArtifact.Metadata == null)
                            {
                                appendedArtifact.Metadata = new Dictionary<string, object>();
                            }
                            foreach (var pair in artifact.Metadata)
                            {
                                appendedArtifact.Metadata[pair.Key] = pair.Value;
                            }
                        }

                        // 如果有描述，更新
                        if (!string.IsNullOrEmpty(artifact.Description))
                        {
                            appendedArtifact.Description = artifact.Description;
                        }

                        newTask.Artifacts[existingIndex] = appendedArtifact; // 用附加版本替换
                        replaced = true;
                    }
                    else
                    {
                        // 在索引处覆盖成品（使用更新的副本）
                        newTask.Artifacts[existingIndex] = artifact;
                        replaced = true;
                    }
                }
                else if (!string.IsNullOrEmpty(artifact.Name))
                {
                    // 按名称查找
                    int namedIndex = newTask.Artifacts.FindIndex(a => a.Name == artifact.Name);
                    if (namedIndex >= 0)
                    {
                        newTask.Artifacts[namedIndex] = artifact; // 按名称替换（使用副本）
                        replaced = true;
                    }
                }

                if (!replaced)
                {
                    newTask.Artifacts.Add(artifact); // 添加为新成品（副本）
                    // 如果存在索引，则排序
                    if (newTask.Artifacts.Any(a => a.Index.HasValue))
                    {
                        newTask.Artifacts = newTask.Artifacts
                            .OrderBy(a => a.Index.GetValueOrDefault(0))
                            .ToList();
                    }
                }
            }

            return new TaskAndHistory { Task = newTask, History = newHistory };
        }

        /// <summary>
        /// 创建任务上下文
        /// </summary>
        private TaskContext CreateTaskContext(
            Schema.Task task,
            Message userMessage,
            List<Message> history)
        {
            return new TaskContext
            {
                Task = task,
                UserMessage = userMessage,
                History = history,
                IsCancelled = () => _activeCancellations.ContainsKey(task.Id)
            };
        }

        /// <summary>
        /// 验证基本JSON-RPC请求结构
        /// </summary>
        private bool IsValidJsonRpcRequest(object? body)
        {
            if (body == null) return false;
            
            var type = body.GetType();
            var hasMethod = type.GetProperty("Method") != null;
            
            return Utils.TypeChecks.IsObject(body) && hasMethod;
        }

        /// <summary>
        /// 创建成功响应
        /// </summary>
        private JSONRPCResponse<T> CreateSuccessResponse<T>(object? id, T result)
        {
            return new JSONRPCResponse<T>
            {
                JsonRpc = "2.0",
                Id = id,
                Result = result
            };
        }

        /// <summary>
        /// 创建错误响应
        /// </summary>
        private JSONRPCResponse<object> CreateErrorResponse(object? id, JSONRPCError error)
        {
            return new JSONRPCResponse<object>
            {
                JsonRpc = "2.0",
                Id = id,
                Error = error
            };
        }

        /// <summary>
        /// 规范化错误
        /// </summary>
        private JSONRPCResponse<object> NormalizeError(Exception error, object? reqId, string? taskId = null)
        {
            var jsonRpcError = error switch
            {
                A2AError a2aError => a2aError.ToJSONRPCError(),
                _ => new JSONRPCError
                {
                    Code = A2AError.InternalError,
                    Message = $"内部服务器错误: {error.Message}"
                }
            };

            _logger?.LogError(error, "处理A2A请求时出错 [TaskId: {TaskId}]: {Message}", taskId, error.Message);
            
            return CreateErrorResponse(reqId, jsonRpcError);
        }

        /// <summary>
        /// 处理任务/发送请求
        /// </summary>
        public async Task<IActionResult> HandleTaskSendAsync(HttpContext context)
        {
            try
            {
                _logger?.LogInformation("开始处理请求");
                
                // 这将由ASP.NET Core中间件从请求正文解析
                string requestBody;
                using (var reader = new System.IO.StreamReader(context.Request.Body))
                {
                    requestBody = await reader.ReadToEndAsync();
                }
                
                _logger?.LogInformation("收到请求: {Request}", requestBody);
                
                var requestObj = JsonConvert.DeserializeObject<JSONRPCRequest>(requestBody, _jsonSettings);
                _logger?.LogInformation("反序列化请求成功，方法: {Method}", requestObj?.Method);
                
                string? taskId = null;
                
                try
                {
                    // 1. 验证基本JSON-RPC结构
                    if (!IsValidJsonRpcRequest(requestObj))
                    {
                        throw A2AError.CreateInvalidRequest("无效的JSON-RPC请求结构。");
                    }

                    // 2. 根据方法路由
                    switch (requestObj?.Method)
                    {
                        case "tasks/send":
                            _logger?.LogInformation("处理tasks/send请求");
                            return await HandleTaskSendInternalAsync(requestObj, context);
                        case "tasks/sendSubscribe":
                            _logger?.LogInformation("处理tasks/sendSubscribe请求");
                            return await HandleTaskSendSubscribeInternalAsync(requestObj, context);
                        case "tasks/get":
                            _logger?.LogInformation("处理tasks/get请求");
                            return await HandleTaskGetInternalAsync(requestObj, context);
                        case "tasks/cancel":
                            _logger?.LogInformation("处理tasks/cancel请求");
                            return await HandleTaskCancelInternalAsync(requestObj, context);
                        default:
                            throw A2AError.CreateMethodNotFound(requestObj?.Method ?? "未知");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "处理请求时发生错误: {Error}", ex.Message);
                    var errorResponse = NormalizeError(ex, requestObj?.Id, taskId);
                    return new JsonResult(errorResponse)
                    {
                        StatusCode = StatusCodes.Status400BadRequest
                    };
                }
            }
            catch (Exception ex) 
            {
                _logger?.LogError(ex, "处理请求的最外层异常: {Error}", ex.Message);
                return new JsonResult(new { error = $"服务器错误: {ex.Message}" })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }

        /// <summary>
        /// 内部实现tasks/send请求处理
        /// </summary>
        private async Task<IActionResult> HandleTaskSendInternalAsync(JSONRPCRequest request, HttpContext context)
        {
            // 解析参数
            var reqParams = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                request.Params?.ToString() ?? "{}", _jsonSettings);
            
            if (reqParams == null || 
                !reqParams.TryGetValue("id", out var idObj) || 
                !reqParams.TryGetValue("message", out var messageObj))
            {
                throw A2AError.CreateInvalidParams("tasks/send需要id和message参数。");
            }

            string taskId = idObj.ToString() ?? string.Empty;
            var message = JsonConvert.DeserializeObject<Message>(messageObj.ToString() ?? "{}", _jsonSettings);
            
            if (message == null)
            {
                throw A2AError.CreateInvalidParams("无效的消息格式。");
            }

            // 获取可选参数
            string? sessionId = null;
            if (reqParams.TryGetValue("sessionId", out var sessionIdObj))
            {
                sessionId = sessionIdObj.ToString();
            }

            Dictionary<string, object>? metadata = null;
            if (reqParams.TryGetValue("metadata", out var metadataObj))
            {
                metadata = JsonConvert.DeserializeObject<Dictionary<string, object>>(metadataObj.ToString() ?? "{}", _jsonSettings);
            }

            // 加载或创建任务
            var taskAndHistory = await LoadOrCreateTaskAndHistoryAsync(taskId, message, sessionId, metadata);
            
            // 创建处理上下文
            var taskContext = CreateTaskContext(taskAndHistory.Task, message, taskAndHistory.History);
            
            // 处理任务
            await foreach (var update in _taskHandler.HandleTaskAsync(taskContext))
            {
                // 应用每个更新
                taskAndHistory = ApplyUpdateToTaskAndHistory(taskAndHistory, update);
                
                // 保存更新的状态
                await _taskStore.SaveAsync(taskAndHistory);
                
                // 如果任务完成、取消或失败，停止处理
                if (taskAndHistory.Task.Status.State == TaskState.Completed ||
                    taskAndHistory.Task.Status.State == TaskState.Canceled ||
                    taskAndHistory.Task.Status.State == TaskState.Failed)
                {
                    break;
                }
            }
            
            // 返回最终任务状态
            var response = CreateSuccessResponse(request.Id, taskAndHistory.Task);
            return new JsonResult(response);
        }

        /// <summary>
        /// 内部实现tasks/sendSubscribe请求处理
        /// </summary>
        private async Task<IActionResult> HandleTaskSendSubscribeInternalAsync(JSONRPCRequest request, HttpContext context)
        {
            // 解析参数
            var reqParams = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                request.Params?.ToString() ?? "{}", _jsonSettings);
            
            if (reqParams == null || 
                !reqParams.TryGetValue("id", out var idObj) || 
                !reqParams.TryGetValue("message", out var messageObj))
            {
                throw A2AError.CreateInvalidParams("tasks/sendSubscribe需要id和message参数。");
            }

            string taskId = idObj.ToString() ?? string.Empty;
            var message = JsonConvert.DeserializeObject<Message>(messageObj.ToString() ?? "{}", _jsonSettings);
            
            if (message == null)
            {
                throw A2AError.CreateInvalidParams("无效的消息格式。");
            }

            // 获取可选参数
            string? sessionId = null;
            if (reqParams.TryGetValue("sessionId", out var sessionIdObj))
            {
                sessionId = sessionIdObj.ToString();
            }

            Dictionary<string, object>? metadata = null;
            if (reqParams.TryGetValue("metadata", out var metadataObj))
            {
                metadata = JsonConvert.DeserializeObject<Dictionary<string, object>>(metadataObj.ToString() ?? "{}", _jsonSettings);
            }
            
            // 设置响应头，启用SSE
            context.Response.Headers["Content-Type"] = "text/event-stream";
            context.Response.Headers["Cache-Control"] = "no-cache";
            context.Response.Headers["Connection"] = "keep-alive";

            // Helper to send SSE events
            async Task SendEventAsync(object eventData)
            {
                var json = JsonConvert.SerializeObject(eventData, _jsonSettings);
                await context.Response.WriteAsync($"data: {json}\n\n");
                await context.Response.Body.FlushAsync();
            }

            try
            {
                // 加载或创建任务
                var taskAndHistory = await LoadOrCreateTaskAndHistoryAsync(taskId, message, sessionId, metadata);
                
                // 创建处理上下文
                var taskContext = CreateTaskContext(taskAndHistory.Task, message, taskAndHistory.History);
                
                // 发送初始状态事件
                var initialStatusEvent = CreateTaskStatusEvent(taskId, taskAndHistory.Task.Status, false);
                await SendEventAsync(CreateSuccessResponse(request.Id, initialStatusEvent));
                
                // 发送初始成品事件（如果有）
                if (taskAndHistory.Task.Artifacts != null)
                {
                    foreach (var artifact in taskAndHistory.Task.Artifacts)
                    {
                        var artifactEvent = CreateTaskArtifactEvent(taskId, artifact, false);
                        await SendEventAsync(CreateSuccessResponse(request.Id, artifactEvent));
                    }
                }
                
                // 处理任务并流式传输更新
                await foreach (var update in _taskHandler.HandleTaskAsync(taskContext))
                {
                    // 应用更新
                    taskAndHistory = ApplyUpdateToTaskAndHistory(taskAndHistory, update);
                    
                    // 保存更新的状态
                    await _taskStore.SaveAsync(taskAndHistory);
                    
                    // 发送事件
                    if (update.IsStatusUpdate() && update.StatusUpdate != null)
                    {
                        bool isFinal = update.StatusUpdate.State == TaskState.Completed ||
                                   update.StatusUpdate.State == TaskState.Canceled ||
                                   update.StatusUpdate.State == TaskState.Failed;
                        
                        var statusEvent = CreateTaskStatusEvent(
                            taskId, taskAndHistory.Task.Status, isFinal);
                        
                        await SendEventAsync(CreateSuccessResponse(request.Id, statusEvent));
                        
                        if (isFinal)
                        {
                            break; // 完成 - 停止流
                        }
                    }
                    else if (update.IsArtifactUpdate() && update.ArtifactUpdate != null)
                    {
                        var artifactEvent = CreateTaskArtifactEvent(
                            taskId, update.ArtifactUpdate, false);
                        
                        await SendEventAsync(CreateSuccessResponse(request.Id, artifactEvent));
                    }
                }
                
                // 发送最终空事件以表示结束
                await SendEventAsync(CreateSuccessResponse<object?>(request.Id, null));
                
                return new EmptyResult(); // 流已完成
            }
            catch (Exception ex)
            {
                // 在流模式下处理错误
                var errorResponse = NormalizeError(ex, request.Id, taskId);
                await SendEventAsync(errorResponse);
                return new EmptyResult();
            }
        }

        /// <summary>
        /// 内部实现tasks/get请求处理
        /// </summary>
        private async Task<IActionResult> HandleTaskGetInternalAsync(JSONRPCRequest request, HttpContext context)
        {
            // 解析参数
            var reqParams = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                request.Params?.ToString() ?? "{}", _jsonSettings);
            
            if (reqParams == null || !reqParams.TryGetValue("id", out var idObj))
            {
                throw A2AError.CreateInvalidParams("tasks/get需要id参数。");
            }

            string taskId = idObj.ToString() ?? string.Empty;
            
            // 加载任务
            var taskAndHistory = await _taskStore.LoadAsync(taskId);
            if (taskAndHistory == null)
            {
                throw A2AError.CreateTaskNotFound(taskId);
            }
            
            // 返回任务
            var response = CreateSuccessResponse(request.Id, taskAndHistory.Task);
            return new JsonResult(response);
        }

        /// <summary>
        /// 内部实现tasks/cancel请求处理
        /// </summary>
        private async Task<IActionResult> HandleTaskCancelInternalAsync(JSONRPCRequest request, HttpContext context)
        {
            // 解析参数
            var reqParams = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                request.Params?.ToString() ?? "{}", _jsonSettings);
            
            if (reqParams == null || !reqParams.TryGetValue("id", out var idObj))
            {
                throw A2AError.CreateInvalidParams("tasks/cancel需要id参数。");
            }

            string taskId = idObj.ToString() ?? string.Empty;
            
            // 加载任务
            var taskAndHistory = await _taskStore.LoadAsync(taskId);
            if (taskAndHistory == null)
            {
                throw A2AError.CreateTaskNotFound(taskId);
            }
            
            // 检查是否可以取消
            if (taskAndHistory.Task.Status.State == TaskState.Completed ||
                taskAndHistory.Task.Status.State == TaskState.Canceled ||
                taskAndHistory.Task.Status.State == TaskState.Failed)
            {
                throw A2AError.CreateTaskNotCancelable(taskId);
            }
            
            // 标记为正在取消
            _activeCancellations[taskId] = true;
            
            // 修改任务状态
            taskAndHistory.Task.Status = new Schema.TaskStatus
            {
                State = TaskState.Canceled,
                Timestamp = DateTimeUtils.GetCurrentTimestamp(),
                Message = new Message
                {
                    Role = "agent",
                    Parts = new List<Part>
                    {
                        new TextPart { Text = "任务已取消。" }
                    }
                }
            };
            
            // 保存更新的状态
            await _taskStore.SaveAsync(taskAndHistory);
            
            // 返回已取消的任务
            var response = CreateSuccessResponse(request.Id, taskAndHistory.Task);
            return new JsonResult(response);
        }

        /// <summary>
        /// 加载或创建任务和历史记录
        /// </summary>
        private async Task<TaskAndHistory> LoadOrCreateTaskAndHistoryAsync(
            string taskId,
            Message initialMessage,
            string? sessionId = null,
            Dictionary<string, object>? metadata = null)
        {
            var existing = await _taskStore.LoadAsync(taskId);
            
            if (existing != null)
            {
                // 添加新消息到历史
                if (initialMessage.Role == "user")
                {
                    existing.History.Add(initialMessage);
                }
                
                // 如果任务已完成、取消或失败，重置为已提交状态
                if (existing.Task.Status.State == TaskState.Completed ||
                    existing.Task.Status.State == TaskState.Canceled ||
                    existing.Task.Status.State == TaskState.Failed)
                {
                    existing.Task.Status = new Schema.TaskStatus
                    {
                        State = TaskState.Submitted,
                        Timestamp = DateTimeUtils.GetCurrentTimestamp()
                    };
                }
                
                return existing;
            }
            
            // 创建新任务
            var newTaskAndHistory = new TaskAndHistory
            {
                Task = new Schema.Task
                {
                    Id = taskId,
                    SessionId = sessionId,
                    Status = new Schema.TaskStatus
                    {
                        State = TaskState.Submitted,
                        Timestamp = DateTimeUtils.GetCurrentTimestamp()
                    },
                    Metadata = metadata
                },
                History = new List<Message> { initialMessage }
            };
            
            // 保存初始任务
            await _taskStore.SaveAsync(newTaskAndHistory);
            
            return newTaskAndHistory;
        }

        /// <summary>
        /// 创建任务状态更新事件
        /// </summary>
        private TaskStatusUpdateEvent CreateTaskStatusEvent(string taskId, Schema.TaskStatus status, bool final)
        {
            return new TaskStatusUpdateEvent
            {
                Id = taskId,
                Status = status,
                Final = final
            };
        }

        /// <summary>
        /// 创建任务成品更新事件
        /// </summary>
        private TaskArtifactUpdateEvent CreateTaskArtifactEvent(string taskId, Artifact artifact, bool final)
        {
            return new TaskArtifactUpdateEvent
            {
                Id = taskId,
                Artifact = artifact,
                Final = final
            };
        }

        /// <summary>
        /// 处理Agent卡片请求
        /// </summary>
        public IActionResult HandleAgentCard()
        {
            if (Card == null)
            {
                return new NotFoundResult();
            }
            
            return new JsonResult(Card);
        }
    }
}