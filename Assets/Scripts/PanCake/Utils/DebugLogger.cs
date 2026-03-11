using UnityEngine;

namespace PanCake.Metroidvania.Utils
{
    /// <summary>
    /// 统一的调试日志工具类 - 避免在代码中重复编写LogDebug方法
    /// </summary>
    public static class DebugLogger
    {
        /// <summary>
        /// 是否在日志中显示时间戳
        /// </summary>
        public static bool ShowTimestamp = true;

        /// <summary>
        /// 获取 HH:MM:SS.sss 格式的时间戳
        /// </summary>
        private static string GetTimestamp()
        {
            if (!ShowTimestamp)
                return string.Empty;

            // Time.time 返回游戏开始后的秒数（float）
            float totalSeconds = Time.time;

            // 计算时、分、秒、毫秒
            int hours = (int)(totalSeconds / 3600);
            int minutes = (int)((totalSeconds % 3600) / 60);
            int seconds = (int)(totalSeconds % 60);
            int milliseconds = (int)((totalSeconds - (int)totalSeconds) * 1000);

            return $"[{hours:D2}:{minutes:D2}:{seconds:D2}.{milliseconds:D3}] ";
        }

        /// <summary>
        /// 记录调试信息（通过Debug.Log）
        /// </summary>
        /// <param name="context">调用者的上下文对象（通常传入this）</param>
        /// <param name="message">调试信息内容</param>
        /// <param name="showDebugInfo">是否显示调试信息的标志</param>
        /// <param name="forceShow">是否强制显示（用于重要信息）</param>
        public static void Log(object context, string message, bool showDebugInfo, bool forceShow = false)
        {
            if (showDebugInfo || forceShow)
            {
                string contextName = GetContextName(context);
                string timestamp = GetTimestamp();

                if (forceShow && !showDebugInfo)
                {
                    // 重要信息使用 LogWarning 确保显示
                    Debug.LogWarning($"{timestamp}[{contextName}] {message}");
                }
                else
                {
                    Debug.Log($"{timestamp}[{contextName}] {message}");
                }
            }
        }

        /// <summary>
        /// 记录警告信息（通过Debug.LogWarning）
        /// </summary>
        /// <param name="context">调用者的上下文对象（通常传入this）</param>
        /// <param name="message">警告信息内容</param>
        /// <param name="showDebugInfo">是否显示调试信息的标志</param>
        /// <param name="forceShow">是否强制显示</param>
        public static void LogWarning(object context, string message, bool showDebugInfo, bool forceShow = true)
        {
            if (showDebugInfo || forceShow)
            {
                string contextName = GetContextName(context);
                string timestamp = GetTimestamp();
                Debug.LogWarning($"{timestamp}[{contextName}] {message}");
            }
        }

        /// <summary>
        /// 记录错误信息（通过Debug.LogError）
        /// </summary>
        /// <param name="context">调用者的上下文对象（通常传入this）</param>
        /// <param name="message">错误信息内容</param>
        public static void LogError(object context, string message)
        {
            string contextName = GetContextName(context);
            string timestamp = GetTimestamp();
            Debug.LogError($"{timestamp}[{contextName}] {message}");
        }

        /// <summary>
        /// 获取调用者类型的调试名称
        /// </summary>
        private static string GetContextName(object context)
        {
            if (context == null)
                return "Unknown";

            // 如果是MonoBehaviour，使用类名
            if (context is MonoBehaviour mb)
                return mb.GetType().Name;

            // 其他类型，直接使用类型名
            return context.GetType().Name;
        }

        /// <summary>
        /// 根据分支条件决定打印不同的调试信息
        /// </summary>
        /// <param name="context">调用者上下文</param>
        /// <param name="condition">判断条件</param>
        /// <param name="trueMessage">条件为真时的信息</param>
        /// <param name="falseMessage">条件为假时的信息</param>
        /// <param name="showDebugInfo">是否显示调试信息</param>
        public static void LogCondition(object context, bool condition, string trueMessage, string falseMessage, bool showDebugInfo)
        {
            string message = condition ? trueMessage : falseMessage;
            Log(context, message, showDebugInfo);
        }

        /// <summary>
        /// 根据分支打印记录方法进入
        /// </summary>
        /// <param name="context">调用者上下文</param>
        /// <param name="methodName">方法名称</param>
        /// <param name="showDebugInfo">是否显示调试信息</param>
        public static void LogMethodEntry(object context, string methodName, bool showDebugInfo)
        {
            Log(context, $"进入方法: {methodName}", showDebugInfo);
        }

        /// <summary>
        /// 根据分支打印记录方法退出
        /// </summary>
        /// <param name="context">调用者上下文</param>
        /// <param name="methodName">方法名称</param>
        /// <param name="showDebugInfo">是否显示调试信息</param>
        public static void LogMethodExit(object context, string methodName, bool showDebugInfo)
        {
            Log(context, $"退出方法: {methodName}", showDebugInfo);
        }

        /// <summary>
        /// 根据分支打印记录状态变化
        /// </summary>
        /// <param name="context">调用者上下文</param>
        /// <param name="propertyName">属性/状态名称</param>
        /// <param name="oldValue">旧值</param>
        /// <param name="newValue">新值</param>
        /// <param name="showDebugInfo">是否显示调试信息</param>
        public static void LogStateChange<T>(object context, string propertyName, T oldValue, T newValue, bool showDebugInfo)
        {
            Log(context, $"{propertyName}: {oldValue} -> {newValue}", showDebugInfo);
        }

        /// <summary>
        /// 专门用于攻击时序调试的日志方法（带彩色高亮）
        /// </summary>
        /// <param name="context">调用者上下文</param>
        /// <param name="stepName">攻击步骤名称</param>
        /// <param name="message">调试信息</param>
        /// <param name="showDebugInfo">是否显示调试信息</param>
        public static void LogAttackTiming(object context, string stepName, string message, bool showDebugInfo)
        {
            if (!showDebugInfo) return;

            string contextName = GetContextName(context);
            string timestamp = GetTimestamp();

            // 攻击时序日志使用彩色格式，更易于在 Console 中识别
            Debug.Log($"<color=cyan>{timestamp}</color><color=yellow>[{contextName}]</color> <color=green>[{stepName}]</color> {message}");
        }

        #region 调试辅助方法

        /// <summary>
        /// 运行时切换时间戳显示
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Initialize()
        {
            // 默认启用时间戳
            ShowTimestamp = true;
        }

        /// <summary>
        /// 便捷方法 - 切换时间戳显示
        /// </summary>
        public static void ToggleTimestamp()
        {
            ShowTimestamp = !ShowTimestamp;
            Debug.Log($"DebugLogger 时间戳显示已{(ShowTimestamp ? "开启" : "关闭")}");
        }

        #endregion
    }
}