using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite;

/// <summary>
/// SQLite包裹生命周期节点仓储实现
/// SQLite parcel lifecycle node repository implementation
/// </summary>
public class SqliteParcelLifecycleNodeRepository : BaseParcelLifecycleNodeRepository<SqliteLogDbContext>
{
    public SqliteParcelLifecycleNodeRepository(
        SqliteLogDbContext context,
        ILogger<SqliteParcelLifecycleNodeRepository> logger,
        ISystemClock clock)
        : base(context, logger, clock)
    {
    }
}
