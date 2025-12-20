using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql;

/// <summary>
/// MySQL包裹生命周期节点仓储实现
/// MySQL parcel lifecycle node repository implementation
/// </summary>
public class MySqlParcelLifecycleNodeRepository : IParcelLifecycleNodeRepository
{
    private readonly MySqlLogDbContext _context;
    private readonly ILogger<MySqlParcelLifecycleNodeRepository> _logger;
    private readonly ISystemClock _clock;

    public MySqlParcelLifecycleNodeRepository(
        MySqlLogDbContext context,
        ILogger<MySqlParcelLifecycleNodeRepository> logger,
        ISystemClock clock)
    {
        _context = context;
        _logger = logger;
        _clock = clock;
    }

    public async Task<bool> AddAsync(ParcelLifecycleNodeEntity node, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(node);
        
        try
        {
            node.CreatedAt = _clock.LocalNow;
            if (node.EventTime == default)
                node.EventTime = _clock.LocalNow;
                
            await _context.ParcelLifecycleNodes.AddAsync(node, cancellationToken).ConfigureAwait(false);
            var result = await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加生命周期节点失败: ParcelId={ParcelId}, Stage={Stage}", 
                node.ParcelId, node.Stage);
            return false;
        }
    }

    public async Task<int> BatchAddAsync(IEnumerable<ParcelLifecycleNodeEntity> nodes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        
        var nodeList = nodes.ToList();
        if (nodeList.Count == 0)
            return 0;
        
        try
        {
            var now = _clock.LocalNow;
            foreach (var node in nodeList)
            {
                node.CreatedAt = now;
                if (node.EventTime == default)
                    node.EventTime = now;
            }
            
            await _context.ParcelLifecycleNodes.AddRangeAsync(nodeList, cancellationToken).ConfigureAwait(false);
            return await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量添加生命周期节点失败: Count={Count}", nodeList.Count);
            return 0;
        }
    }

    public async Task<IReadOnlyList<ParcelLifecycleNodeEntity>> GetByParcelIdAsync(
        string parcelId, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parcelId);
        
        return await _context.ParcelLifecycleNodes
            .AsNoTracking()
            .Where(n => n.ParcelId == parcelId)
            .OrderByDescending(n => n.EventTime)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<(IReadOnlyList<ParcelLifecycleNodeEntity> Items, int TotalCount)> GetByTimeRangeAsync(
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

        var query = _context.ParcelLifecycleNodes
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
