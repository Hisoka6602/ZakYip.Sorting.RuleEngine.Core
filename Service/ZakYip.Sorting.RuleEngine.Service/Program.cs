using NLog;
using LiteDB;
using NLog.Web;
using MySqlConnector;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Application.Services;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using ZakYip.Sorting.RuleEngine.Service.Configuration;
using ZakYip.Sorting.RuleEngine.Application.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.Sharding;
using ZakYip.Sorting.RuleEngine.Infrastructure.Services;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.WdtWms;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql;
using ZakYip.Sorting.RuleEngine.Infrastructure.BackgroundServices;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.LiteDb;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.JushuitanErp;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.PostCollection;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.PostProcessingCenter;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.ApiCommunicationLogs;

// 配置NLog
var logger = LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();
// 中文注释：强制修正工作目录，避免相对路径（如 appsettings.json）解析到 System32
Directory.SetCurrentDirectory(AppContext.BaseDirectory);

try
{
    logger.Info("应用程序启动中...");

    var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddEnvironmentVariables()
        .AddCommandLine(args)
        .Build();

    var appSettingsForUrls = configuration.GetSection("AppSettings").Get<AppSettings>();

    if (appSettingsForUrls == null)
    {
        logger.Warn("配置文件中未找到 AppSettings 节点或反序列化失败，已使用默认配置值。");
        appSettingsForUrls = new AppSettings();
    }

    var host = Host.CreateDefaultBuilder(args)
#if !DEBUG
        // 仅在Release模式下配置Windows服务
        .UseWindowsService()
#endif
        .ConfigureAppConfiguration((context, configBuilder) =>
        {
            // 复用预先构建的 configuration，避免重复读取配置文件
            // Reuse pre-built configuration to avoid reading config files twice
            configBuilder.Sources.Clear();
            configBuilder.AddConfiguration(configuration);
        })
        .ConfigureLogging(logging =>
        {
            logging.ClearProviders();
        })
        .UseNLog()
        .ConfigureWebHostDefaults(webBuilder =>
        {
            // 配置监听URL（在ConfigureWebHostDefaults开始时立即配置）
            // Configure listen URLs (configure immediately at the start of ConfigureWebHostDefaults)
            if (appSettingsForUrls.MiniApi?.Urls != null && appSettingsForUrls.MiniApi.Urls.Length > 0)
            {
                // 检查是否通过命令行参数指定了URLs
                // Check if URLs are specified via command line arguments
                var urlsFromArgs = args.Any(a => a.StartsWith("--urls=", StringComparison.OrdinalIgnoreCase) ||
                                                  a.Equals("--urls", StringComparison.OrdinalIgnoreCase));
                if (!urlsFromArgs)
                {
                    logger.Info("从配置文件应用监听地址: {Urls}", string.Join(", ", appSettingsForUrls.MiniApi.Urls));
                    webBuilder.UseUrls(appSettingsForUrls.MiniApi.Urls);
                }
                else
                {
                    logger.Info("使用命令行指定的监听地址");
                }
            }

            webBuilder.ConfigureServices((context, services) =>
            {
                var configuration = context.Configuration;

                // 配置应用设置（需要提前读取以配置URL）
                // Load application settings early to configure URLs
                var appSettings = configuration.GetSection("AppSettings").Get<AppSettings>()
                    ?? new AppSettings();

                // 注册配置
                services.Configure<AppSettings>(configuration.GetSection("AppSettings"));

                // 注册分片配置
                services.Configure<ShardingSettings>(configuration.GetSection("AppSettings:Sharding"));

                // 注册日志文件清理配置
                services.Configure<ZakYip.Sorting.RuleEngine.Infrastructure.Configuration.LogFileCleanupSettings>(
                    configuration.GetSection("AppSettings:LogFileCleanup"));

                // 注册数据库熔断器配置
                services.Configure<ZakYip.Sorting.RuleEngine.Infrastructure.Configuration.DatabaseCircuitBreakerSettings>(
                    configuration.GetSection("AppSettings:MySql:CircuitBreaker"));

                // 注册IDwsTimeoutSettings接口（用于Application层，从LiteDB加载）
                // Register IDwsTimeoutSettings interface (for Application layer, loaded from LiteDB)
                services.AddSingleton<ZakYip.Sorting.RuleEngine.Domain.Interfaces.IDwsTimeoutSettings,
                    ZakYip.Sorting.RuleEngine.Infrastructure.Configuration.DwsTimeoutSettingsFromDb>();

                // 注册数据库方言
                // Register database dialects
                services.AddSingleton<ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Dialects.MySqlDialect>();
                services.AddSingleton<ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Dialects.SqliteDialect>();

                // 根据配置选择主数据库方言
                // Select primary database dialect based on configuration
                if (appSettings.MySql.Enabled && !string.IsNullOrEmpty(appSettings.MySql.ConnectionString))
                {
                    services.AddSingleton<ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Dialects.IDatabaseDialect>(
                        sp => sp.GetRequiredService<ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Dialects.MySqlDialect>());
                }
                else
                {
                    services.AddSingleton<ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Dialects.IDatabaseDialect>(
                        sp => sp.GetRequiredService<ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Dialects.SqliteDialect>());
                }

                // 配置LiteDB（用于配置存储）
                // Configure LiteDB (for configuration storage)
                services.AddSingleton<ILiteDatabase>(sp =>
                {
                    var dbPath = Path.GetDirectoryName(appSettings.LiteDb.ConnectionString.Replace("Filename=", "").Split(';')[0]);
                    if (!string.IsNullOrEmpty(dbPath) && !Directory.Exists(dbPath))
                    {
                        Directory.CreateDirectory(dbPath);
                    }

                    var db = new LiteDatabase(appSettings.LiteDb.ConnectionString);

                    // 配置实体ID映射
                    // Configure entity ID mapping
                    ConfigureLiteDbEntityMapping(db.Mapper);

                    return db;
                });

                // 配置日志数据库（带熔断器的弹性日志仓储）
                ConfigureSqliteLogging(services, appSettings);

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

                        services.AddDbContext<MySqlLogDbContext>(options =>
                            ConfigureMySqlDbContext(options, appSettings.MySql.ConnectionString, serverVersion, appSettings.MySql.ConnectionPool),
                            ServiceLifetime.Scoped);

                        // 如果启用了分片功能，也注册ShardedLogDbContext
                        // Register ShardedLogDbContext if sharding is enabled
                        var shardingEnabled = configuration.GetValue<bool>("AppSettings:Sharding:Enabled");
                        if (shardingEnabled)
                        {
                            services.AddDbContext<ShardedLogDbContext>(options =>
                                ConfigureMySqlDbContext(options, appSettings.MySql.ConnectionString, serverVersion, appSettings.MySql.ConnectionPool),
                                ServiceLifetime.Scoped);
                            logger.Info("分片数据库上下文已注册");
                        }

                        logger.Info("MySQL数据库连接配置成功，使用弹性日志仓储");
                        // 使用带熔断器的弹性日志仓储
                        services.AddScoped<ILogRepository, ResilientLogRepository>();

                        // 注册MySQL表存在性检查器
                        services.AddScoped<ITableExistenceChecker, MySqlTableExistenceChecker>();

                        // 使用MySQL监控告警仓储
                        services.AddScoped<IMonitoringAlertRepository, MySqlMonitoringAlertRepository>();
                        logger.Info("使用MySQL监控告警仓储");

                        // 使用MySQL配置审计日志仓储
                        services.AddScoped<IConfigurationAuditLogRepository, MySqlConfigurationAuditLogRepository>();
                        logger.Info("使用MySQL配置审计日志仓储");
                        
                        // 使用MySQL包裹信息仓储
                        services.AddScoped<IParcelInfoRepository, ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql.MySqlParcelInfoRepository>();
                        services.AddScoped<IParcelLifecycleNodeRepository, ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql.MySqlParcelLifecycleNodeRepository>();
                        logger.Info("使用MySQL包裹信息仓储");
                        
                        // 注册包裹信息应用服务（Scoped）
                        services.AddScoped<IParcelInfoAppService, ParcelInfoAppService>();
                        logger.Info("已注册包裹信息应用服务（Scoped）");
                    }
                    catch (Exception ex)
                    {
                        // MySQL配置失败，使用SQLite仓储
                        logger.Warn(ex, "MySQL数据库连接配置失败，降级使用SQLite仓储: {Message}", ex.Message);
                        services.AddScoped<ILogRepository, SqliteLogRepository>();

                        // 降级使用SQLite监控告警仓储
                        services.AddScoped<IMonitoringAlertRepository, SqliteMonitoringAlertRepository>();
                        logger.Info("降级使用SQLite监控告警仓储");

                        // 降级使用SQLite配置审计日志仓储
                        services.AddScoped<IConfigurationAuditLogRepository, SqliteConfigurationAuditLogRepository>();
                        logger.Info("降级使用SQLite配置审计日志仓储");
                        
                        // 降级使用SQLite包裹信息仓储
                        services.AddScoped<IParcelInfoRepository, ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite.SqliteParcelInfoRepository>();
                        services.AddScoped<IParcelLifecycleNodeRepository, ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite.SqliteParcelLifecycleNodeRepository>();
                        logger.Info("降级使用SQLite包裹信息仓储");
                        
                        // 注册包裹信息应用服务（Scoped）
                        services.AddScoped<IParcelInfoAppService, ParcelInfoAppService>();
                        logger.Info("已注册包裹信息应用服务（Scoped）");
                    }
                }
                else
                {
                    logger.Info("MySQL未启用或连接字符串为空，使用SQLite仓储");
                    // MySQL未启用，直接使用SQLite仓储
                    services.AddScoped<ILogRepository, SqliteLogRepository>();

                    // 使用SQLite监控告警仓储
                    services.AddScoped<IMonitoringAlertRepository, SqliteMonitoringAlertRepository>();
                    logger.Info("使用SQLite监控告警仓储");

                    // 使用SQLite配置审计日志仓储
                    services.AddScoped<IConfigurationAuditLogRepository, SqliteConfigurationAuditLogRepository>();
                    logger.Info("使用SQLite配置审计日志仓储");
                    
                    // 使用SQLite包裹信息仓储
                    services.AddScoped<IParcelInfoRepository, ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite.SqliteParcelInfoRepository>();
                    services.AddScoped<IParcelLifecycleNodeRepository, ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite.SqliteParcelLifecycleNodeRepository>();
                    logger.Info("使用SQLite包裹信息仓储");
                    
                    // 注册包裹信息应用服务（Scoped）
                    services.AddScoped<IParcelInfoAppService, ParcelInfoAppService>();
                    logger.Info("已注册包裹信息应用服务（Scoped）");
                }

                // 配置HttpClient用于WCS API
                // 注册所有API适配器实现
                // 配置从LiteDB加载，HttpClient使用默认值
                // Configuration loaded from LiteDB, HttpClient uses default values
                services.AddHttpClient<WcsApiClient>(client =>
                {
                    // 提供默认的BaseAddress和Timeout
                    // API客户端将从LiteDB加载实际配置
                    // Provide default BaseAddress and Timeout
                    // API client will load actual config from LiteDB
                    client.BaseAddress = new Uri("http://localhost");
                    client.Timeout = TimeSpan.FromSeconds(30);
                })
                .AddTypedClient<WcsApiClient>((client, sp) =>
                {
                    var loggerWcs = sp.GetRequiredService<ILogger<WcsApiClient>>();
                    var clock = sp.GetRequiredService<ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock>();
                    var configRepo = sp.GetRequiredService<IWcsApiConfigRepository>();
                    return new WcsApiClient(
                        client,
                        loggerWcs,
                        clock,
                        configRepo);
                });

                // 注册旺店通WMS API适配器
                // 配置从LiteDB加载，HttpClient使用默认值
                // Configuration loaded from LiteDB, HttpClient uses default values
                services.AddHttpClient<WdtWmsApiClient>(client =>
                {
                    // 提供默认的BaseAddress和Timeout
                    // API客户端将从LiteDB加载实际配置
                    // Provide default BaseAddress and Timeout
                    // API client will load actual config from LiteDB
                    client.BaseAddress = new Uri("http://localhost");
                    client.Timeout = TimeSpan.FromSeconds(30);
                })
                .AddTypedClient<WdtWmsApiClient>((client, sp) =>
                {
                    var loggerWdt = sp.GetRequiredService<ILogger<WdtWmsApiClient>>();
                    var clock = sp.GetRequiredService<ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock>();
                    var configRepo = sp.GetRequiredService<IWdtWmsConfigRepository>();
                    return new WdtWmsApiClient(
                        client,
                        loggerWdt,
                        clock,
                        configRepo);
                });

                // 注册聚水潭ERP API适配器
                // 配置从LiteDB加载，HttpClient使用默认值
                // Configuration loaded from LiteDB, HttpClient uses default values
                services.AddHttpClient<JushuitanErpApiClient>(client =>
                {
                    // 提供默认的BaseAddress和Timeout
                    // API客户端将从LiteDB加载实际配置
                    // Provide default BaseAddress and Timeout
                    // API client will load actual config from LiteDB
                    client.BaseAddress = new Uri("http://localhost");
                    client.Timeout = TimeSpan.FromSeconds(30);
                })
                .AddTypedClient<JushuitanErpApiClient>((client, sp) =>
                {
                    var loggerJst = sp.GetRequiredService<ILogger<JushuitanErpApiClient>>();
                    var clock = sp.GetRequiredService<ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock>();
                    var configRepo = sp.GetRequiredService<IJushuitanErpConfigRepository>();
                    return new JushuitanErpApiClient(
                        client,
                        loggerJst,
                        clock,
                        configRepo);
                });

                // 注册邮政处理中心API适配器
                // 配置从LiteDB加载，HttpClient使用默认值
                // Configuration loaded from LiteDB, HttpClient uses default values
                services.AddHttpClient<PostProcessingCenterApiClient>(client =>
                {
                    // 提供默认的BaseAddress和Timeout
                    // API客户端将从LiteDB加载实际配置
                    // Provide default BaseAddress and Timeout
                    // API client will load actual config from LiteDB
                    client.BaseAddress = new Uri("http://localhost");
                    client.Timeout = TimeSpan.FromSeconds(30);
                })
                .ConfigurePrimaryHttpMessageHandler(() => HttpClientConfigurationHelper.CreatePostalApiHandler());

                // 注册邮政分揽投机构API适配器
                // 配置从LiteDB加载，HttpClient使用默认值
                // Configuration loaded from LiteDB, HttpClient uses default values
                services.AddHttpClient<PostCollectionApiClient>(client =>
                {
                    // 提供默认的BaseAddress和Timeout
                    // API客户端将从LiteDB加载实际配置
                    // Provide default BaseAddress and Timeout
                    // API client will load actual config from LiteDB
                    client.BaseAddress = new Uri("http://localhost");
                    client.Timeout = TimeSpan.FromSeconds(30);
                })
                .ConfigurePrimaryHttpMessageHandler(() => HttpClientConfigurationHelper.CreatePostalApiHandler());

                // 注册自动应答模式服务
                // Register auto-response mode service
                services.AddSingleton<IAutoResponseModeService, ZakYip.Sorting.RuleEngine.Infrastructure.Services.AutoResponseModeService>();

                // 注册所有WCS API适配器到DI容器
                // Register all WCS API adapters to DI container
                services.AddSingleton<IWcsApiAdapter>(sp => sp.GetRequiredService<WcsApiClient>());
                services.AddSingleton<IWcsApiAdapter>(sp => sp.GetRequiredService<WdtWmsApiClient>());
                services.AddSingleton<IWcsApiAdapter>(sp => sp.GetRequiredService<JushuitanErpApiClient>());
                services.AddSingleton<IWcsApiAdapter>(sp => sp.GetRequiredService<PostProcessingCenterApiClient>());
                services.AddSingleton<IWcsApiAdapter>(sp => sp.GetRequiredService<PostCollectionApiClient>());

                // 注册模拟适配器（用于自动应答模式）
                // Register mock adapter (for auto-response mode)
                services.AddSingleton<MockWcsApiAdapter>();
                services.AddSingleton<IWcsApiAdapter>(sp => sp.GetRequiredService<MockWcsApiAdapter>());

                // 注册适配器工厂 - 根据配置和自动应答模式选择激活的适配器
                // Register adapter factory - selects active adapter based on configuration and auto-response mode
                services.AddSingleton<IWcsApiAdapterFactory>(sp =>
                {
                    var adapters = sp.GetServices<IWcsApiAdapter>();
                    var autoResponseModeService = sp.GetRequiredService<IAutoResponseModeService>();
                    var loggerFactory = sp.GetRequiredService<ILogger<WcsApiAdapterFactory>>();
                    return new WcsApiAdapterFactory(adapters, appSettings.ActiveApiAdapter, autoResponseModeService, loggerFactory);
                });

                // 注册仓储（数据库访问层保持Scoped）
                // Register repositories (keep database access layer as Scoped)
                services.AddScoped<IRuleRepository, LiteDbRuleRepository>();
                services.AddScoped<IChuteRepository, LiteDbChuteRepository>();
                services.AddScoped<IPerformanceMetricRepository, LiteDbPerformanceMetricRepository>();
                // IMonitoringAlertRepository 现在根据数据库配置在上面注册（MySQL或SQLite）
                // IMonitoringAlertRepository is now registered above based on database configuration (MySQL or SQLite)
                services.AddScoped<IApiCommunicationLogRepository, ApiCommunicationLogRepository>();
                services.AddScoped<ICommunicationLogRepository, ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.CommunicationLogs.CommunicationLogRepository>();

                // 注册DWS相关仓储
                // Register DWS-related repositories
                services.AddScoped<IDwsConfigRepository, LiteDbDwsConfigRepository>();
                services.AddScoped<IDwsDataTemplateRepository, LiteDbDwsDataTemplateRepository>();
                services.AddScoped<IDwsTimeoutConfigRepository, LiteDbDwsTimeoutConfigRepository>();

                // 注册分拣机配置仓储
                // Register Sorter configuration repository
                services.AddScoped<ISorterConfigRepository, LiteDbSorterConfigRepository>();

                // 注册WCS API配置仓储
                // Register WCS API configuration repository
                services.AddScoped<IWcsApiConfigRepository, LiteDbWcsApiConfigRepository>();

                // 注册邮政API配置仓储
                // Register Postal API configuration repositories
                services.AddScoped<IPostCollectionConfigRepository, LiteDbPostCollectionConfigRepository>();
                services.AddScoped<IPostProcessingCenterConfigRepository, LiteDbPostProcessingCenterConfigRepository>();

                // 注册ERP API配置仓储
                // Register ERP API configuration repositories
                services.AddScoped<IJushuitanErpConfigRepository, LiteDbJushuitanErpConfigRepository>();
                services.AddScoped<IWdtWmsConfigRepository, LiteDbWdtWmsConfigRepository>();
                services.AddScoped<IWdtErpFlagshipConfigRepository, LiteDbWdtErpFlagshipConfigRepository>();

                // 注册DWS数据解析器
                // Register DWS data parser
                services.AddSingleton<IDwsDataParser, DwsDataParser>();

                // 添加内存缓存（带可配置的绝对过期和滑动过期）
                // 从配置读取缓存大小限制（以条目数为单位），如果未配置则使用默认值
                var cacheSizeLimit = configuration.GetValue<long?>("Cache:SizeLimit") ?? 1024;
                services.AddMemoryCache(options =>
                {
                    options.SizeLimit = cacheSizeLimit; // 设置缓存大小限制
                    options.CompactionPercentage = 0.25; // 压缩百分比
                });

                // 注册系统时钟（单例模式）
                // Register system clock (Singleton mode)
                services.AddSingleton<ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock, ZakYip.Sorting.RuleEngine.Infrastructure.Services.SystemClock>();

                // 注册WCS API通信日志后台服务（Singleton + HostedService）
                // Register WCS API log background service (Singleton + HostedService)
                services.AddSingleton<WcsApiLogBackgroundService>();
                services.AddHostedService(sp => sp.GetRequiredService<WcsApiLogBackgroundService>());

                // 注册API请求日志后台服务（Singleton + HostedService）
                // Register API request log background service (Singleton + HostedService)
                var useMySql = appSettings.MySql.Enabled && !string.IsNullOrEmpty(appSettings.MySql.ConnectionString);
                services.AddSingleton(sp => new ApiRequestLogBackgroundService(
                    sp.GetRequiredService<IServiceScopeFactory>(),
                    sp.GetRequiredService<ILogger<ApiRequestLogBackgroundService>>(),
                    useMySql));
                services.AddHostedService(sp => sp.GetRequiredService<ApiRequestLogBackgroundService>());

                // 注册应用服务（单例模式，除数据库外）
                // Register application services (Singleton mode, except database)
                services.AddSingleton<PerformanceMetricService>();
                services.AddSingleton<IRuleEngineService, RuleEngineService>();
                services.AddSingleton<RuleValidationService>();
                services.AddScoped<IDataAnalysisService, ZakYip.Sorting.RuleEngine.Infrastructure.Services.DataAnalysisService>();
                services.AddSingleton<IMonitoringService, ZakYip.Sorting.RuleEngine.Infrastructure.Services.MonitoringService>();

                // 注册配置热更新服务（单例）
                // Register configuration hot-reload service (Singleton)
                services.AddSingleton<ConfigCacheService>();
                services.AddSingleton<ParcelCacheService>();
                services.AddSingleton<IConfigReloadService, ConfigReloadService>();

                // 注册适配器管理器（单例）
                // Register adapter managers (Singleton)
                services.AddSingleton<IDwsAdapterManager, DwsAdapterManager>();
                services.AddSingleton<ISorterAdapterManager, SorterAdapterManager>();

                // 注册包裹活动追踪器（用于空闲检测）
                services.AddSingleton<IParcelActivityTracker, ZakYip.Sorting.RuleEngine.Infrastructure.Services.ParcelActivityTracker>();

                // 注册配置缓存服务
                services.AddSingleton<ZakYip.Sorting.RuleEngine.Infrastructure.Services.ConfigurationCacheService>();

                // 注册监控Hub通知器
                services.AddSingleton<ZakYip.Sorting.RuleEngine.Service.Hubs.MonitoringHubNotifier>();

                // 注册事件驱动服务
                services.AddSingleton<ParcelOrchestrationService>();
                services.AddMediatR(cfg =>
                {
                    cfg.RegisterServicesFromAssembly(typeof(ZakYip.Sorting.RuleEngine.Application.Services.RuleEngineService).Assembly);
                });

                // 注册后台服务
                // 适配器连接服务必须在配置缓存预加载之后启动
                // Adapter connection service must start after configuration cache preload
                services.AddHostedService<ZakYip.Sorting.RuleEngine.Infrastructure.BackgroundServices.ConfigurationCachePreloadService>();
                services.AddHostedService<ZakYip.Sorting.RuleEngine.Infrastructure.BackgroundServices.AdapterConnectionService>();
                services.AddHostedService<ParcelQueueProcessorService>();
                services.AddHostedService<DwsTimeoutCheckerService>();
                services.AddHostedService<DataCleanupService>();
                services.AddHostedService<DataArchiveService>();
                services.AddHostedService<MySqlAutoTuningService>();
                services.AddHostedService<ShardingTableManagementService>();
                services.AddHostedService<LogFileCleanupService>();
                services.AddHostedService<ZakYip.Sorting.RuleEngine.Infrastructure.BackgroundServices.MonitoringAlertService>();

                // 添加健康检查
                services.AddHealthChecks()
                    .AddCheck<ZakYip.Sorting.RuleEngine.Service.HealthChecks.MySqlHealthCheck>("mysql", tags: new[] { "database", "mysql" })
                    .AddCheck<ZakYip.Sorting.RuleEngine.Service.HealthChecks.SqliteHealthCheck>("sqlite", tags: new[] { "database", "sqlite" })
                    .AddCheck<ZakYip.Sorting.RuleEngine.Service.HealthChecks.MemoryCacheHealthCheck>("memory_cache", tags: new[] { "cache" })
                    .AddCheck<ZakYip.Sorting.RuleEngine.Service.HealthChecks.WcsApiHealthCheck>("wcs_api", tags: new[] { "external", "api" })
                    .AddCheck<ZakYip.Sorting.RuleEngine.Service.HealthChecks.DwsConnectionHealthCheck>("dws_connection", tags: new[] { "dws", "connection" })
                    .AddCheck<ZakYip.Sorting.RuleEngine.Service.HealthChecks.RulesConfigHealthCheck>("rules_config", tags: new[] { "configuration", "rules" })
                    .AddCheck<ZakYip.Sorting.RuleEngine.Service.HealthChecks.ThirdPartyApiConfigHealthCheck>("third_party_api_config", tags: new[] { "configuration", "api" });

                // 添加控制器和API服务
                services.AddControllers(options =>
                {
                    // 添加全局模型验证过滤器
                    options.Filters.Add<ZakYip.Sorting.RuleEngine.Infrastructure.Filters.ModelValidationFilter>();
                })
                    .AddNewtonsoftJson(options =>
                    {
                        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                        options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                        options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
                        // 配置枚举序列化为字符串类型，避免在 Swagger 中显示为魔法数字
                        // Configure enum serialization as string type to avoid displaying as magic numbers in Swagger
                        options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                    });
                services.AddEndpointsApiExplorer();

                // 添加SignalR服务
                services.AddSignalR(options =>
                {
#if DEBUG
                    options.EnableDetailedErrors = true; // 仅开发环境启用详细错误，帮助调试
#else
                    options.EnableDetailedErrors = false; // 生产环境禁用详细错误，防止信息泄露
#endif
                    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
                    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
                });

                if (appSettings.MiniApi.EnableSwagger)
                {
                    services.AddSwaggerGen(c =>
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
                services.AddCors(options =>
                {
                    options.AddDefaultPolicy(policy =>
                    {
                        policy.AllowAnyOrigin()
                              .AllowAnyMethod()
                              .AllowAnyHeader();
                    });
                });
            });

            webBuilder.Configure((context, app) =>
            {
                var appSettings = context.Configuration.GetSection("AppSettings").Get<AppSettings>()
                    ?? new AppSettings();

                // 初始化 SystemClockProvider 用于静态上下文
                // Initialize SystemClockProvider for static contexts using DI-registered instance
                var clockForProvider = app.ApplicationServices.GetRequiredService<ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock>();
                ZakYip.Sorting.RuleEngine.Domain.Services.SystemClockProvider.Initialize(
                    () => clockForProvider.LocalNow,
                    () => clockForProvider.UtcNow
                );

                // 验证 SystemClockProvider 已初始化
                // Validate SystemClockProvider is initialized
                if (!ZakYip.Sorting.RuleEngine.Domain.Services.SystemClockProvider.IsInitialized)
                {
                    throw new InvalidOperationException("SystemClockProvider initialization failed!");
                }

                // 初始化数据库
                InitializeDatabases(app.ApplicationServices, appSettings);

                // 配置HTTP管道

                // 添加API请求日志中间件
                app.UseMiddleware<ZakYip.Sorting.RuleEngine.Infrastructure.Middleware.ApiRequestLoggingMiddleware>();

                if (context.HostingEnvironment.IsDevelopment() || appSettings.MiniApi.EnableSwagger)
                {
                    app.UseSwagger();
                    app.UseSwaggerUI(c =>
                    {
                        c.SwaggerEndpoint("/swagger/v1/swagger.json", "分拣规则引擎 API v1");
                    });
                }

                app.UseCors();
                app.UseRouting();

                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();

                    // 映射SignalR Hub端点
                    endpoints.MapHub<ZakYip.Sorting.RuleEngine.Service.Hubs.SortingHub>("/hubs/sorting");
                    endpoints.MapHub<ZakYip.Sorting.RuleEngine.Service.Hubs.DwsHub>("/hubs/dws");
                    endpoints.MapHub<ZakYip.Sorting.RuleEngine.Service.Hubs.MonitoringHub>("/hubs/monitoring");

                    // 健康检查端点 - 简单版本
                    endpoints.MapGet("/health", () =>
                    {
                        var clock = app.ApplicationServices.GetRequiredService<ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock>();
                        return Results.Ok(new
                        {
                            status = "healthy",
                            timestamp = clock.LocalNow
                        });
                    })
                    .WithName("HealthCheck");

                    // 详细健康检查端点 - 包含所有组件状态
                    endpoints.MapHealthChecks("/health/detail", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
                    {
                        ResponseWriter = async (context, report) =>
                        {
                            var clock = app.ApplicationServices.GetRequiredService<ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock>();
                            context.Response.ContentType = "application/json";
                            var result = new
                            {
                                status = report.Status.ToString(),
                                timestamp = clock.LocalNow,
                                duration = report.TotalDuration.TotalMilliseconds,
                                checks = report.Entries.Select(e => new
                                {
                                    name = e.Key,
                                    status = e.Value.Status.ToString(),
                                    description = e.Value.Description,
                                    duration = e.Value.Duration.TotalMilliseconds,
                                    exception = e.Value.Exception?.Message,
                                    tags = e.Value.Tags
                                })
                            };
                            await context.Response.WriteAsJsonAsync(result).ConfigureAwait(false);
                        }
                    });

                    // 按标签过滤的健康检查端点
                    endpoints.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
                    {
                        Predicate = check => check.Tags.Contains("database")
                    });

                    endpoints.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
                    {
                        Predicate = _ => false // 仅检查服务是否运行
                    });
                });
            });

            webBuilder.UseKestrel(options =>
            {
                options.AddServerHeader = false; // 不发送Server头以提高安全性
                options.Limits.MaxConcurrentConnections = 1000;
                options.Limits.MaxConcurrentUpgradedConnections = 1000;
                options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB
                options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
                options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
            });
        })
        .Build();

#if DEBUG
    logger.Info("控制台模式运行（DEBUG模式）");
#else
    logger.Info("Windows服务模式已启用");
#endif

    // 在Windows平台上检查并配置防火墙（异步执行，不阻塞主线程）
    if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
    {
        // 启动防火墙和网络配置任务（不阻塞主线程）
        _ = Task.Run(async () =>
        {
            try
            {
                logger.Info("检测到Windows平台，开始配置防火墙和端口规则 | Windows platform detected, starting firewall and port configuration");

                // 获取配置
                var configuration = host.Services.GetRequiredService<IConfiguration>();
                var appSettings = configuration.GetSection("AppSettings").Get<ZakYip.Sorting.RuleEngine.Service.Configuration.AppSettings>();

                if (appSettings?.MiniApi?.Urls != null && appSettings.MiniApi.Urls.Length > 0)
                {
                    // 提取需要的端口
                    var ports = ZakYip.Sorting.RuleEngine.Infrastructure.Services.WindowsFirewallManager.ExtractPortsFromUrls(appSettings.MiniApi.Urls);

                    if (ports.Any())
                    {
                        logger.Info("检测到需要开放的端口: {Ports} | Detected ports to open: {Ports}", string.Join(", ", ports));

                        // 创建防火墙管理器并配置
                        var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
                        var firewallLogger = loggerFactory.CreateLogger<ZakYip.Sorting.RuleEngine.Infrastructure.Services.WindowsFirewallManager>();
                        var firewallManager = new ZakYip.Sorting.RuleEngine.Infrastructure.Services.WindowsFirewallManager(firewallLogger);

                        var success = await firewallManager.EnsureFirewallConfiguredAsync(ports).ConfigureAwait(false);
                        if (success)
                        {
                            logger.Info("防火墙配置完成 | Firewall configuration completed");
                        }
                        else
                        {
                            logger.Warn("防火墙配置未完全成功，请检查日志 | Firewall configuration not fully successful, please check logs");
                        }

                        // 配置网络适配器（启用巨帧和最大传输缓存）
                        logger.Info("开始配置网络适配器 | Starting network adapter configuration");
                        var adapterSuccess = await firewallManager.ConfigureNetworkAdaptersAsync().ConfigureAwait(false);
                        if (adapterSuccess)
                        {
                            logger.Info("网络适配器配置完成 | Network adapter configuration completed");
                        }
                        else
                        {
                            logger.Warn("网络适配器配置未完全成功，请检查日志 | Network adapter configuration not fully successful, please check logs");
                        }
                    }
                    else
                    {
                        logger.Warn("未能从配置的URL中提取端口信息 | Could not extract port information from configured URLs");
                    }
                }
                else
                {
                    logger.Warn("未配置MiniApi.Urls，跳过防火墙端口配置 | MiniApi.Urls not configured, skipping firewall port configuration");
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "配置防火墙和网络适配器时发生错误，程序将继续运行 | Error occurred while configuring firewall and network adapters, program will continue running");
            }
        });
    }

    await host.RunAsync().ConfigureAwait(false);
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

/// <summary>
/// 配置LiteDB实体ID映射
/// Configure LiteDB entity ID mapping
/// </summary>
/// <param name="mapper">LiteDB的BsonMapper实例 / LiteDB BsonMapper instance</param>
static void ConfigureLiteDbEntityMapping(BsonMapper mapper)
{
    // 配置实体ID映射：将业务ID字段映射为LiteDB的_id字段
    // Configure entity ID mapping: Map business ID fields to LiteDB's _id field
    // 这样可以确保通过业务ID（如ConfigId）进行查询、更新和删除操作
    // This ensures queries, updates, and deletes work with business IDs (like ConfigId)

    // 单例配置实体 - 使用固定ID
    // Singleton configuration entities - Use fixed IDs
    mapper.Entity<ZakYip.Sorting.RuleEngine.Domain.Entities.DwsConfig>()
        .Id(x => x.ConfigId);

    mapper.Entity<ZakYip.Sorting.RuleEngine.Domain.Entities.DwsDataTemplate>()
        .Id(x => x.TemplateId);

    mapper.Entity<ZakYip.Sorting.RuleEngine.Domain.Entities.SorterConfig>()
        .Id(x => x.ConfigId);
    
    mapper.Entity<ZakYip.Sorting.RuleEngine.Domain.Entities.WcsApiConfig>()
        .Id(x => x.ConfigId);

    // 其他实体 - 使用自动生成或业务ID
    // Other entities - Use auto-generated or business IDs
    mapper.Entity<ZakYip.Sorting.RuleEngine.Domain.Entities.SortingRule>()
        .Id(x => x.RuleId);

    mapper.Entity<ZakYip.Sorting.RuleEngine.Domain.Entities.MonitoringAlert>()
        .Id(x => x.AlertId);

    mapper.Entity<ZakYip.Sorting.RuleEngine.Domain.Entities.PerformanceMetric>()
        .Id(x => x.MetricId);

    // 注意：Chute 使用 ChuteId (long) 作为主键，这是自增ID
    // Note: Chute uses ChuteId (long) as primary key, which is auto-increment
    mapper.Entity<ZakYip.Sorting.RuleEngine.Domain.Entities.Chute>()
        .Id(x => x.ChuteId, true); // true表示自增 / true means auto-increment
}

// <summary>
// 配置MySQL数据库上下文
// Configure MySQL database context
// </summary>
static void ConfigureMySqlDbContext(DbContextOptionsBuilder options, string connectionString, ServerVersion serverVersion, ConnectionPoolSettings poolSettings)
{
    // 应用连接池配置到连接字符串
    // Apply connection pool settings to connection string
    var builder = new MySqlConnectionStringBuilder(connectionString)
    {
        Pooling = poolSettings.Pooling,
        MinimumPoolSize = (uint)poolSettings.MinPoolSize,
        MaximumPoolSize = (uint)poolSettings.MaxPoolSize,
        ConnectionLifeTime = (uint)poolSettings.ConnectionLifetimeSeconds,
        ConnectionIdleTimeout = (uint)poolSettings.ConnectionIdleTimeoutSeconds,
        ConnectionTimeout = (uint)poolSettings.ConnectionTimeoutSeconds
    };

    options.UseMySql(builder.ConnectionString, serverVersion);

    // 配置安全日志选项 / Configure secure logging options
    DatabaseConfigurationHelper.ConfigureSecureLogging(options);
}

// <summary>
// 配置SQLite日志
// </summary>
static void ConfigureSqliteLogging(IServiceCollection services, AppSettings appSettings)
{
    var dbPath = Path.GetDirectoryName(appSettings.Sqlite.ConnectionString.Replace("Data Source=", "").Split(';')[0]);
    if (!string.IsNullOrEmpty(dbPath) && !Directory.Exists(dbPath))
    {
        Directory.CreateDirectory(dbPath);
    }

    services.AddDbContext<SqliteLogDbContext>(options =>
    {
        options.UseSqlite(appSettings.Sqlite.ConnectionString);

        // 配置安全日志选项 / Configure secure logging options
        DatabaseConfigurationHelper.ConfigureSecureLogging(options);
    });

    services.AddScoped<ILogRepository, SqliteLogRepository>();
}

// <summary>
// 确保MySQL数据库存在，如果不存在则创建
// </summary>
static bool EnsureMySqlDatabaseExists(string connectionString, NLog.Logger logger)
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

// <summary>
// 初始化数据库并自动应用迁移
// </summary>
static void InitializeDatabases(IServiceProvider services, AppSettings appSettings)
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
/// HTTP客户端配置辅助类 - 提取重复的HTTP处理器配置
/// HTTP Client Configuration Helper - Extracts duplicate HTTP handler configuration
/// </summary>
internal static class HttpClientConfigurationHelper
{
    /// <summary>
    /// 创建配置好的SocketsHttpHandler，用于邮政API等SOAP服务
    /// Creates a configured SocketsHttpHandler for postal APIs and other SOAP services
    /// </summary>
    public static SocketsHttpHandler CreatePostalApiHandler()
    {
        return new SocketsHttpHandler
        {
            // Enable all SSL/TLS protocols to maximize compatibility
            SslOptions = new System.Net.Security.SslClientAuthenticationOptions
            {
                // Allow all SSL/TLS versions for maximum compatibility with postal API servers
                EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 |
                                     System.Security.Authentication.SslProtocols.Tls13,
                // Bypass certificate validation (as configured in original code)
                RemoteCertificateValidationCallback = (sender, certificate, chain, errors) => true
            },
            // Set connection lifetime to avoid connection reuse issues
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            // Limit connection idle time to prevent stale connections
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            // Disable HTTP/2 to ensure HTTP/1.1 is used for better SOAP compatibility
            // Some SOAP servers may not support HTTP/2 properly
            MaxConnectionsPerServer = 10
        };
    }
}

/// <summary>
/// 数据库配置辅助类 - 提取重复的数据库日志配置
/// Database Configuration Helper - Extracts duplicate database logging configuration
/// </summary>
internal static class DatabaseConfigurationHelper
{
    /// <summary>
    /// 配置数据库上下文的安全日志选项
    /// Configure secure logging options for database context
    /// </summary>
    /// <param name="options">数据库上下文选项 / Database context options</param>
    public static void ConfigureSecureLogging(DbContextOptionsBuilder options)
    {
        // 生产环境安全配置：禁止敏感数据日志和详细错误
        // 仅在DEBUG模式下启用详细错误，帮助开发调试
#if DEBUG
        options.EnableDetailedErrors();
        options.EnableSensitiveDataLogging(); // 仅开发环境显示SQL参数
#else
        // 生产环境：禁用敏感数据日志，防止SQL语句和参数泄露
        options.EnableSensitiveDataLogging(false);
#endif

        // 配置日志：仅记录警告及以上级别，过滤SQL语句日志
        options.LogTo(
            message => System.Diagnostics.Debug.WriteLine(message),
            Microsoft.Extensions.Logging.LogLevel.Warning);
    }
}
