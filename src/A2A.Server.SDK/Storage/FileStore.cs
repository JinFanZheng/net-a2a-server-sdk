using A2A.Server.SDK.Errors;
using A2A.Server.SDK.Schema;
using System.Text.Json;
using System.Threading.Tasks;

namespace A2A.Server.SDK.Storage
{
    /// <summary>
    /// 使用文件系统实现的任务存储
    /// </summary>
    public class FileStore : ITaskStore
    {
        private readonly string _baseDir;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// 创建新的文件存储实例
        /// </summary>
        /// <param name="baseDir">存储任务文件的基础目录（可选）</param>
        public FileStore(string? baseDir = null)
        {
            // 默认目录相对于当前工作目录
            _baseDir = baseDir ?? Path.Combine(Directory.GetCurrentDirectory(), ".a2a-tasks");
            
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        /// <summary>
        /// 确保目录存在
        /// </summary>
        private async System.Threading.Tasks.Task EnsureDirectoryExistsAsync()
        {
            try
            {
                Directory.CreateDirectory(_baseDir);
                await System.Threading.Tasks.Task.CompletedTask;
            }
            catch (Exception ex)
            {
                throw A2AError.CreateInternalError(
                    $"创建目录 {_baseDir} 失败: {ex.Message}",
                    ex);
            }
        }

        /// <summary>
        /// 获取任务文件路径
        /// </summary>
        private string GetTaskFilePath(string taskId)
        {
            // 净化taskId以防止目录遍历
            string safeTaskId = Path.GetFileName(taskId);
            return Path.Combine(_baseDir, $"{safeTaskId}.json");
        }

        /// <summary>
        /// 获取历史文件路径
        /// </summary>
        private string GetHistoryFilePath(string taskId)
        {
            // 净化taskId
            string safeTaskId = Path.GetFileName(taskId);
            if (safeTaskId != taskId || taskId.Contains(".."))
            {
                throw A2AError.CreateInvalidParams($"无效的任务ID格式: {taskId}");
            }
            return Path.Combine(_baseDir, $"{safeTaskId}.history.json");
        }

        /// <summary>
        /// 读取JSON文件
        /// </summary>
        private async System.Threading.Tasks.Task<T?> ReadJsonFileAsync<T>(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return default;
                }

                using var stream = File.OpenRead(filePath);
                return await JsonSerializer.DeserializeAsync<T>(stream, _jsonOptions);
            }
            catch (Exception ex) when (ex is not A2AError)
            {
                if (ex is FileNotFoundException)
                {
                    return default; // 文件未找到对于加载操作不是错误
                }
                
                throw A2AError.CreateInternalError(
                    $"读取文件 {filePath} 失败: {ex.Message}",
                    ex);
            }
        }

        /// <summary>
        /// 写入JSON文件
        /// </summary>
        private async System.Threading.Tasks.Task WriteJsonFileAsync<T>(string filePath, T data)
        {
            try
            {
                await EnsureDirectoryExistsAsync();
                using var stream = File.Create(filePath);
                await JsonSerializer.SerializeAsync(stream, data, _jsonOptions);
            }
            catch (Exception ex) when (ex is not A2AError)
            {
                throw A2AError.CreateInternalError(
                    $"写入文件 {filePath} 失败: {ex.Message}",
                    ex);
            }
        }

        /// <summary>
        /// 加载任务及其历史记录
        /// </summary>
        public async System.Threading.Tasks.Task<TaskAndHistory?> LoadAsync(string taskId)
        {
            string taskFilePath = GetTaskFilePath(taskId);
            string historyFilePath = GetHistoryFilePath(taskId);

            // 首先读取任务文件 - 如果它不存在，任务就不存在
            var task = await ReadJsonFileAsync<Schema.Task>(taskFilePath);
            if (task == null)
            {
                return null; // 任务未找到
            }

            // 任务存在，现在尝试读取历史记录。它可能还不存在。
            List<Message> history = new();
            try
            {
                var historyContent = await ReadJsonFileAsync<TaskHistory>(historyFilePath);
                if (historyContent != null)
                {
                    history = historyContent.MessageHistory;
                }
            }
            catch (Exception ex)
            {
                // 记录读取历史文件的错误，但继续使用空历史记录
                Console.Error.WriteLine($"[FileStore] 读取任务 {taskId} 的历史文件时出错: {ex.Message}");
                // 继续使用空历史记录
            }

            return new TaskAndHistory { Task = task, History = history };
        }

        /// <summary>
        /// 保存任务及其关联的消息历史记录
        /// </summary>
        public async System.Threading.Tasks.Task SaveAsync(TaskAndHistory data)
        {
            var task = data.Task;
            var history = data.History;
            
            string taskFilePath = GetTaskFilePath(task.Id);
            string historyFilePath = GetHistoryFilePath(task.Id);

            // 确保目录存在（writeJsonFile会这样做，但最好这样做）
            await EnsureDirectoryExistsAsync();

            // 写入两个文件 - 可能并行
            var taskHistory = new TaskHistory { MessageHistory = history };
            
            await System.Threading.Tasks.Task.WhenAll(
                WriteJsonFileAsync(taskFilePath, task),
                WriteJsonFileAsync(historyFilePath, taskHistory)
            );
        }
    }
} 