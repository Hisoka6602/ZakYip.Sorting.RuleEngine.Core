using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence;

/// <summary>
/// 包裹生命周期节点仓储基类，包含所有共享的CRUD操作
/// Base parcel lifecycle node repository with all shared CRUD operations
/// </summary>
/// <typeparam name="TContext">DbContext类型 / DbContext type</typeparam>
public abstract class BaseParcelLifecycleNodeRepository<TContext> : IParcelLifecycleNodeRepository
    where TContext : BaseLogDbContext
{
    protected readonly TContext Context;
    protected readonly ILogger Logger;
    protected readonly ISystemClock Clock;

    protected BaseParcelLifecycleNodeRepository(
        TContext context,
        ILogger logger,
        ISystemClock clock)
    {
        Context = context;
        Logger = logger;
        Clock = clock;
    }

    public virtual async Task<bool> AddAsync(ParcelLifecycleNodeEntity node, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(node);
        
        try
        {
            node.CreatedAt = Clock.LocalNow;
            if (node.EventTime == default)
                node.EventTime = Clock.LocalNow;
                
            await Context.ParcelLifecycleNodes.AddAsync(node, cancellationToken).ConfigureAwait(false);
            var result = await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return result > 0;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "添加生命周期节点失败: ParcelId={ParcelId}, Stage={Stage}", 
                node.ParcelId, node.Stage);
            return false;
        }
    }

    public virtual async Task<int> BatchAddAsync(IEnumerable<ParcelLifecycleNodeEntity> nodes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        
        var nodeList = nodes.ToList();
        if (nodeList.Count == 0)
            return 0;
        
        try
        {
            var now = Clock.LocalNow;
            foreach (var node in nodeList)
            {
                node.CreatedAt = now;
                if (node.EventTime == default)
                    node.EventTime = now;
            }
            
            await Context.ParcelLifecycleNodes.AddRangeAsync(nodeList, cancellationToken).ConfigureAwait(false);
            return await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "批量添加生命周期节点失败: Count={Count}", nodeList.Count);
            return 0;
        }
    }

    public virtual async Task<IReadOnlyList<ParcelLifecycleNodeEntity>> GetByParcelIdAsync(
        string parcelId, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parcelId);
        
        return await Context.ParcelLifecycleNodes
            .AsNoTracking()
            .Where(n => n.ParcelId == parcelId)
            .OrderByDescending(n => n.EventTime)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public virtual async Task<(IReadOnlyList<ParcelLifecycleNodeEntity> Items, int TotalCount)> GetByTimeRangeAsync(
        DateTime startTime,
        DateTime endTime,
        ParcelLifecycleStage? stage = null,
        int page = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 100;
        if (pageSize > 1000) pageSize = 1000;

        var query = Context.ParcelLifecycleNodes
            .AsNoTracking()
            .Where(n => n.EventTime >= startTime && n.EventTime <= endTime);

        if (stage.HasValue)
            query = query.Where(n => n.Stage == stage.Value);

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await query
            .OrderByDescending(n => n.EventTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (items, totalCount);
    }
}
