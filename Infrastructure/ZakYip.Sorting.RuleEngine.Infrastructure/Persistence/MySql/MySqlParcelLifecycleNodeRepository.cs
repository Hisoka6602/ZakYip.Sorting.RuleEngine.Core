using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql;

/// <summary>
/// MySQL包裹生命周期节点仓储实现
/// MySQL parcel lifecycle node repository implementation
/// </summary>
public class MySqlParcelLifecycleNodeRepository : BaseParcelLifecycleNodeRepository<MySqlLogDbContext>
{
    public MySqlParcelLifecycleNodeRepository(
        MySqlLogDbContext context,
        ILogger<MySqlParcelLifecycleNodeRepository> logger,
        ISystemClock clock)
        : base(context, logger, clock)
    {
    }
}
