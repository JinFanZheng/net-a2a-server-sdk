using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace A2A.Server.SDK.Schema
{
    /// <summary>
    /// Part类型的自定义JSON转换器
    /// </summary>
    public class PartConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Part);
        }

        /// <summary>
        /// 读取JSON并根据类型创建相应的Part对象
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="objectType"></param>
        /// <param name="existingValue"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        /// <exception cref="JsonSerializationException"></exception>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            string type = (string)jo["type"]!;

            Part part;
            switch (type)
            {
                case "text":
                    part = new TextPart();
                    break;
                case "file":
                    part = new FilePart();
                    break;
                case "data":
                    part = new DataPart();
                    break;
                default:
                    throw new JsonSerializationException($"未知的Part类型: {type}");
            }

            serializer.Populate(jo.CreateReader(), part);
            return part;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }

    /// <summary>
    /// 文件内容的基础接口
    /// </summary>
    public class FileContentBase
    {
        /// <summary>
        /// 文件的可选名称
        /// </summary>
        [JsonProperty("name")]
        public string? Name { get; set; }

        /// <summary>
        /// 文件内容的可选MIME类型
        /// </summary>
        [JsonProperty("mimeType")]
        public string? MimeType { get; set; }
    }

    /// <summary>
    /// 使用Base64编码的文件内容
    /// </summary>
    public class FileContentBytes : FileContentBase
    {
        /// <summary>
        /// 以Base64字符串编码的文件内容
        /// </summary>
        [JsonProperty("bytes")]
        public string Bytes { get; set; } = string.Empty;
    }

    /// <summary>
    /// 使用URI指向的文件内容
    /// </summary>
    public class FileContentUri : FileContentBase
    {
        /// <summary>
        /// 指向文件内容的URI
        /// </summary>
        [JsonProperty("uri")]
        public string Uri { get; set; } = string.Empty;
    }

    /// <summary>
    /// 消息部分的基类
    /// </summary>
    public abstract class Part
    {
        /// <summary>
        /// 此部分的可选元数据
        /// </summary>
        [JsonProperty("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// 表示文本内容的消息部分
    /// </summary>
    public class TextPart : Part
    {
        /// <summary>
        /// 类型标识符
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; } = "text";

        /// <summary>
        /// 文本内容
        /// </summary>
        [JsonProperty("text")]
        public string Text { get; set; } = string.Empty;
    }

    /// <summary>
    /// 表示文件内容的消息部分
    /// </summary>
    public class FilePart : Part
    {
        /// <summary>
        /// 类型标识符
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; } = "file";

        /// <summary>
        /// 文件内容，可以是内联的或通过URI提供
        /// </summary>
        [JsonProperty("file")]
        public FileContentBase File { get; set; } = null!;
    }

    /// <summary>
    /// 表示结构化数据内容的消息部分
    /// </summary>
    public class DataPart : Part
    {
        /// <summary>
        /// 类型标识符
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; } = "data";

        /// <summary>
        /// 作为JSON对象的结构化数据内容
        /// </summary>
        [JsonProperty("data")]
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// 表示成品/产物
    /// </summary>
    public class Artifact
    {
        /// <summary>
        /// 成品的可选名称
        /// </summary>
        [JsonProperty("name")]
        public string? Name { get; set; }

        /// <summary>
        /// 成品的可选描述
        /// </summary>
        [JsonProperty("description")]
        public string? Description { get; set; }

        /// <summary>
        /// 成品的组成部分
        /// </summary>
        [JsonProperty("parts")]
        public List<Part> Parts { get; set; } = new List<Part>();

        /// <summary>
        /// 用于排序成品的可选索引，特别是在流式传输或更新中相关
        /// </summary>
        [JsonProperty("index")]
        public int? Index { get; set; }

        /// <summary>
        /// 可选标志，指示此成品内容是否应附加到先前内容（用于流式传输）
        /// </summary>
        [JsonProperty("append")]
        public bool? Append { get; set; }

        /// <summary>
        /// 与成品关联的可选元数据
        /// </summary>
        [JsonProperty("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// 可选标志，指示这是否是此成品的最后一块数据（用于流式传输）
        /// </summary>
        [JsonProperty("lastChunk")]
        public bool? LastChunk { get; set; }
    }

    /// <summary>
    /// 表示消息
    /// </summary>
    public class Message
    {
        /// <summary>
        /// 发送者的角色（用户或代理）
        /// </summary>
        [JsonProperty("role")]
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// 消息内容，由一个或多个部分组成
        /// </summary>
        [JsonProperty("parts")]
        public List<Part> Parts { get; set; } = new List<Part>();

        /// <summary>
        /// 与消息关联的可选元数据
        /// </summary>
        [JsonProperty("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// 表示任务状态
    /// </summary>
    public class TaskStatus
    {
        /// <summary>
        /// 任务的当前状态
        /// </summary>
        [JsonProperty("state")]
        public TaskState State { get; set; }

        /// <summary>
        /// 与当前状态关联的可选消息（例如，进度更新，最终响应）
        /// </summary>
        [JsonProperty("message")]
        public Message? Message { get; set; }

        /// <summary>
        /// 记录此状态的时间戳（ISO 8601格式）
        /// </summary>
        [JsonProperty("timestamp")]
        public string? Timestamp { get; set; }
    }
}