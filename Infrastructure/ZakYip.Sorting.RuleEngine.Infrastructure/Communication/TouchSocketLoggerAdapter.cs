using Microsoft.Extensions.Logging;
using TouchSocket.Core;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Communication;

/// <summary>
/// TouchSocket日志适配器（共享类，避免重复代码）
/// TouchSocket logger adapter (shared class to avoid code duplication)
/// </summary>
public class TouchSocketLoggerAdapter : ILog
{
    private readonly ILogger _logger;

    public TouchSocketLoggerAdapter(ILogger logger)
    {
        _logger = logger;
    }

    TouchSocket.Core.LogLevel ILog.LogLevel { get; set; } = TouchSocket.Core.LogLevel.Info;

    public void Log(TouchSocket.Core.LogLevel logLevel, object source, string message, Exception? exception)
    {
        var level = logLevel switch
        {
            TouchSocket.Core.LogLevel.Trace => Microsoft.Extensions.Logging.LogLevel.Trace,
            TouchSocket.Core.LogLevel.Debug => Microsoft.Extensions.Logging.LogLevel.Debug,
            TouchSocket.Core.LogLevel.Info => Microsoft.Extensions.Logging.LogLevel.Information,
            TouchSocket.Core.LogLevel.Warning => Microsoft.Extensions.Logging.LogLevel.Warning,
            TouchSocket.Core.LogLevel.Error => Microsoft.Extensions.Logging.LogLevel.Error,
            TouchSocket.Core.LogLevel.Critical => Microsoft.Extensions.Logging.LogLevel.Critical,
            _ => Microsoft.Extensions.Logging.LogLevel.Information
        };

        _logger.Log(level, exception, "[TouchSocket] {Message}", message);
    }
}
