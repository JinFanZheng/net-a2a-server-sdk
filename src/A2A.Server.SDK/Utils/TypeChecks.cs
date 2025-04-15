using A2A.Server.SDK.Schema;

namespace A2A.Server.SDK.Utils
{
    /// <summary>
    /// 类型检查辅助方法
    /// </summary>
    public static class TypeChecks
    {
        /// <summary>
        /// 检查值是否为纯对象（不包括数组和null）
        /// </summary>
        /// <param name="value">要检查的值</param>
        /// <returns>如果值是纯对象，则为true，否则为false</returns>
        public static bool IsObject(object? value)
        {
            return value != null && value.GetType().IsClass && !(value is string) && !(value is Array);
        }

        /// <summary>
        /// 类型检查，检查对象是否为任务状态更新（没有'parts'）
        /// 用于区分处理程序生成的更新
        /// </summary>
        /// <param name="update">要检查的更新</param>
        /// <returns>如果是任务状态更新，则为true，否则为false</returns>
        public static bool IsTaskStatusUpdate(object update)
        {
            if (update is TaskYieldUpdate taskYieldUpdate)
            {
                return taskYieldUpdate.StatusUpdate != null;
            }
            
            // 检查是否有'state'属性但没有'parts'属性（Artifacts有parts）
            var type = update.GetType();
            var hasState = type.GetProperty("State") != null;
            var hasParts = type.GetProperty("Parts") != null;
            
            return IsObject(update) && hasState && !hasParts;
        }

        /// <summary>
        /// 类型检查，检查对象是否为成品更新（有'parts'）
        /// 用于区分处理程序生成的更新
        /// </summary>
        /// <param name="update">要检查的更新</param>
        /// <returns>如果是成品更新，则为true，否则为false</returns>
        public static bool IsArtifactUpdate(object update)
        {
            if (update is TaskYieldUpdate taskYieldUpdate)
            {
                return taskYieldUpdate.ArtifactUpdate != null;
            }
            
            // 检查是否有'parts'属性
            var type = update.GetType();
            var hasParts = type.GetProperty("Parts") != null;
            
            return IsObject(update) && hasParts;
        }
    }
} 