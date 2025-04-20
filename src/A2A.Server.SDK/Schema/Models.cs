using Newtonsoft.Json;

namespace A2A.Server.SDK.Schema
{
    /// <summary>
    /// 基础JSON-RPC消息标识接口
    /// </summary>
    public interface IJSONRPCMessageIdentifier
    {
        /// <summary>
        /// 请求标识符。可以是字符串、数字或null。
        /// 响应必须具有与其相关请求相同的ID。
        /// 通知（不需要响应的请求）应省略ID或使用null。
        /// </summary>
        [JsonProperty("id")]
        public object? Id { get; set; }
    }

    /// <summary>
    /// 所有JSON-RPC消息的基础接口（请求和响应）
    /// </summary>
    public interface IJSONRPCMessage : IJSONRPCMessageIdentifier
    {
        /// <summary>
        /// 指定JSON-RPC版本。必须是"2.0"。
        /// </summary>
        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; set; }
    }

    /// <summary>
    /// 表示JSON-RPC请求对象的基本结构。
    /// 特定请求类型应继承此类。
    /// </summary>
    public class JSONRPCRequest : IJSONRPCMessage
    {
        /// <summary>
        /// 指定JSON-RPC版本。必须是"2.0"。
        /// </summary>
        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        /// <summary>
        /// 请求标识符。
        /// </summary>
        [JsonProperty("id")]
        public object? Id { get; set; }

        /// <summary>
        /// 要调用的方法名称。
        /// </summary>
        [JsonProperty("method")]
        public string Method { get; set; } = string.Empty;

        /// <summary>
        /// 方法的参数。可以是结构化对象、数组或null/省略。
        /// 特定请求接口将定义确切的类型。
        /// </summary>
        [JsonProperty("params")]
        public object? Params { get; set; }
    }

    /// <summary>
    /// 表示JSON-RPC错误对象。
    /// </summary>
    public class JSONRPCError
    {
        /// <summary>
        /// 指示发生的错误类型的编号。
        /// </summary>
        [JsonProperty("code")]
        public int Code { get; set; }

        /// <summary>
        /// 提供错误的简短描述的字符串。
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 有关错误的可选附加数据。
        /// </summary>
        [JsonProperty("data")]
        public object? Data { get; set; }
    }

    /// <summary>
    /// 表示JSON-RPC响应对象。
    /// </summary>
    public class JSONRPCResponse<TResult> : IJSONRPCMessage
    {
        /// <summary>
        /// 指定JSON-RPC版本。必须是"2.0"。
        /// </summary>
        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        /// <summary>
        /// 请求标识符。
        /// </summary>
        [JsonProperty("id")]
        public object? Id { get; set; }

        /// <summary>
        /// 方法调用的结果。在成功时是必需的。
        /// 如果发生错误，则应为null或省略。
        /// </summary>
        [JsonProperty("result")]
        public TResult? Result { get; set; }

        /// <summary>
        /// 如果在请求期间发生错误，则为错误对象。在失败时是必需的。
        /// 如果请求成功，则应为null或省略。
        /// </summary>
        [JsonProperty("error")]
        public JSONRPCError? Error { get; set; }
    }

    /// <summary>
    /// 表示代理的元数据卡片，描述其属性和功能。
    /// </summary>
    public class AgentCard
    {
        /// <summary>
        /// 代理的名称。
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 代理的可选描述。
        /// </summary>
        [JsonProperty("description")]
        public string? Description { get; set; }

        /// <summary>
        /// 与代理交互的基本URL端点。
        /// </summary>
        [JsonProperty("url")]
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// 代理提供者的信息。
        /// </summary>
        [JsonProperty("provider")]
        public AgentProvider? Provider { get; set; }

        /// <summary>
        /// 代理或其API的版本标识符。
        /// </summary>
        [JsonProperty("version")]
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// 指向代理文档的可选URL。
        /// </summary>
        [JsonProperty("documentationUrl")]
        public string? DocumentationUrl { get; set; }

        /// <summary>
        /// 代理支持的功能。
        /// </summary>
        [JsonProperty("capabilities")]
        public AgentCapabilities Capabilities { get; set; } = new();

        /// <summary>
        /// 与代理交互所需的身份验证详细信息。
        /// </summary>
        [JsonProperty("authentication")]
        public AgentAuthentication? Authentication { get; set; }

        /// <summary>
        /// 代理支持的默认输入模式（例如，'text'、'file'、'json'）。
        /// </summary>
        [JsonProperty("defaultInputModes")]
        public List<string>? DefaultInputModes { get; set; }

        /// <summary>
        /// 代理支持的默认输出模式（例如，'text'、'file'、'json'）。
        /// </summary>
        [JsonProperty("defaultOutputModes")]
        public List<string>? DefaultOutputModes { get; set; }

        /// <summary>
        /// 代理提供的特定技能列表。
        /// </summary>
        [JsonProperty("skills")]
        public List<AgentSkill> Skills { get; set; } = new();
    }
    
    /// <summary>
    /// 定义Agent的认证方案和凭据
    /// </summary>
    public class AgentAuthentication
    {
        /// <summary>
        /// 支持的认证方案列表
        /// </summary>
        [JsonProperty("schemes")]
        public List<string> Schemes { get; set; } = new List<string>();

        /// <summary>
        /// 认证凭据。可以是字符串（例如令牌）或null（如果最初不需要）
        /// </summary>
        [JsonProperty("credentials")]
        public string? Credentials { get; set; }
    }

    /// <summary>
    /// 描述Agent的能力
    /// </summary>
    public class AgentCapabilities
    {
        /// <summary>
        /// 指示Agent是否支持流式响应
        /// </summary>
        [JsonProperty("streaming")]
        public bool Streaming { get; set; }

        /// <summary>
        /// 指示Agent是否支持推送通知机制
        /// </summary>
        [JsonProperty("pushNotifications")]
        public bool PushNotifications { get; set; }

        /// <summary>
        /// 指示Agent是否支持提供状态转换历史
        /// </summary>
        [JsonProperty("stateTransitionHistory")]
        public bool StateTransitionHistory { get; set; }
    }

    /// <summary>
    /// 表示Agent背后的提供商或组织
    /// </summary>
    public class AgentProvider
    {
        /// <summary>
        /// 提供Agent的组织名称
        /// </summary>
        [JsonProperty("organization")]
        public string Organization { get; set; } = string.Empty;

        /// <summary>
        /// 与Agent提供商关联的URL
        /// </summary>
        [JsonProperty("url")]
        public string? Url { get; set; }
    }

    /// <summary>
    /// 定义Agent提供的特定技能或能力
    /// </summary>
    public class AgentSkill
    {
        /// <summary>
        /// 技能的唯一标识符
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 技能的人类可读名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 技能的可选描述
        /// </summary>
        [JsonProperty("description")]
        public string? Description { get; set; }

        /// <summary>
        /// 与技能关联的可选标签列表，用于分类
        /// </summary>
        [JsonProperty("tags")]
        public List<string>? Tags { get; set; }

        /// <summary>
        /// 技能的可选示例输入或用例列表
        /// </summary>
        [JsonProperty("examples")]
        public List<string>? Examples { get; set; }

        /// <summary>
        /// 此技能支持的可选输入模式列表，覆盖Agent默认值
        /// </summary>
        [JsonProperty("inputModes")]
        public List<string>? InputModes { get; set; }

        /// <summary>
        /// 此技能支持的可选输出模式列表，覆盖Agent默认值
        /// </summary>
        [JsonProperty("outputModes")]
        public List<string>? OutputModes { get; set; }
    }
} 