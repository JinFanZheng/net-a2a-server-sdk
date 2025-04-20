using A2A.Server.SDK.Schema;

namespace A2A.Server.SDK.Storage
{
    /// <summary>
    /// 任务存储提供程序的简化接口。
    /// 存储和检索任务及其完整的消息历史记录。
    /// </summary>
    public interface ITaskStore
    {
        /// <summary>
        /// 保存任务及其关联的消息历史记录。
        /// 如果任务ID存在，则覆盖现有数据。
        /// </summary>
        /// <param name="data">包含任务及其历史记录的对象</param>
        /// <returns>在保存操作完成时解析的任务</returns>
        System.Threading.Tasks.Task SaveAsync(TaskAndHistory data);

        /// <summary>
        /// 按任务ID加载任务及其历史记录。
        /// </summary>
        /// <param name="taskId">要加载的任务的ID</param>
        /// <returns>包含Task及其历史记录的对象的任务，如果未找到则为null</returns>
        System.Threading.Tasks.Task<TaskAndHistory?> LoadAsync(string taskId);
    }
} 