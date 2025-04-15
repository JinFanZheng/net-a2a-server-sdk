using A2A.Server.SDK.Schema;

namespace A2A.Server.SDK.Errors
{
    /// <summary>
    /// A2A服务操作的自定义错误类，包含JSON-RPC错误代码。
    /// </summary>
    public class A2AError : Exception
    {
        // 定义标准的错误代码常量
        public const int ParseError = -32700;
        public const int InvalidRequest = -32600;
        public const int MethodNotFound = -32601;
        public const int InvalidParams = -32602;
        public const int InternalError = -32603;
        public const int TaskNotFound = -32000;
        public const int TaskNotCancelable = -32001;
        public const int PushNotificationNotSupported = -32002;
        public const int UnsupportedOperation = -32003;

        /// <summary>
        /// 错误代码
        /// </summary>
        public int Code { get; }

        /// <summary>
        /// 可选的额外错误数据
        /// </summary>
        public new object? Data { get; }

        /// <summary>
        /// 可选的关联任务ID上下文
        /// </summary>
        public string? TaskId { get; }

        /// <summary>
        /// 创建A2A错误的新实例
        /// </summary>
        /// <param name="code">错误代码</param>
        /// <param name="message">错误消息</param>
        /// <param name="data">关联数据</param>
        /// <param name="taskId">相关任务ID</param>
        public A2AError(int code, string message, object? data = null, string? taskId = null) 
            : base(message)
        {
            Code = code;
            Data = data;
            TaskId = taskId;
        }

        /// <summary>
        /// 将错误格式化为标准JSON-RPC错误对象结构
        /// </summary>
        /// <returns>JSON-RPC错误对象</returns>
        public JSONRPCError ToJSONRPCError()
        {
            var errorObject = new JSONRPCError
            {
                Code = Code,
                Message = Message
            };

            if (Data != null)
            {
                errorObject.Data = Data;
            }

            return errorObject;
        }

        // 静态工厂方法用于常见错误

        /// <summary>
        /// 创建解析错误
        /// </summary>
        public static A2AError CreateParseError(string message, object? data = null)
        {
            return new A2AError(ParseError, message, data);
        }

        /// <summary>
        /// 创建无效请求错误
        /// </summary>
        public static A2AError CreateInvalidRequest(string message, object? data = null)
        {
            return new A2AError(InvalidRequest, message, data);
        }

        /// <summary>
        /// 创建方法未找到错误
        /// </summary>
        public static A2AError CreateMethodNotFound(string method)
        {
            return new A2AError(MethodNotFound, $"方法未找到: {method}");
        }

        /// <summary>
        /// 创建无效参数错误
        /// </summary>
        public static A2AError CreateInvalidParams(string message, object? data = null)
        {
            return new A2AError(InvalidParams, message, data);
        }

        /// <summary>
        /// 创建内部错误
        /// </summary>
        public static A2AError CreateInternalError(string message, object? data = null)
        {
            return new A2AError(InternalError, message, data);
        }

        /// <summary>
        /// 创建任务未找到错误
        /// </summary>
        public static A2AError CreateTaskNotFound(string taskId)
        {
            return new A2AError(TaskNotFound, $"任务未找到: {taskId}", null, taskId);
        }

        /// <summary>
        /// 创建任务不可取消错误
        /// </summary>
        public static A2AError CreateTaskNotCancelable(string taskId)
        {
            return new A2AError(TaskNotCancelable, $"任务不可取消: {taskId}", null, taskId);
        }

        /// <summary>
        /// 创建推送通知不支持错误
        /// </summary>
        public static A2AError CreatePushNotificationNotSupported()
        {
            return new A2AError(PushNotificationNotSupported, "不支持推送通知");
        }

        /// <summary>
        /// 创建不支持的操作错误
        /// </summary>
        public static A2AError CreateUnsupportedOperation(string operation)
        {
            return new A2AError(UnsupportedOperation, $"不支持的操作: {operation}");
        }
    }
} 