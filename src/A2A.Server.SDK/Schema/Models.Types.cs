using Newtonsoft.Json;

namespace A2A.Server.SDK.Schema
{
    /// <summary>
    /// 表示创建任务的请求
    /// </summary>
    public class CreateTaskRequest
    {
        /// <summary>
        /// 任务的类型
        /// </summary>
        [JsonProperty("taskType")]
        public string TaskType { get; set; } = string.Empty;

        /// <summary>
        /// 任务的唯一标识符（可选）
        /// </summary>
        [JsonProperty("taskId")]
        public string? TaskId { get; set; }

        /// <summary>
        /// 此任务所属会话的标识符（可选）
        /// </summary>
        [JsonProperty("sessionId")]
        public string? SessionId { get; set; }

        /// <summary>
        /// 任务的输入参数
        /// </summary>
        [JsonProperty("inputParameters")]
        public Dictionary<string, object>? InputParameters { get; set; }

        /// <summary>
        /// 与任务关联的元数据
        /// </summary>
        [JsonProperty("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// 成品更新信息
    /// </summary>
    public class ArtifactUpdate
    {
        /// <summary>
        /// 成品的名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 成品的索引
        /// </summary>
        [JsonProperty("index")]
        public int? Index { get; set; }

        /// <summary>
        /// 要附加的部分
        /// </summary>
        [JsonProperty("appendParts")]
        public List<Part>? AppendParts { get; set; }

        /// <summary>
        /// 是否是最后一块
        /// </summary>
        [JsonProperty("lastChunk")]
        public bool? LastChunk { get; set; }

        /// <summary>
        /// 与成品关联的元数据
        /// </summary>
        [JsonProperty("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// 任务事件
    /// </summary>
    public class TaskEvent
    {
        /// <summary>
        /// 关联的任务ID
        /// </summary>
        [JsonProperty("taskId")]
        public string TaskId { get; set; } = string.Empty;

        /// <summary>
        /// 事件数据
        /// </summary>
        [JsonProperty("eventData")]
        public object EventData { get; set; } = null!;

        /// <summary>
        /// 事件时间戳
        /// </summary>
        [JsonProperty("timestamp")]
        public DateTimeOffset Timestamp { get; set; }
    }
} 