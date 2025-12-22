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
    private int[] _chuteNumbers = [1, 2, 3]; // 默认格口数组 / Default chute array
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
    /// <param name="chuteNumbers">可选的格口号数组 / Optional chute numbers array</param>
    public void Enable(int[]? chuteNumbers = null)
    {
        lock (_lock)
        {
            if (chuteNumbers != null && chuteNumbers.Length > 0)
            {
                // 创建副本以防止外部修改内部数组 / Create a copy to prevent external modification
                _chuteNumbers = (int[])chuteNumbers.Clone();
                _logger.LogInformation(
                    "自动应答模式已启用，使用自定义格口数组: [{ChuteNumbers}] / Auto-response mode enabled with custom chute array: [{ChuteNumbers}]",
                    string.Join(", ", _chuteNumbers));
            }
            else
            {
                _logger.LogInformation(
                    "自动应答模式已启用，使用默认格口数组: [{ChuteNumbers}] / Auto-response mode enabled with default chute array: [{ChuteNumbers}]",
                    string.Join(", ", _chuteNumbers));
            }
            
            _isEnabled = true;
            
            // 警告：自动应答模式与规则分拣模式互斥
            // Warning: Auto-response mode is mutually exclusive with rule sorting mode
            _logger.LogWarning(
                "⚠️ 自动应答模式已启用。此模式与规则分拣模式互斥。" +
                "系统将从配置的格口数组中随机选择格口，不会使用规则引擎进行匹配。" +
                " / Auto-response mode enabled. This mode is mutually exclusive with rule sorting mode. " +
                "The system will randomly select chutes from the configured array and will not use the rule engine.");
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
                _logger.LogInformation(
                    "自动应答模式已禁用。系统将恢复使用规则分拣模式。" +
                    " / Auto-response mode disabled. System will resume using rule sorting mode.");
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

    /// <summary>
    /// 获取当前配置的格口号数组
    /// Get current configured chute numbers array
    /// </summary>
    public int[] ChuteNumbers
    {
        get
        {
            lock (_lock)
            {
                // 返回副本，防止外部修改内部数组 / Return a copy to prevent external modification
                return (int[])_chuteNumbers.Clone();
            }
        }
    }
}
