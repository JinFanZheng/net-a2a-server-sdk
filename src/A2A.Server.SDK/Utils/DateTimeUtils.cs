namespace A2A.Server.SDK.Utils
{
    /// <summary>
    /// 日期时间相关的工具方法
    /// </summary>
    public static class DateTimeUtils
    {
        /// <summary>
        /// 生成ISO 8601格式的时间戳
        /// </summary>
        /// <returns>当前时间戳字符串</returns>
        public static string GetCurrentTimestamp()
        {
            return DateTime.UtcNow.ToString("o");
        }
    }
} 