namespace ZakYip.Sorting.RuleEngine.Infrastructure.Configuration;

/// <summary>
/// 日志文件清理配置
/// </summary>
public class LogFileCleanupSettings
{
    /// <summary>
    /// 是否启用日志文件清理，默认true
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// 日志保留天数，默认7天
    /// </summary>
    public int RetentionDays { get; set; } = 7;
    
    /// <summary>
    /// 日志文件目录，默认"./logs"
    /// </summary>
    public string LogDirectory { get; set; } = "./logs";
}
