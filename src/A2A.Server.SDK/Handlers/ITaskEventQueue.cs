using A2A.Server.SDK.Schema;

namespace A2A.Server.SDK.Handlers
{
    /// <summary>
    /// 任务事件队列的接口
    /// </summary>
    public interface ITaskEventQueue
    {
        /// <summary>
        /// 将事件排入队列
        /// </summary>
        /// <param name="taskEvent">要排队的事件</param>
        /// <returns>完成的任务</returns>
        System.Threading.Tasks.Task EnqueueEventAsync(TaskEvent taskEvent);

        /// <summary>
        /// 从队列中获取特定任务的下一个事件
        /// </summary>
        /// <param name="taskId">目标任务的ID</param>
        /// <param name="timeout">等待的超时</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>接收到的事件或null</returns>
        System.Threading.Tasks.Task<TaskEvent?> GetNextEventAsync(
            string taskId, 
            TimeSpan timeout, 
            System.Threading.CancellationToken cancellationToken);
    }
} 