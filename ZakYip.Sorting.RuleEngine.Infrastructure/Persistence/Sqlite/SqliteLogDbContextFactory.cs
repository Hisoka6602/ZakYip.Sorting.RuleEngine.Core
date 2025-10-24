using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite;

/// <summary>
/// 设计时工厂，用于EF Core迁移
/// Design-time factory for EF Core migrations
/// </summary>
public class SqliteLogDbContextFactory : IDesignTimeDbContextFactory<SqliteLogDbContext>
{
    private const string ConnectionString = "Data Source=logs.db";

    public SqliteLogDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SqliteLogDbContext>();
        
        // 使用虚拟连接字符串进行迁移生成
        // Use dummy connection string for migration generation
        optionsBuilder.UseSqlite(ConnectionString);

        return new SqliteLogDbContext(optionsBuilder.Options);
    }
}
