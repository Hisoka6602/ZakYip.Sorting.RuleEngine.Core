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
using ZakYip.Sorting.RuleEngine.Infrastructure.BackgroundServices;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.LiteDb;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite;
using ZakYip.Sorting.RuleEngine.Infrastructure.Sharding;
using ZakYip.Sorting.RuleEngine.Service.Configuration;

namespace ZakYip.Sorting.RuleEngine.Service;

/// <summary>
/// 主程序入口 - Windows服务与MiniAPI
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 配置Windows服务
        builder.Host.UseWindowsService();

        // 配置应用设置
        var appSettings = builder.Configuration.GetSection("AppSettings").Get<AppSettings>() 
            ?? new AppSettings();

        // 注册配置
        builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
        
        // 注册分片配置
        builder.Services.Configure<ShardingSettings>(builder.Configuration.GetSection("AppSettings:Sharding"));
        
        // 注册数据库熔断器配置
        builder.Services.Configure<ZakYip.Sorting.RuleEngine.Infrastructure.Configuration.DatabaseCircuitBreakerSettings>(
            builder.Configuration.GetSection("AppSettings:MySql:CircuitBreaker"));

        // 配置LiteDB（用于配置存储）
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
                builder.Services.AddScoped<ILogRepository, ResilientLogRepository>();
            }
            catch
            {
                // MySQL配置失败，使用SQLite仓储
                builder.Services.AddScoped<ILogRepository, SqliteLogRepository>();
            }
        }
        else
        {
            // MySQL未启用，直接使用SQLite仓储
            builder.Services.AddScoped<ILogRepository, SqliteLogRepository>();
        }

        // 配置HttpClient用于第三方API
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
        builder.Services.AddScoped<IRuleRepository, LiteDbRuleRepository>();

        // 添加内存缓存（带可配置的绝对过期和滑动过期）
        // 从配置读取缓存大小限制（以条目数为单位），如果未配置则使用默认值
        var cacheSizeLimit = builder.Configuration.GetValue<long?>("Cache:SizeLimit") ?? 1024;
        builder.Services.AddMemoryCache(options =>
        {
            options.SizeLimit = cacheSizeLimit; // 设置缓存大小限制
            options.CompactionPercentage = 0.25; // 压缩百分比
        });

        // 注册应用服务
        builder.Services.AddScoped<IRuleEngineService, RuleEngineService>();
        builder.Services.AddScoped<IParcelProcessingService, ParcelProcessingService>();
        
        // 注册包裹活动追踪器（用于空闲检测）
        builder.Services.AddSingleton<IParcelActivityTracker, ZakYip.Sorting.RuleEngine.Infrastructure.Services.ParcelActivityTracker>();
        
        // 注册事件驱动服务
        builder.Services.AddSingleton<ParcelOrchestrationService>();
        builder.Services.AddMediatR(cfg => 
        {
            cfg.RegisterServicesFromAssembly(typeof(ZakYip.Sorting.RuleEngine.Application.Services.RuleEngineService).Assembly);
        });
        
        // 注册后台服务
        builder.Services.AddHostedService<ParcelQueueProcessorService>();
        builder.Services.AddHostedService<DataCleanupService>();
        builder.Services.AddHostedService<DataArchiveService>();
        builder.Services.AddHostedService<MySqlAutoTuningService>();
        builder.Services.AddHostedService<ShardingTableManagementService>();

        // 添加控制器和API服务
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        
        // 添加SignalR服务
        builder.Services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
            options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
        });
        
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

        // 配置CORS（SignalR需要）
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
        InitializeDatabases(app.Services, appSettings);

        // 配置HTTP管道
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
        
        // 映射SignalR Hub端点
        app.MapHub<ZakYip.Sorting.RuleEngine.Service.Hubs.SortingHub>("/hubs/sorting");
        app.MapHub<ZakYip.Sorting.RuleEngine.Service.Hubs.DwsHub>("/hubs/dws");

        // 添加最小API端点
        ConfigureMinimalApi(app);

        app.Run();
    }

    /// <summary>
    /// 配置SQLite日志
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
    /// 初始化数据库并自动应用迁移
    /// </summary>
    private static void InitializeDatabases(IServiceProvider services, AppSettings appSettings)
    {
        using var scope = services.CreateScope();

        // 确保MySQL或SQLite数据库创建并应用迁移
        if (appSettings.MySql.Enabled)
        {
            try
            {
                var mysqlContext = scope.ServiceProvider.GetService<MySqlLogDbContext>();
                if (mysqlContext != null)
                {
                    // 自动应用数据库迁移
                    mysqlContext.Database.Migrate();
                }
            }
            catch
            {
                // 如果MySQL失败，使用SQLite
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
    /// </summary>
    private static void ConfigureMinimalApi(WebApplication app)
    {
        // 健康检查端点
        app.MapGet("/health", () => Results.Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow
        }))
        .WithName("HealthCheck")
        .WithOpenApi();

        // 版本信息端点
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

