using LiteDB;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using NLog;
using NLog.Web;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
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
        // 配置NLog
        var logger = LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();
        
        try
        {
            logger.Info("应用程序启动中...");
            
            var builder = WebApplication.CreateBuilder(args);

#if !DEBUG
            // 仅在Release模式下配置Windows服务
            builder.Host.UseWindowsService();
            logger.Info("Windows服务模式已启用");
#else
            logger.Info("控制台模式运行（DEBUG模式）");
#endif

            // 配置NLog作为日志提供程序
            builder.Logging.ClearProviders();
            builder.Host.UseNLog();

            // 配置应用设置（需要提前读取以配置URL）
            // Load application settings early to configure URLs
            var appSettings = builder.Configuration.GetSection("AppSettings").Get<AppSettings>() 
                ?? new AppSettings();

            // 配置Kestrel服务器和监听URL
            builder.WebHost.UseKestrel(options =>
            {
                options.AddServerHeader = false; // 不发送Server头以提高安全性
                options.Limits.MaxConcurrentConnections = 1000;
                options.Limits.MaxConcurrentUpgradedConnections = 1000;
                options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB
                options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
                options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
            });

            // 配置监听URL（仅当未通过命令行参数指定时才使用配置文件中的URL）
            // Configure listen URLs (only use config file URLs if not specified via command line)
            var urls = builder.Configuration["urls"];
            if (string.IsNullOrEmpty(urls) && 
                appSettings.MiniApi?.Urls != null && 
                appSettings.MiniApi.Urls.Length > 0)
            {
                builder.WebHost.UseUrls(appSettings.MiniApi.Urls);
            }

        // 注册配置
        builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
        
        // 注册分片配置
        builder.Services.Configure<ShardingSettings>(builder.Configuration.GetSection("AppSettings:Sharding"));
        
        // 注册日志文件清理配置
        builder.Services.Configure<ZakYip.Sorting.RuleEngine.Infrastructure.Configuration.LogFileCleanupSettings>(
            builder.Configuration.GetSection("AppSettings:LogFileCleanup"));
        
        // 注册数据库熔断器配置
        builder.Services.Configure<ZakYip.Sorting.RuleEngine.Infrastructure.Configuration.DatabaseCircuitBreakerSettings>(
            builder.Configuration.GetSection("AppSettings:MySql:CircuitBreaker"));

        // 注册数据库方言
        // Register database dialects
        builder.Services.AddSingleton<ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Dialects.MySqlDialect>();
        builder.Services.AddSingleton<ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Dialects.SqliteDialect>();
        
        // 根据配置选择主数据库方言
        // Select primary database dialect based on configuration
        if (appSettings.MySql.Enabled && !string.IsNullOrEmpty(appSettings.MySql.ConnectionString))
        {
            builder.Services.AddSingleton<ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Dialects.IDatabaseDialect>(
                sp => sp.GetRequiredService<ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Dialects.MySqlDialect>());
        }
        else
        {
            builder.Services.AddSingleton<ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Dialects.IDatabaseDialect>(
                sp => sp.GetRequiredService<ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Dialects.SqliteDialect>());
        }

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
                logger.Info("尝试配置MySQL数据库连接...");
                
                // 使用配置的服务器版本或默认版本，避免在服务配置阶段连接数据库
                // Use configured server version or default version to avoid connecting to database during service configuration
                var serverVersion = !string.IsNullOrEmpty(appSettings.MySql.ServerVersion)
                    ? ServerVersion.Parse(appSettings.MySql.ServerVersion)
                    : new MySqlServerVersion(new Version(8, 0, 21)); // 默认使用MySQL 8.0.21，作为最低兼容版本。可通过配置使用更高版本（如8.0.33），但默认选择8.0.21以确保与大多数生产环境兼容。
                
                builder.Services.AddDbContext<MySqlLogDbContext>(options =>
                    options.UseMySql(
                        appSettings.MySql.ConnectionString,
                        serverVersion),
                    ServiceLifetime.Scoped);
                
                // 如果启用了分片功能，也注册ShardedLogDbContext
                // Register ShardedLogDbContext if sharding is enabled
                var shardingEnabled = builder.Configuration.GetValue<bool>("AppSettings:Sharding:Enabled");
                if (shardingEnabled)
                {
                    builder.Services.AddDbContext<ShardedLogDbContext>(options =>
                        options.UseMySql(
                            appSettings.MySql.ConnectionString,
                            serverVersion),
                        ServiceLifetime.Scoped);
                    logger.Info("分片数据库上下文已注册");
                }
                
                logger.Info("MySQL数据库连接配置成功，使用弹性日志仓储");
                // 使用带熔断器的弹性日志仓储
                builder.Services.AddScoped<ILogRepository, ResilientLogRepository>();
            }
            catch (Exception ex)
            {
                // MySQL配置失败，使用SQLite仓储
                logger.Warn(ex, "MySQL数据库连接配置失败，降级使用SQLite仓储: {Message}", ex.Message);
                builder.Services.AddScoped<ILogRepository, SqliteLogRepository>();
            }
        }
        else
        {
            logger.Info("MySQL未启用或连接字符串为空，使用SQLite仓储");
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
        builder.Services.AddScoped<IChuteRepository, LiteDbChuteRepository>();
        builder.Services.AddScoped<IThirdPartyApiConfigRepository, LiteDbThirdPartyApiConfigRepository>();
        builder.Services.AddScoped<IPerformanceMetricRepository, LiteDbPerformanceMetricRepository>();

        // 添加内存缓存（带可配置的绝对过期和滑动过期）
        // 从配置读取缓存大小限制（以条目数为单位），如果未配置则使用默认值
        var cacheSizeLimit = builder.Configuration.GetValue<long?>("Cache:SizeLimit") ?? 1024;
        builder.Services.AddMemoryCache(options =>
        {
            options.SizeLimit = cacheSizeLimit; // 设置缓存大小限制
            options.CompactionPercentage = 0.25; // 压缩百分比
        });

        // 注册应用服务
        builder.Services.AddScoped<PerformanceMetricService>();
        builder.Services.AddScoped<IRuleEngineService, RuleEngineService>();
        builder.Services.AddScoped<IParcelProcessingService, ParcelProcessingService>();
        builder.Services.AddScoped<RuleValidationService>();
        builder.Services.AddScoped<IGanttChartService, ZakYip.Sorting.RuleEngine.Infrastructure.Services.GanttChartService>();
        builder.Services.AddScoped<IChuteStatisticsService, ZakYip.Sorting.RuleEngine.Infrastructure.Services.ChuteStatisticsService>();
        
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
        builder.Services.AddHostedService<LogFileCleanupService>();

        // 添加控制器和API服务
        builder.Services.AddControllers(options =>
        {
            // 添加全局模型验证过滤器
            options.Filters.Add<ZakYip.Sorting.RuleEngine.Infrastructure.Filters.ModelValidationFilter>();
        })
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
            });
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
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "分拣规则引擎 API",
                    Version = "v1",
                    Description = "ZakYip 分拣规则引擎核心系统 API - 提供包裹分拣、规则管理、格口管理、日志查询等功能"
                });
                
                // 启用XML文档注释 - Service项目
                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
                }
                
                // 启用XML文档注释 - Domain项目
                var domainXmlFile = "ZakYip.Sorting.RuleEngine.Domain.xml";
                var domainXmlPath = Path.Combine(AppContext.BaseDirectory, domainXmlFile);
                if (File.Exists(domainXmlPath))
                {
                    c.IncludeXmlComments(domainXmlPath, includeControllerXmlComments: true);
                }
                
                // 启用XML文档注释 - Application项目
                var applicationXmlFile = "ZakYip.Sorting.RuleEngine.Application.xml";
                var applicationXmlPath = Path.Combine(AppContext.BaseDirectory, applicationXmlFile);
                if (File.Exists(applicationXmlPath))
                {
                    c.IncludeXmlComments(applicationXmlPath, includeControllerXmlComments: true);
                }
                
                // 启用数据注解
                c.EnableAnnotations();
                
                // 添加枚举架构过滤器，用于显示枚举值的描述
                c.SchemaFilter<ZakYip.Sorting.RuleEngine.Service.Filters.EnumSchemaFilter>();
            });
        }

        // 配置CORS（用于API和SignalR等端点）
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
        
        // 添加API请求日志中间件
        app.UseMiddleware<ZakYip.Sorting.RuleEngine.Infrastructure.Middleware.ApiRequestLoggingMiddleware>();

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
        catch (Exception ex)
        {
            logger.Error(ex, "应用程序启动时发生严重错误");
            throw;
        }
        finally
        {
            LogManager.Shutdown();
        }
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
    /// 确保MySQL数据库存在，如果不存在则创建
    /// </summary>
    private static bool EnsureMySqlDatabaseExists(string connectionString, NLog.Logger logger)
    {
        try
        {
            // 解析连接字符串获取数据库名称
            var builder = new MySqlConnector.MySqlConnectionStringBuilder(connectionString);
            var databaseName = builder.Database;
            
            if (string.IsNullOrEmpty(databaseName))
            {
                logger.Error("MySQL连接字符串中未指定数据库名称");
                return false;
            }
            
            // 创建不包含数据库名称的连接字符串，用于连接到MySQL服务器
            builder.Database = "";
            var serverConnectionString = builder.ConnectionString;
            
            logger.Info("检查MySQL服务器连接: {Server}:{Port}", builder.Server, builder.Port);
            
            // 验证数据库名称，防止SQL注入
            if (!System.Text.RegularExpressions.Regex.IsMatch(databaseName, @"^[a-zA-Z0-9_]+$"))
            {
                logger.Error("数据库名称包含非法字符: {Database}", databaseName);
                return false;
            }
            
            // 连接到MySQL服务器（不指定数据库）
            using (var connection = new MySqlConnector.MySqlConnection(serverConnectionString))
            {
                connection.Open();
                logger.Info("成功连接到MySQL服务器");
                
                // 检查数据库是否存在
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT SCHEMA_NAME FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = @databaseName";
                    command.Parameters.AddWithValue("@databaseName", databaseName);
                    var result = command.ExecuteScalar();
                    
                    if (result == null)
                    {
                        // 数据库不存在，创建它
                        logger.Info("数据库 '{Database}' 不存在，正在创建...", databaseName);
                        // 对于CREATE DATABASE语句，MySQL不支持参数化数据库名
                        // 但我们已通过正则表达式验证了名称只包含字母、数字和下划线，因此是安全的
                        command.Parameters.Clear();
                        command.CommandText = $"CREATE DATABASE `{databaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci";
                        command.ExecuteNonQuery();
                        logger.Info("成功创建数据库 '{Database}'", databaseName);
                    }
                    else
                    {
                        logger.Info("数据库 '{Database}' 已存在", databaseName);
                    }
                }
            }
            
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "确保MySQL数据库存在时发生错误: {Message}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 初始化数据库并自动应用迁移
    /// </summary>
    private static void InitializeDatabases(IServiceProvider services, AppSettings appSettings)
    {
        var logger = LogManager.GetCurrentClassLogger();
        using var scope = services.CreateScope();

        // 确保MySQL或SQLite数据库创建并应用迁移
        if (appSettings.MySql.Enabled)
        {
            try
            {
                var mysqlContext = scope.ServiceProvider.GetService<MySqlLogDbContext>();
                if (mysqlContext != null)
                {
                    logger.Info("尝试应用MySQL数据库迁移...");
                    
                    // 首先确保数据库存在
                    if (!EnsureMySqlDatabaseExists(appSettings.MySql.ConnectionString, logger))
                    {
                        logger.Warn("无法确保MySQL数据库存在");
                        throw new InvalidOperationException("无法确保MySQL数据库存在");
                    }
                    
                    // 检查数据库连接
                    var canConnect = mysqlContext.Database.CanConnect();
                    if (!canConnect)
                    {
                        logger.Warn("无法连接到MySQL数据库");
                        throw new InvalidOperationException("无法连接到MySQL数据库");
                    }
                    
                    // 自动应用数据库迁移（这会创建表如果不存在）
                    mysqlContext.Database.Migrate();
                    logger.Info("MySQL数据库迁移成功");
                }
            }
            catch (Exception ex)
            {
                // 如果MySQL失败，使用SQLite
                logger.Warn(ex, "MySQL数据库迁移失败，降级使用SQLite: {Message}", ex.Message);
                try
                {
                    var sqliteContext = scope.ServiceProvider.GetService<SqliteLogDbContext>();
                    if (sqliteContext != null)
                    {
                        logger.Info("应用SQLite数据库迁移...");
                        sqliteContext.Database.Migrate();
                        logger.Info("SQLite数据库迁移成功");
                    }
                }
                catch (Exception sqliteEx)
                {
                    logger.Error(sqliteEx, "SQLite数据库迁移也失败: {Message}", sqliteEx.Message);
                }
            }
        }
        else
        {
            try
            {
                logger.Info("MySQL未启用，使用SQLite数据库");
                var sqliteContext = scope.ServiceProvider.GetService<SqliteLogDbContext>();
                if (sqliteContext != null)
                {
                    logger.Info("应用SQLite数据库迁移...");
                    sqliteContext.Database.Migrate();
                    logger.Info("SQLite数据库迁移成功");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "SQLite数据库迁移失败: {Message}", ex.Message);
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

