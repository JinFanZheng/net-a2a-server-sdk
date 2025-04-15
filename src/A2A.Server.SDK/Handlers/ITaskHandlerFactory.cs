using A2A.Server.SDK.Schema;

namespace A2A.Server.SDK.Handlers
{
    /// <summary>
    /// 任务处理器工厂接口
    /// </summary>
    public interface ITaskHandlerFactory
    {
        /// <summary>
        /// 为指定任务创建处理器
        /// </summary>
        /// <param name="task">要处理的任务</param>
        /// <returns>任务处理器实例</returns>
        TaskHandler CreateHandler(Schema.Task task);
    }
} 