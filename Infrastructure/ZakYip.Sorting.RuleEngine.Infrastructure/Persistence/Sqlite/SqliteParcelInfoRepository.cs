using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite;

/// <summary>
/// SQLite包裹信息仓储实现
/// SQLite parcel info repository implementation
/// </summary>
public class SqliteParcelInfoRepository : IParcelInfoRepository
{
    private readonly SqliteLogDbContext _context;
    private readonly ILogger<SqliteParcelInfoRepository> _logger;
    private readonly ISystemClock _clock;

    public SqliteParcelInfoRepository(
        SqliteLogDbContext context,
        ILogger<SqliteParcelInfoRepository> logger,
        ISystemClock clock)
    {
        _context = context;
        _logger = logger;
        _clock = clock;
    }

    public async Task<ParcelInfo?> GetByIdAsync(string parcelId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parcelId);
        
        return await _context.ParcelInfos
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ParcelId == parcelId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> AddAsync(ParcelInfo parcelInfo, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parcelInfo);
        
        try
        {
            parcelInfo.CreatedAt = _clock.LocalNow;
            await _context.ParcelInfos.AddAsync(parcelInfo, cancellationToken).ConfigureAwait(false);
            var result = await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加包裹信息失败: ParcelId={ParcelId}", parcelInfo.ParcelId);
            return false;
        }
    }

    public async Task<bool> UpdateAsync(ParcelInfo parcelInfo, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parcelInfo);
        
        try
        {
            parcelInfo.UpdatedAt = _clock.LocalNow;
            _context.ParcelInfos.Update(parcelInfo);
            var result = await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新包裹信息失败: ParcelId={ParcelId}", parcelInfo.ParcelId);
            return false;
        }
    }

    public async Task<int> BatchUpdateAsync(IEnumerable<ParcelInfo> parcelInfos, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parcelInfos);
        
        var parcelList = parcelInfos.ToList();
        if (parcelList.Count == 0)
            return 0;
        
        try
        {
            var now = _clock.LocalNow;
            foreach (var parcel in parcelList)
            {
                parcel.UpdatedAt = now;
            }
            
            _context.ParcelInfos.UpdateRange(parcelList);
            return await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量更新包裹信息失败: Count={Count}", parcelList.Count);
            return 0;
        }
    }

    public async Task<(IReadOnlyList<ParcelInfo> Items, int TotalCount)> SearchAsync(
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

        var query = _context.ParcelInfos.AsNoTracking();

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

    public async Task<ParcelInfo?> GetLatestWithoutDwsDataAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ParcelInfos
            .AsNoTracking()
            .Where(p => p.Weight == null || p.Volume == null)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ParcelInfo>> GetByBagIdAsync(string bagId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bagId);
        
        return await _context.ParcelInfos
            .AsNoTracking()
            .Where(p => p.BagId == bagId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
