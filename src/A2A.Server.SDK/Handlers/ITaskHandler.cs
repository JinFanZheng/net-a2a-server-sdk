using A2A.Server.SDK.Schema;

namespace A2A.Server.SDK.Handlers
{
    /// <summary>
    /// 定义任务处理器的接口
    /// 
    /// 处理器实现为异步迭代器。他们接收有关任务和触发消息的上下文。
    /// 他们可以执行工作并"yield"状态或成品更新（TaskYieldUpdate）。
    /// 服务器消费这些yields，在存储中更新任务状态，并在适用时流式传输事件。
    /// </summary>
    public interface ITaskHandler
    {
        /// <summary>
        /// 处理任务的方法
        /// </summary>
        /// <param name="context">包含任务详情、取消状态和存储访问的TaskContext对象</param>
        /// <returns>可以迭代的异步枚举器，产生任务状态或成品更新</returns>
        IAsyncEnumerable<TaskYieldUpdate> HandleTaskAsync(TaskContext context);
    }
} 