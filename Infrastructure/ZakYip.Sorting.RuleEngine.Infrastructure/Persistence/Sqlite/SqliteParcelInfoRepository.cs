using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite;

/// <summary>
/// SQLite包裹信息仓储实现
/// SQLite parcel info repository implementation
/// </summary>
public class SqliteParcelInfoRepository : BaseParcelInfoRepository<SqliteLogDbContext>
{
    public SqliteParcelInfoRepository(
        SqliteLogDbContext context,
        ILogger<SqliteParcelInfoRepository> logger,
        ISystemClock clock)
        : base(context, logger, clock)
    {
    }
}
