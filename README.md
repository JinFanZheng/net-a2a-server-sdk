# A2A Server SDK for .NET

这个SDK提供了用于在.NET应用程序中实现A2A（Agent-to-Agent）协议服务器的工具和库。

## 特性

- 完全符合A2A协议规范
- 支持同步和流式响应
- 内存和文件系统存储选项
- 与ASP.NET Core无缝集成
- 完整的C#类型支持和文档
- 内置的任务状态管理
- 支持任务取消和恢复
- 丰富的错误处理和日志记录

## 快速开始

### 安装

安装A2A Server SDK NuGet包：

```shell
dotnet add package A2A.Server.SDK
```

### 基本用法

#### 1. 创建一个任务处理器

```csharp
using A2A.Server.SDK.Handlers;
using A2A.Server.SDK.Schema;

public class MyTaskHandler : ITaskHandler
{
    public async IAsyncEnumerable<TaskYieldUpdate> HandleTaskAsync(TaskContext context)
    {
        // 产生状态更新
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
                        new TextPart { Text = "正在处理您的请求..." }
                    }
                }
            }
        };

        // 执行工作
        await Task.Delay(1000);

        // 产生任务结果
        yield return new TaskYieldUpdate
        {
            ArtifactUpdate = new Artifact
            {
                Name = "result.txt",
                Parts = new List<Part>
                {
                    new TextPart { Text = "这是结果" }
                }
            }
        };

        // 标记为完成
        yield return new TaskYieldUpdate
        {
            StatusUpdate = new TaskStatus
            {
                State = TaskState.Completed,
                Message = new Message
                {
                    Role = "agent",
                    Parts = new List<Part>
                    {
                        new TextPart { Text = "已完成！" }
                    }
                }
            }
        };
    }
}
```

#### 2. 在ASP.NET Core应用程序中集成

```csharp
using A2A.Server.SDK.Extensions;
using A2A.Server.SDK.Handlers;

var builder = WebApplication.CreateBuilder(args);

// 添加A2A服务
builder.Services.AddA2AServer(options =>
{
    options.BasePath = "/a2a";
    options.Card = new
    {
        name = "我的A2A代理",
        description = "一个示例A2A代理",
        version = "1.0.0",
        capabilities = new
        {
            streaming = true
        },
        skills = new[]
        {
            new
            {
                id = "example",
                name = "示例技能"
            }
        }
    };
});

var app = builder.Build();

// 配置CORS（如果需要）
app.UseCors("A2APolicy");

// 映射A2A端点
app.MapA2AEndpoints(new MyTaskHandler());

app.Run();
```

#### 3. 手动使用A2AServer类

```csharp
using A2A.Server.SDK;
using A2A.Server.SDK.Handlers;
using A2A.Server.SDK.Storage;

// 创建任务处理器
var taskHandler = new MyTaskHandler();

// 创建服务器实例
var server = new A2AServer(taskHandler, new A2AServerOptions
{
    TaskStore = new FileStore("./tasks"), // 或使用InMemoryTaskStore
    BasePath = "/"
});

// 在ASP.NET Core中手动处理请求
app.MapPost("/", async (HttpContext context) =>
{
    var result = await server.HandleTaskSendAsync(context);
    await result.ExecuteResultAsync(new ActionContext { HttpContext = context });
});
```

## 高级用法

### 处理任务取消

```csharp
public async IAsyncEnumerable<TaskYieldUpdate> HandleTaskAsync(TaskContext context)
{
    // 发送工作状态更新
    yield return new TaskYieldUpdate
    {
        StatusUpdate = new TaskStatus { State = TaskState.Working }
    };

    for (int i = 0; i < 10; i++)
    {
        // 检查任务是否被取消
        if (context.IsCancelled())
        {
            yield return new TaskYieldUpdate
            {
                StatusUpdate = new TaskStatus { State = TaskState.Canceled }
            };
            yield break;
        }

        await Task.Delay(500);
        // 执行工作...
    }
    
    // 完成任务
    yield return new TaskYieldUpdate
    {
        StatusUpdate = new TaskStatus { State = TaskState.Completed }
    };
}
```

### 自定义存储实现

```csharp
public class MyCustomTaskStore : ITaskStore
{
    // 实现接口方法
    public Task<TaskAndHistory> GetTaskAsync(string taskId) 
    {
        // 实现从数据库或其他存储中获取任务
    }
    
    public Task SaveAsync(TaskAndHistory taskAndHistory)
    {
        // 实现保存任务到数据库或其他存储
    }
    
    // 其他接口方法实现...
}

// 使用自定义存储
var server = new A2AServer(handler, new A2AServerOptions
{
    TaskStore = new MyCustomTaskStore()
});
```

## 存储选项

SDK提供了多种存储实现：

- `InMemoryTaskStore`：将任务存储在内存中（开发/测试用途）
- `FileStore`：将任务存储在文件系统中（持久性存储）

您也可以通过实现`ITaskStore`接口创建自定义存储，如基于数据库的存储。

## 处理不同类型的消息部分

A2A协议支持多种消息部分类型。以下是如何处理它们：

```csharp
public async IAsyncEnumerable<TaskYieldUpdate> HandleTaskAsync(TaskContext context)
{
    // 访问用户消息部分
    foreach (var part in context.UserMessage.Parts)
    {
        if (part is TextPart textPart)
        {
            // 处理文本部分
            var text = textPart.Text;
            // ...
        }
        else if (part is ImagePart imagePart)
        {
            // 处理图像部分
            var imageUrl = imagePart.Source;
            // ...
        }
        // 其他部分类型...
    }
    
    // 返回包含多种部分类型的成品
    yield return new TaskYieldUpdate
    {
        ArtifactUpdate = new Artifact
        {
            Name = "result",
            Parts = new List<Part>
            {
                new TextPart { Text = "文本结果" },
                new ImagePart { Source = "https://example.com/image.jpg" }
            }
        }
    };
}
```

## 示例

查看项目中的`samples`和`tests`目录，了解更多示例：

- `SimpleA2AServer`：基础服务器示例
- `MovieAgent`：集成第三方API的电影信息代理

## 故障排除

### 常见问题

1. **无法接收请求**：确保正确配置了CORS和端点路径
2. **任务状态未正确更新**：检查异步流是否正确yield返回了状态更新
3. **成品未正确生成**：确保成品部分格式正确

### 启用调试日志

```csharp
var server = new A2AServer(handler, new A2AServerOptions 
{
    EnableDebugLogging = true
}, loggerFactory.CreateLogger<A2AServer>());
```

## 许可证

MIT

## 贡献

欢迎提交Pull Request。请确保在提交前运行测试并遵循项目的代码风格。