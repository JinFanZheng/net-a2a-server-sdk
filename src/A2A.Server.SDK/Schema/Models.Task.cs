using Newtonsoft.Json;

namespace A2A.Server.SDK.Schema
{
    /// <summary>
    /// 表示任务
    /// </summary>
    public class Task
    {
        /// <summary>
        /// 任务的唯一标识符
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 此任务所属会话的可选标识符
        /// </summary>
        [JsonProperty("sessionId")]
        public string? SessionId { get; set; }

        /// <summary>
        /// 任务的当前状态
        /// </summary>
        [JsonProperty("status")]
        public TaskStatus Status { get; set; } = new TaskStatus();

        /// <summary>
        /// 与任务关联的可选成品列表（例如，输出，中间文件）
        /// </summary>
        [JsonProperty("artifacts")]
        public List<Artifact>? Artifacts { get; set; }

        /// <summary>
        /// 与任务关联的可选元数据
        /// </summary>
        [JsonProperty("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// 任务和其历史记录的组合
    /// </summary>
    public class TaskAndHistory
    {
        /// <summary>
        /// 任务对象
        /// </summary>
        public Task Task { get; set; } = new Task();

        /// <summary>
        /// 消息历史记录
        /// </summary>
        public List<Message> History { get; set; } = new List<Message>();
    }

    /// <summary>
    /// 任务历史
    /// </summary>
    public class TaskHistory
    {
        /// <summary>
        /// 按时间顺序排列的消息列表
        /// </summary>
        [JsonProperty("messageHistory")]
        public List<Message> MessageHistory { get; set; } = new List<Message>();
    }

    /// <summary>
    /// 任务状态更新事件
    /// </summary>
    public class TaskStatusUpdateEvent
    {
        /// <summary>
        /// 被更新任务的ID
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 任务的新状态
        /// </summary>
        [JsonProperty("status")]
        public TaskStatus Status { get; set; } = new TaskStatus();

        /// <summary>
        /// 指示这是否是任务的最终更新的标志
        /// </summary>
        [JsonProperty("final")]
        public bool Final { get; set; }

        /// <summary>
        /// 与此更新事件关联的可选元数据
        /// </summary>
        [JsonProperty("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// 任务成品更新事件
    /// </summary>
    public class TaskArtifactUpdateEvent
    {
        /// <summary>
        /// 被更新任务的ID
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 任务的新的或更新的成品
        /// </summary>
        [JsonProperty("artifact")]
        public Artifact Artifact { get; set; } = new Artifact();

        /// <summary>
        /// 指示这是否是任务的最终更新的标志
        /// </summary>
        [JsonProperty("final")]
        public bool Final { get; set; }

        /// <summary>
        /// 与此更新事件关联的可选元数据
        /// </summary>
        [JsonProperty("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// 任务上下文
    /// </summary>
    public class TaskContext
    {
        /// <summary>
        /// 调用或恢复处理程序时任务的当前状态。
        /// 注意：这是一个快照。对于异步操作期间的绝对最新状态，
        /// 处理程序可能需要通过存储重新加载任务。
        /// </summary>
        public Task Task { get; set; } = new Task();

        /// <summary>
        /// 触发此处理程序调用或恢复的特定用户消息。
        /// </summary>
        public Message UserMessage { get; set; } = new Message();

        /// <summary>
        /// 检查是否已请求取消此任务的函数。
        /// 处理程序应理想地在长时间运行的操作期间定期检查此项。
        /// </summary>
        public Func<bool> IsCancelled { get; set; } = () => false;

        /// <summary>
        /// 与任务关联的消息历史记录，直到调用处理程序时为止。
        /// 可选，因为历史记录可能并非总是可用或相关。
        /// </summary>
        public List<Message>? History { get; set; }
    }

    /// <summary>
    /// TaskHandler可以产生的可能类型的更新。
    /// 它要么是部分TaskStatus（没有服务器管理的时间戳），
    /// 要么是完整的Artifact对象。
    /// </summary>
    public class TaskYieldUpdate
    {
        /// <summary>
        /// 任务状态更新
        /// </summary>
        public TaskStatus? StatusUpdate { get; set; }

        /// <summary>
        /// 成品更新
        /// </summary>
        public Artifact? ArtifactUpdate { get; set; }

        /// <summary>
        /// 检查此更新是否为状态更新
        /// </summary>
        /// <returns>如果是状态更新则为true，否则为false</returns>
        public bool IsStatusUpdate() => StatusUpdate != null;

        /// <summary>
        /// 检查此更新是否为成品更新
        /// </summary>
        /// <returns>如果是成品更新则为true，否则为false</returns>
        public bool IsArtifactUpdate() => ArtifactUpdate != null;
    }

    /// <summary>
    /// 表示任务的状态
    /// </summary>
    public enum TaskState
    {
        /// <summary>未开始</summary>
        NotStarted,
        /// <summary>已提交</summary>
        Submitted,
        /// <summary>正在处理</summary>
        Working,
        /// <summary>需要输入</summary>
        InputRequired,
        /// <summary>已完成</summary>
        Completed,
        /// <summary>已取消</summary>
        Canceled,
        /// <summary>失败</summary>
        Failed,
        /// <summary>未知</summary>
        Unknown
    }
} 