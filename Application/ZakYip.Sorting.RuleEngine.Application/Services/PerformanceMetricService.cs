using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.Services;

/// <summary>
/// 性能指标收集服务
/// </summary>
public class PerformanceMetricService
{
    private readonly IServiceScopeFactory? _serviceScopeFactory;
    private readonly ILogger<PerformanceMetricService> _logger;

    public PerformanceMetricService(
        ILogger<PerformanceMetricService> logger,
        IServiceScopeFactory? serviceScopeFactory = null)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    /// <summary>
    /// 执行操作并记录性能指标
    /// </summary>
    public async Task<T> ExecuteWithMetricsAsync<T>(
        string operationName,
        Func<Task<T>> operation,
        string? parcelId = null,
        string? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;
        string? errorMessage = null;
        T? result = default;

        try
        {
            result = await operation();
            success = true;
            return result;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            throw;
        }
        finally
        {
            stopwatch.Stop();

            var metric = new PerformanceMetric
            {
                OperationName = operationName,
                ParcelId = parcelId,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Success = success,
                ErrorMessage = errorMessage,
                Metadata = metadata
            };

            await RecordMetricAsync(metric, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 记录性能指标
    /// </summary>
    private async Task RecordMetricAsync(PerformanceMetric metric, CancellationToken cancellationToken)
    {
        try
        {
            if (_serviceScopeFactory != null)
            {
                // 使用 IServiceScopeFactory 创建 scope 来访问 scoped repository
                // Use IServiceScopeFactory to create scope to access scoped repository
                using var scope = _serviceScopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetService<IPerformanceMetricRepository>();
                if (repository != null)
                {
                    await repository.RecordMetricAsync(metric, cancellationToken).ConfigureAwait(false);
                }
            }

            // 同时记录到日志
            _logger.LogInformation(
                "性能指标 - 操作: {Operation}, 包裹: {ParcelId}, 时长: {Duration}ms, 成功: {Success}",
                metric.OperationName,
                metric.ParcelId ?? "N/A",
                metric.DurationMs,
                metric.Success);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "记录性能指标失败");
        }
    }
}
