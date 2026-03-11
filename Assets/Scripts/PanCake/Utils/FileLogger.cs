using UnityEngine;
using System.IO;
using System;

namespace PanCake.Metroidvania.Utils
{
    /// <summary>
    /// 文件日志记录器
    /// 将Unity的所有日志输出到文件
    /// </summary>
    public class FileLogger : MonoBehaviour
    {
        [Header("日志文件配置")]
        [Tooltip("日志文件名（不含扩展名）")]
        [SerializeField]
        private string logFileName = "game_log";

        [Tooltip("是否在文件名中添加时间戳")]
        [SerializeField]
        private bool addTimestampToFileName = true;

        [Tooltip("是否记录堆栈跟踪（Error和Exception会强制记录）")]
        [SerializeField]
        private bool includeStackTrace = false;

        [Header("过滤设置")]
        [Tooltip("是否启用日志过滤（只记录包含关键词的日志）")]
        [SerializeField]
        private bool enableFilter = false;

        [Tooltip("过滤关键词列表（为空则记录所有日志）")]
        [SerializeField]
        private string[] filterKeywords = new string[]
        {
            "[QuestCompleteRewardFlowAction]",
            "[QuestRewardWindow]",
            "timescale"
        };

        [Header("调试设置")]
        [Tooltip("是否在启动时打印日志文件路径")]
        [SerializeField]
        private bool printLogPathOnStart = true;

        private StreamWriter logWriter;
        private string logFilePath;
        private bool isInitialized = false;

        void Awake()
        {
            // 防止重复实例
            if (FindObjectsOfType<FileLogger>().Length > 1)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            InitializeLogger();
        }

        private void InitializeLogger()
        {
            try
            {
                // 创建日志文件夹
#if UNITY_EDITOR
                // 在编辑器中，保存在项目根目录的 Logger 文件夹下
                string logFolder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logger");
#else
                // 在打包后的版本中，保存在执行文件同级的 Logger 文件夹下
                string logFolder = Path.Combine(Application.dataPath, "../Logger");
#endif
                Directory.CreateDirectory(logFolder);

                // 生成日志文件名
                string fileName = logFileName;
                if (addTimestampToFileName)
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                    fileName = $"{logFileName}_{timestamp}";
                }

                logFilePath = Path.Combine(logFolder, $"{fileName}.txt");

                // 创建日志写入器
                logWriter = new StreamWriter(logFilePath, true);
                logWriter.AutoFlush = true; // 自动刷新，确保日志立即写入

                // 写入日志头部
                logWriter.WriteLine("==========================================");
                logWriter.WriteLine($"Game Log - {Application.productName}");
                logWriter.WriteLine($"Started at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                logWriter.WriteLine($"Unity Version: {Application.unityVersion}");
                logWriter.WriteLine($"Platform: {Application.platform}");
                logWriter.WriteLine("==========================================");
                logWriter.WriteLine();

                // 订阅Unity日志事件
                Application.logMessageReceived += HandleLog;

                isInitialized = true;

                if (printLogPathOnStart)
                {
                    Debug.Log($"✅ FileLogger 已启动，日志文件路径: {logFilePath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ FileLogger 初始化失败: {ex.Message}");
            }
        }

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (!isInitialized || logWriter == null)
                return;

            try
            {
                // 过滤检查
                if (enableFilter && filterKeywords.Length > 0)
                {
                    bool matchesFilter = false;
                    foreach (var keyword in filterKeywords)
                    {
                        if (!string.IsNullOrEmpty(keyword) && logString.Contains(keyword))
                        {
                            matchesFilter = true;
                            break;
                        }
                    }

                    if (!matchesFilter)
                        return; // 不匹配过滤条件，跳过
                }

                // 格式化日志条目
                string logEntry = $"[{DateTime.Now:HH:mm:ss.fff}] [{type}] {logString}";

                // 记录堆栈跟踪（Error和Exception强制记录）
                if ((type == LogType.Error || type == LogType.Exception) || 
                    (includeStackTrace && !string.IsNullOrEmpty(stackTrace)))
                {
                    logEntry += $"\n{stackTrace}";
                }

                logWriter.WriteLine(logEntry);
            }
            catch (Exception ex)
            {
                // 避免日志记录本身导致的错误
                Debug.LogError($"FileLogger 写入失败: {ex.Message}");
            }
        }

        private void CloseLogger()
        {
            if (isInitialized)
            {
                try
                {
                    Application.logMessageReceived -= HandleLog;

                    if (logWriter != null)
                    {
                        logWriter.WriteLine();
                        logWriter.WriteLine("==========================================");
                        logWriter.WriteLine($"Log Ended at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                        logWriter.WriteLine("==========================================");
                        logWriter.Close();
                        logWriter = null;
                    }

                    isInitialized = false;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"FileLogger 关闭失败: {ex.Message}");
                }
            }
        }

        void OnDestroy()
        {
            CloseLogger();
        }

        void OnApplicationQuit()
        {
            CloseLogger();
        }

        #region 公共方法

        /// <summary>
        /// 获取当前日志文件路径
        /// </summary>
        public string GetLogFilePath()
        {
            return logFilePath;
        }

        /// <summary>
        /// 打开日志文件所在文件夹
        /// </summary>
        [ContextMenu("打开日志文件夹")]
        public void OpenLogFolder()
        {
            if (!string.IsNullOrEmpty(logFilePath))
            {
                string folder = Path.GetDirectoryName(logFilePath);
                Application.OpenURL($"file://{folder}");
            }
        }

        /// <summary>
        /// 手动写入一条日志
        /// </summary>
        public void WriteCustomLog(string message)
        {
            if (isInitialized && logWriter != null)
            {
                logWriter.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Custom] {message}");
            }
        }

        #endregion

        #region 调试方法

        [ContextMenu("打印日志文件路径")]
        private void DebugPrintLogPath()
        {
            if (isInitialized)
            {
                Debug.Log($"日志文件路径: {logFilePath}");
            }
            else
            {
                Debug.LogWarning("FileLogger 尚未初始化");
            }
        }

        [ContextMenu("写入测试日志")]
        private void DebugWriteTestLog()
        {
            Debug.Log("这是一条测试日志");
            Debug.LogWarning("这是一条测试警告");
        }

        #endregion
    }
}
