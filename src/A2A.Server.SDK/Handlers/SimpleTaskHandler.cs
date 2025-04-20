using A2A.Server.SDK.Schema;

namespace A2A.Server.SDK.Handlers
{
    /// <summary>
    /// 简单任务处理器的示例实现
    /// </summary>
    public class SimpleTaskHandler : ITaskHandler
    {
        /// <summary>
        /// 处理任务的异步方法
        /// </summary>
        /// <param name="context">任务上下文</param>
        /// <returns>任务更新的异步枚举</returns>
        public async IAsyncEnumerable<TaskYieldUpdate> HandleTaskAsync(TaskContext context)
        {
            // 记录消息
            Console.WriteLine($"处理任务 {context.Task.Id}");
            
            // 产生工作中状态更新
            yield return new TaskYieldUpdate
            {
                StatusUpdate = new Schema.TaskStatus
                {
                    State = TaskState.Working,
                    Message = new Message
                    {
                        Role = "agent",
                        Parts = new List<Part>
                        {
                            new TextPart { Text = "正在处理..." }
                        }
                    }
                }
            };

            // 模拟工作
            await System.Threading.Tasks.Task.Delay(1500);

            // 检查是否取消
            if (context.IsCancelled())
            {
                Console.WriteLine("任务已取消！");
                yield break;
            }

            // 产生一个成品
            yield return new TaskYieldUpdate
            {
                ArtifactUpdate = new Artifact
                {
                    Name = "output.txt",
                    Parts = new List<Part>
                    {
                        new TextPart { Text = $"任务 {context.Task.Id} 的结果" }
                    }
                }
            };

            // 模拟更多工作
            await System.Threading.Tasks.Task.Delay(500);

            // 产生完成状态更新
            yield return new TaskYieldUpdate
            {
                StatusUpdate = new Schema.TaskStatus
                {
                    State = TaskState.Completed,
                    Message = new Message
                    {
                        Role = "agent",
                        Parts = new List<Part>
                        {
                            new TextPart { Text = "完成！" }
                        }
                    }
                }
            };
        }
    }
} 