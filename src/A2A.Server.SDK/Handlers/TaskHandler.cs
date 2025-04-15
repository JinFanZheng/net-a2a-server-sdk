using A2A.Server.SDK.Schema;

namespace A2A.Server.SDK.Handlers
{
    /// <summary>
    /// 表示任务处理器的抽象基类
    /// </summary>
    public abstract class TaskHandler
    {
        /// <summary>
        /// 当任务状态更新时的回调
        /// </summary>
        public Func<Schema.TaskStatus, System.Threading.Tasks.Task>? OnStatusUpdate { get; set; }

        /// <summary>
        /// 当消息添加到任务历史记录时的回调
        /// </summary>
        public Func<Message, System.Threading.Tasks.Task>? OnMessageAdded { get; set; }

        /// <summary>
        /// 当任务添加成品时的回调
        /// </summary>
        public Func<Artifact, System.Threading.Tasks.Task>? OnArtifactAdded { get; set; }

        /// <summary>
        /// 当任务更新成品时的回调
        /// </summary>
        public Func<ArtifactUpdate, System.Threading.Tasks.Task>? OnArtifactUpdated { get; set; }

        /// <summary>
        /// 获取与此处理器关联的任务
        /// </summary>
        public Schema.Task Task { get; protected set; } = new Schema.Task();

        /// <summary>
        /// 处理任务的主要方法
        /// </summary>
        /// <param name="inputParameters">任务的输入参数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>完成的任务</returns>
        public abstract System.Threading.Tasks.Task HandleTaskAsync(
            Dictionary<string, object>? inputParameters, 
            System.Threading.CancellationToken cancellationToken);

        /// <summary>
        /// 通知处理器有新事件可用
        /// </summary>
        public virtual void NotifyEventAvailable()
        {
            // 默认实现为空，子类可以覆盖以处理事件
        }
    }
} 