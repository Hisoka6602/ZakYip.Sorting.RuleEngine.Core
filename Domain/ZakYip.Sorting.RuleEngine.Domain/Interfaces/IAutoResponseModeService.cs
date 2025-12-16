namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 自动应答模式服务接口
/// Auto-response mode service interface
/// </summary>
public interface IAutoResponseModeService
{
    /// <summary>
    /// 启用自动应答模式
    /// Enable auto-response mode
    /// </summary>
    /// <param name="chuteNumbers">可选的格口号数组，例如 [1,2,3,4,5,6]。如果未指定，默认使用 [1,2,3] / Optional chute numbers array, e.g. [1,2,3,4,5,6]. Defaults to [1,2,3] if not specified</param>
    void Enable(int[]? chuteNumbers = null);

    /// <summary>
    /// 禁用自动应答模式
    /// Disable auto-response mode
    /// </summary>
    void Disable();

    /// <summary>
    /// 获取自动应答模式状态
    /// Get auto-response mode status
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// 获取当前配置的格口号数组
    /// Get current configured chute numbers array
    /// </summary>
    int[] ChuteNumbers { get; }
}
