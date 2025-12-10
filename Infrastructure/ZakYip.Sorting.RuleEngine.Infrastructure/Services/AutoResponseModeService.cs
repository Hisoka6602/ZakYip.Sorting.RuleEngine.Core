using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Services;

/// <summary>
/// 自动应答模式服务实现
/// Auto-response mode service implementation
/// </summary>
public class AutoResponseModeService : IAutoResponseModeService
{
    private readonly ILogger<AutoResponseModeService> _logger;
    private bool _isEnabled;
    private readonly object _lock = new();

    public AutoResponseModeService(ILogger<AutoResponseModeService> logger)
    {
        _logger = logger;
        _isEnabled = false; // 默认关闭 / Default disabled
    }

    /// <summary>
    /// 启用自动应答模式
    /// Enable auto-response mode
    /// </summary>
    public void Enable()
    {
        lock (_lock)
        {
            if (!_isEnabled)
            {
                _isEnabled = true;
                _logger.LogInformation("自动应答模式已启用 / Auto-response mode enabled");
            }
        }
    }

    /// <summary>
    /// 禁用自动应答模式
    /// Disable auto-response mode
    /// </summary>
    public void Disable()
    {
        lock (_lock)
        {
            if (_isEnabled)
            {
                _isEnabled = false;
                _logger.LogInformation("自动应答模式已禁用 / Auto-response mode disabled");
            }
        }
    }

    /// <summary>
    /// 获取自动应答模式状态
    /// Get auto-response mode status
    /// </summary>
    public bool IsEnabled
    {
        get
        {
            lock (_lock)
            {
                return _isEnabled;
            }
        }
    }
}
