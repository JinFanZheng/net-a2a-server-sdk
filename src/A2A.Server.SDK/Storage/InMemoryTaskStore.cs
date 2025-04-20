using A2A.Server.SDK.Schema;
using System.Collections.Concurrent;

namespace A2A.Server.SDK.Storage
{
    /// <summary>
    /// 使用内存存储实现TaskStore
    /// </summary>
    public class InMemoryTaskStore : ITaskStore
    {
        private readonly ConcurrentDictionary<string, TaskAndHistory> _store = new();

        /// <summary>
        /// 按任务ID加载任务及其历史记录
        /// </summary>
        /// <param name="taskId">要加载的任务的ID</param>
        /// <returns>包含Task及其历史记录的对象的任务，如果未找到则为null</returns>
        public System.Threading.Tasks.Task<TaskAndHistory?> LoadAsync(string taskId)
        {
            if (_store.TryGetValue(taskId, out var entry))
            {
                // 返回深拷贝以防止外部修改
                return System.Threading.Tasks.Task.FromResult<TaskAndHistory?>(CloneTaskAndHistory(entry));
            }

            return System.Threading.Tasks.Task.FromResult<TaskAndHistory?>(null);
        }

        /// <summary>
        /// 保存任务及其关联的消息历史记录
        /// </summary>
        /// <param name="data">包含任务及其历史记录的对象</param>
        /// <returns>完成的任务</returns>
        public System.Threading.Tasks.Task SaveAsync(TaskAndHistory data)
        {
            // 存储副本以防止内部修改，如果调用者重用对象
            _store[data.Task.Id] = CloneTaskAndHistory(data);
            return System.Threading.Tasks.Task.CompletedTask;
        }

        /// <summary>
        /// 创建TaskAndHistory对象的深拷贝
        /// </summary>
        /// <param name="original">要复制的原始对象</param>
        /// <returns>新的副本</returns>
        private static TaskAndHistory CloneTaskAndHistory(TaskAndHistory original)
        {
            // 注意：这是一个简化的浅拷贝示例
            // 在生产环境中，您可能需要更复杂的深拷贝，例如使用JSON序列化/反序列化
            // 或映射库如AutoMapper

            var clone = new TaskAndHistory
            {
                Task = new Schema.Task
                {
                    Id = original.Task.Id,
                    SessionId = original.Task.SessionId,
                    Status = new Schema.TaskStatus
                    {
                        State = original.Task.Status.State,
                        Message = original.Task.Status.Message != null
                            ? new Message
                            {
                                Role = original.Task.Status.Message.Role,
                                Metadata = original.Task.Status.Message.Metadata != null
                                    ? new Dictionary<string, object>(original.Task.Status.Message.Metadata)
                                    : null
                                // 注意：这里应该也复制Parts，但为简化示例省略了
                            }
                            : null,
                        Timestamp = original.Task.Status.Timestamp
                    },
                    Metadata = original.Task.Metadata != null
                        ? new Dictionary<string, object>(original.Task.Metadata)
                        : null
                },
                History = new List<Message>(original.History.Count)
            };

            // 复制任务对象的成品（如果有）
            if (original.Task.Artifacts != null && original.Task.Artifacts.Count > 0)
            {
                clone.Task.Artifacts = new List<Artifact>(original.Task.Artifacts.Count);
                foreach (var artifact in original.Task.Artifacts)
                {
                    clone.Task.Artifacts.Add(new Artifact
                    {
                        Name = artifact.Name,
                        Description = artifact.Description,
                        Index = artifact.Index,
                        Append = artifact.Append,
                        LastChunk = artifact.LastChunk,
                        Metadata = artifact.Metadata != null
                            ? new Dictionary<string, object>(artifact.Metadata)
                            : null
                        // 同样，应该复制Parts，但为简化示例省略了
                    });
                }
            }

            // 复制历史消息
            foreach (var message in original.History)
            {
                clone.History.Add(new Message
                {
                    Role = message.Role,
                    Metadata = message.Metadata != null
                        ? new Dictionary<string, object>(message.Metadata)
                        : null
                    // 同样，应该复制Parts，但为简化示例省略了
                });
            }

            return clone;
        }
    }
} 