using LiteDB;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ZakYip.Sorting.RuleEngine.Application.Interfaces;
using ZakYip.Sorting.RuleEngine.Application.Services;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.LiteDb;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite;
using ZakYip.Sorting.RuleEngine.Service.Configuration;

namespace ZakYip.Sorting.RuleEngine.Service;

/// <summary>
/// 主程序入口
/// Main program entry point - Windows Service with MiniAPI
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 配置Windows服务
        // Configure Windows Service
        builder.Host.UseWindowsService();

        // 配置应用设置
        // Configure application settings
        var appSettings = builder.Configuration.GetSection("AppSettings").Get<AppSettings>() 
            ?? new AppSettings();

        // 注册配置
        // Register configuration
        builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
        
        // 注册数据库熔断器配置
        // Register database circuit breaker configuration
        builder.Services.Configure<ZakYip.Sorting.RuleEngine.Infrastructure.Configuration.DatabaseCircuitBreakerSettings>(
            builder.Configuration.GetSection("AppSettings:MySql:CircuitBreaker"));

        // 配置LiteDB（用于配置存储）
        // Configure LiteDB for configuration storage
        builder.Services.AddSingleton<ILiteDatabase>(sp =>
        {
            var dbPath = Path.GetDirectoryName(appSettings.LiteDb.ConnectionString.Replace("Filename=", "").Split(';')[0]);
            if (!string.IsNullOrEmpty(dbPath) && !Directory.Exists(dbPath))
            {
                Directory.CreateDirectory(dbPath);
            }
            return new LiteDatabase(appSettings.LiteDb.ConnectionString);
        });

        // 配置日志数据库（带熔断器的弹性日志仓储）
        // Configure logging database (Resilient log repository with circuit breaker)
        ConfigureSqliteLogging(builder.Services, appSettings);
        
        if (appSettings.MySql.Enabled && !string.IsNullOrEmpty(appSettings.MySql.ConnectionString))
        {
            try
            {
                builder.Services.AddDbContext<MySqlLogDbContext>(options =>
                    options.UseMySql(
                        appSettings.MySql.ConnectionString,
                        ServerVersion.AutoDetect(appSettings.MySql.ConnectionString)),
                    ServiceLifetime.Scoped);
                
                // 使用带熔断器的弹性日志仓储
                // Use resilient log repository with circuit breaker
                builder.Services.AddScoped<ILogRepository, ResilientLogRepository>();
            }
            catch
            {
                // MySQL配置失败，使用SQLite仓储
                // MySQL configuration failed, use SQLite repository
                builder.Services.AddScoped<ILogRepository, SqliteLogRepository>();
            }
        }
        else
        {
            // MySQL未启用，直接使用SQLite仓储
            // MySQL not enabled, use SQLite repository directly
            builder.Services.AddScoped<ILogRepository, SqliteLogRepository>();
        }

        // 配置HttpClient用于第三方API
        // Configure HttpClient for third-party API
        builder.Services.AddHttpClient<IThirdPartyApiClient, ThirdPartyApiClient>(client =>
        {
            client.BaseAddress = new Uri(appSettings.ThirdPartyApi.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(appSettings.ThirdPartyApi.TimeoutSeconds);
            
            if (!string.IsNullOrEmpty(appSettings.ThirdPartyApi.ApiKey))
            {
                client.DefaultRequestHeaders.Add("X-API-Key", appSettings.ThirdPartyApi.ApiKey);
            }
        });

        // 注册仓储
        // Register repositories
        builder.Services.AddScoped<IRuleRepository, LiteDbRuleRepository>();

        // 添加内存缓存（带可配置的绝对过期和滑动过期）
        // Add memory cache with configurable absolute and sliding expiration
        builder.Services.AddMemoryCache(options =>
        {
            options.SizeLimit = null; // 不限制缓存大小
            options.CompactionPercentage = 0.25; // 压缩百分比
        });

        // 注册应用服务
        // Register application services
        builder.Services.AddScoped<IRuleEngineService, RuleEngineService>();
        builder.Services.AddScoped<IParcelProcessingService, ParcelProcessingService>();

        // 添加控制器和API服务
        // Add controllers and API services
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        
        if (appSettings.MiniApi.EnableSwagger)
        {
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new()
                {
                    Title = "分拣规则引擎 API",
                    Version = "v1",
                    Description = "ZakYip 分拣规则引擎核心系统 API"
                });
            });
        }

        // 配置CORS
        // Configure CORS
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        var app = builder.Build();

        // 初始化数据库
        // Initialize databases
        InitializeDatabases(app.Services, appSettings);

        // 配置HTTP管道
        // Configure HTTP pipeline
        if (app.Environment.IsDevelopment() || appSettings.MiniApi.EnableSwagger)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "分拣规则引擎 API v1");
            });
        }

        app.UseCors();
        app.UseRouting();
        app.MapControllers();

        // 添加最小API端点
        // Add minimal API endpoints
        ConfigureMinimalApi(app);

        app.Run();
    }

    /// <summary>
    /// 配置SQLite日志
    /// Configure SQLite logging
    /// </summary>
    private static void ConfigureSqliteLogging(IServiceCollection services, AppSettings appSettings)
    {
        var dbPath = Path.GetDirectoryName(appSettings.Sqlite.ConnectionString.Replace("Data Source=", "").Split(';')[0]);
        if (!string.IsNullOrEmpty(dbPath) && !Directory.Exists(dbPath))
        {
            Directory.CreateDirectory(dbPath);
        }

        services.AddDbContext<SqliteLogDbContext>(options =>
            options.UseSqlite(appSettings.Sqlite.ConnectionString));
        
        services.AddScoped<ILogRepository, SqliteLogRepository>();
    }

    /// <summary>
    /// 初始化数据库
    /// Initialize databases with automatic migrations
    /// </summary>
    private static void InitializeDatabases(IServiceProvider services, AppSettings appSettings)
    {
        using var scope = services.CreateScope();

        // 确保MySQL或SQLite数据库创建并应用迁移
        // Ensure MySQL or SQLite database is created and apply migrations
        if (appSettings.MySql.Enabled)
        {
            try
            {
                var mysqlContext = scope.ServiceProvider.GetService<MySqlLogDbContext>();
                if (mysqlContext != null)
                {
                    // 自动应用数据库迁移
                    // Automatically apply database migrations
                    mysqlContext.Database.Migrate();
                }
            }
            catch
            {
                // 如果MySQL失败，使用SQLite
                // If MySQL fails, use SQLite
                var sqliteContext = scope.ServiceProvider.GetService<SqliteLogDbContext>();
                if (sqliteContext != null)
                {
                    sqliteContext.Database.Migrate();
                }
            }
        }
        else
        {
            var sqliteContext = scope.ServiceProvider.GetService<SqliteLogDbContext>();
            if (sqliteContext != null)
            {
                sqliteContext.Database.Migrate();
            }
        }
    }

    /// <summary>
    /// 配置最小API端点
    /// Configure minimal API endpoints
    /// </summary>
    private static void ConfigureMinimalApi(WebApplication app)
    {
        // 健康检查端点
        // Health check endpoint
        app.MapGet("/health", () => Results.Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow
        }))
        .WithName("HealthCheck")
        .WithOpenApi();

        // 版本信息端点
        // Version info endpoint
        app.MapGet("/version", () => Results.Ok(new
        {
            version = "1.0.0",
            name = "ZakYip.Sorting.RuleEngine.Core",
            description = "分拣规则引擎核心系统"
        }))
        .WithName("Version")
        .WithOpenApi();
    }
}

