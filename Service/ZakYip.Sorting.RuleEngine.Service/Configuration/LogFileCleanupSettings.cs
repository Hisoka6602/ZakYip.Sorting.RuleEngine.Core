namespace ZakYip.Sorting.RuleEngine.Service.Configuration;

/// <summary>
/// 日志文件清理配置
/// Log file cleanup settings
/// </summary>
public class LogFileCleanupSettings
{
    /// <summary>
    /// 是否启用日志文件清理，默认true
    /// Enable log file cleanup, default true
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// 日志保留天数，默认7天
    /// Log retention days, default 7
    /// </summary>
    public int RetentionDays { get; set; } = 7;
    
    /// <summary>
    /// 日志文件目录，默认"./logs"
    /// Log file directory, default "./logs"
    /// </summary>
    public string LogDirectory { get; set; } = "./logs";
}
