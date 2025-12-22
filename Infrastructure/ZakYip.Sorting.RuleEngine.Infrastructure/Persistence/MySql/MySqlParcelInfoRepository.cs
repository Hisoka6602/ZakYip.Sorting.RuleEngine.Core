using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql;

/// <summary>
/// MySQL包裹信息仓储实现
/// MySQL parcel info repository implementation
/// </summary>
public class MySqlParcelInfoRepository : BaseParcelInfoRepository<MySqlLogDbContext>
{
    public MySqlParcelInfoRepository(
        MySqlLogDbContext context,
        ILogger<MySqlParcelInfoRepository> logger,
        ISystemClock clock)
        : base(context, logger, clock)
    {
    }
}
