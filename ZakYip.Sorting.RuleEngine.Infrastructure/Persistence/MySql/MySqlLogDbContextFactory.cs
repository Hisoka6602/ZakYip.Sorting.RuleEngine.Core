using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql;

/// <summary>
/// 设计时工厂，用于EF Core迁移
/// Design-time factory for EF Core migrations
/// </summary>
public class MySqlLogDbContextFactory : IDesignTimeDbContextFactory<MySqlLogDbContext>
{
    public MySqlLogDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MySqlLogDbContext>();
        
        // 使用虚拟连接字符串进行迁移生成
        // Use dummy connection string for migration generation
        optionsBuilder.UseMySql(
            "Server=localhost;Database=sorting_logs;User=root;Password=password;",
            new MySqlServerVersion(new Version(8, 0, 21)));

        return new MySqlLogDbContext(optionsBuilder.Options);
    }
}
