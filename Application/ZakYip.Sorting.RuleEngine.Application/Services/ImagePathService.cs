using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.Services;

/// <summary>
/// 图片路径管理服务
/// Image path management service
/// </summary>
public class ImagePathService
{
    private readonly ILogRepository _logRepository;
    private readonly ILogger<ImagePathService> _logger;

    public ImagePathService(ILogRepository logRepository, ILogger<ImagePathService> logger)
    {
        _logRepository = logRepository;
        _logger = logger;
    }

    /// <summary>
    /// 批量更新图片路径（用于处理磁盘迁移等场景）
    /// Bulk update image paths (for handling disk migration scenarios)
    /// </summary>
    /// <param name="oldPrefix">旧路径前缀（例如：D:\） / Old path prefix (e.g., D:\)</param>
    /// <param name="newPrefix">新路径前缀（例如：E:\） / New path prefix (e.g., E:\)</param>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    /// <returns>更新的记录数 / Number of updated records</returns>
    public async Task<int> BulkUpdateImagePathsAsync(string oldPrefix, string newPrefix, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(oldPrefix))
        {
            throw new ArgumentException("Old prefix cannot be null or empty", nameof(oldPrefix));
        }

        if (string.IsNullOrWhiteSpace(newPrefix))
        {
            throw new ArgumentException("New prefix cannot be null or empty", nameof(newPrefix));
        }

        _logger.LogInformation("Starting bulk image path update from '{OldPrefix}' to '{NewPrefix}'", oldPrefix, newPrefix);

        try
        {
            var updatedCount = await _logRepository.BulkUpdateImagePathsAsync(oldPrefix, newPrefix, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Successfully updated {Count} image paths", updatedCount);
            return updatedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bulk update image paths from '{OldPrefix}' to '{NewPrefix}'", oldPrefix, newPrefix);
            throw;
        }
    }

    /// <summary>
    /// 将本地路径转换为可访问的API URL
    /// Convert local path to accessible API URL
    /// </summary>
    /// <param name="localPath">本地路径 / Local path</param>
    /// <param name="baseUrl">API基础URL / Base API URL</param>
    /// <returns>可访问的URL / Accessible URL</returns>
    public string ConvertLocalPathToUrl(string localPath, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(localPath))
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new ArgumentException("Base URL cannot be null or empty", nameof(baseUrl));
        }

        // Remove drive letter and backslashes, convert to forward slashes
        var relativePath = localPath;

        // Remove drive letter (e.g., "D:\")
        if (relativePath.Length >= 3 && relativePath[1] == ':' && relativePath[2] == '\\')
        {
            relativePath = relativePath.Substring(3);
        }

        // Convert backslashes to forward slashes
        relativePath = relativePath.Replace('\\', '/');

        // Ensure base URL doesn't end with slash and relative path doesn't start with slash
        baseUrl = baseUrl.TrimEnd('/');
        relativePath = relativePath.TrimStart('/');

        return $"{baseUrl}/{relativePath}";
    }
}
