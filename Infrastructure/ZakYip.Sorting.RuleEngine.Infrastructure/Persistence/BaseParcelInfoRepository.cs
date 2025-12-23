using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence;

/// <summary>
/// 包裹信息仓储基类，包含所有共享的CRUD操作
/// Base parcel info repository with all shared CRUD operations
/// </summary>
/// <typeparam name="TContext">DbContext类型 / DbContext type</typeparam>
public abstract class BaseParcelInfoRepository<TContext> : IParcelInfoRepository
    where TContext : BaseLogDbContext
{
    protected readonly TContext Context;
    protected readonly ILogger Logger;
    protected readonly ISystemClock Clock;

    protected BaseParcelInfoRepository(
        TContext context,
        ILogger logger,
        ISystemClock clock)
    {
        Context = context;
        Logger = logger;
        Clock = clock;
    }

    public virtual async Task<ParcelInfo?> GetByIdAsync(string parcelId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parcelId);
        
        try
        {
            return await Context.ParcelInfos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ParcelId == parcelId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex.Message.Contains("doesn't exist") || ex.Message.Contains("不存在"))
        {
            // 表不存在时返回null，不抛出异常，确保不影响业务流程
            // Return null when table doesn't exist, don't throw exception to ensure business flow continues
            Logger.LogWarning(ex,
                "数据库表不存在，返回null: ParcelId={ParcelId}",
                parcelId);
            return null;
        }
    }

    public virtual async Task<bool> AddAsync(ParcelInfo parcelInfo, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parcelInfo);
        
        try
        {
            parcelInfo.CreatedAt = Clock.LocalNow;
            await Context.ParcelInfos.AddAsync(parcelInfo, cancellationToken).ConfigureAwait(false);
            var result = await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return result > 0;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "添加包裹信息失败: ParcelId={ParcelId}", parcelInfo.ParcelId);
            return false;
        }
    }

    public virtual async Task<bool> UpdateAsync(ParcelInfo parcelInfo, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parcelInfo);
        
        try
        {
            parcelInfo.UpdatedAt = Clock.LocalNow;
            Context.ParcelInfos.Update(parcelInfo);
            var result = await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return result > 0;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "更新包裹信息失败: ParcelId={ParcelId}", parcelInfo.ParcelId);
            return false;
        }
    }

    public virtual async Task<int> BatchUpdateAsync(IEnumerable<ParcelInfo> parcelInfos, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parcelInfos);
        
        var parcelList = parcelInfos.ToList();
        if (parcelList.Count == 0)
            return 0;
        
        try
        {
            var now = Clock.LocalNow;
            foreach (var parcel in parcelList)
            {
                parcel.UpdatedAt = now;
            }
            
            Context.ParcelInfos.UpdateRange(parcelList);
            return await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "批量更新包裹信息失败: Count={Count}", parcelList.Count);
            return 0;
        }
    }

    public virtual async Task<(IReadOnlyList<ParcelInfo> Items, int TotalCount)> SearchAsync(
        ParcelStatus? status = null,
        ParcelLifecycleStage? lifecycleStage = null,
        string? bagId = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int page = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 100;
        if (pageSize > 1000) pageSize = 1000;

        var query = Context.ParcelInfos.AsNoTracking();

        // 应用过滤条件
        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        if (lifecycleStage.HasValue)
            query = query.Where(p => p.LifecycleStage == lifecycleStage.Value);

        if (!string.IsNullOrWhiteSpace(bagId))
            query = query.Where(p => p.BagId == bagId);

        if (startTime.HasValue)
            query = query.Where(p => p.CreatedAt >= startTime.Value);

        if (endTime.HasValue)
            query = query.Where(p => p.CreatedAt <= endTime.Value);

        // 获取总数
        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        // 分页并排序
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (items, totalCount);
    }

    public virtual async Task<ParcelInfo?> GetLatestWithoutDwsDataAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await Context.ParcelInfos
                .AsNoTracking()
                .Where(p => string.IsNullOrEmpty(p.Barcode))
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex.Message.Contains("doesn't exist") || ex.Message.Contains("不存在"))
        {
            // 表不存在时返回null，不抛出异常
            // Return null when table doesn't exist, don't throw exception
            Logger.LogWarning(ex,
                "数据库表不存在，无法查找最新未绑定包裹，返回null");
            return null;
        }
    }

    public virtual async Task<IReadOnlyList<ParcelInfo>> GetByBagIdAsync(string bagId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bagId);
        
        return await Context.ParcelInfos
            .AsNoTracking()
            .Where(p => p.BagId == bagId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
