namespace ZakYip.Sorting.RuleEngine.Application.Interfaces;

/// <summary>
/// 配置热更新服务接口
/// Configuration hot-reload service interface
/// </summary>
public interface IConfigReloadService
{
    /// <summary>
    /// 重新加载DWS配置
    /// Reload DWS configuration
    /// </summary>
    Task ReloadDwsConfigAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 重新加载WCS配置
    /// Reload WCS configuration
    /// </summary>
    Task ReloadWcsConfigAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 重新加载分拣机配置
    /// Reload Sorter configuration
    /// </summary>
    Task ReloadSorterConfigAsync(CancellationToken cancellationToken = default);
}
