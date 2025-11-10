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
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.JushuitanErp;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.WdtWms;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.PostCollection;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.PostProcessingCenter;
using ZakYip.Sorting.RuleEngine.Infrastructure.BackgroundServices;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.ApiCommunicationLogs;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.LiteDb;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite;
using ZakYip.Sorting.RuleEngine.Infrastructure.Sharding;
using ZakYip.Sorting.RuleEngine.Service.Configuration;

// 配置NLog
var logger = LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();

try
{
    logger.Info("应用程序启动中...");

    var host = Host.CreateDefaultBuilder(args)
#if !DEBUG
        // 仅在Release模式下配置Windows服务
        .UseWindowsService()
#endif
        .ConfigureLogging(logging =>
        {
            logging.ClearProviders();
        })
        .UseNLog()
        .ConfigureWebHostDefaults(webBuilder =>
        {
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
                services.AddSingleton<ILiteDatabase>(sp =>
                {
                    var dbPath = Path.GetDirectoryName(appSettings.LiteDb.ConnectionString.Replace("Filename=", "").Split(';')[0]);
                    if (!string.IsNullOrEmpty(dbPath) && !Directory.Exists(dbPath))
                    {
                        Directory.CreateDirectory(dbPath);
                    }
                    return new LiteDatabase(appSettings.LiteDb.ConnectionString);
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
                            ConfigureMySqlDbContext(options, appSettings.MySql.ConnectionString, serverVersion),
                            ServiceLifetime.Scoped);
                        
                        // 如果启用了分片功能，也注册ShardedLogDbContext
                        // Register ShardedLogDbContext if sharding is enabled
                        var shardingEnabled = configuration.GetValue<bool>("AppSettings:Sharding:Enabled");
                        if (shardingEnabled)
                        {
                            services.AddDbContext<ShardedLogDbContext>(options =>
                                ConfigureMySqlDbContext(options, appSettings.MySql.ConnectionString, serverVersion),
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
                    }
                    catch (Exception ex)
                    {
                        // MySQL配置失败，使用SQLite仓储
                        logger.Warn(ex, "MySQL数据库连接配置失败，降级使用SQLite仓储: {Message}", ex.Message);
                        services.AddScoped<ILogRepository, SqliteLogRepository>();
                        
                        // 降级使用SQLite监控告警仓储
                        services.AddScoped<IMonitoringAlertRepository, SqliteMonitoringAlertRepository>();
                        logger.Info("降级使用SQLite监控告警仓储");
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
                }

                // 配置HttpClient用于WCS API
                // 注册所有API适配器实现
                services.AddHttpClient<WcsApiClient>(client =>
                {
                    client.BaseAddress = new Uri(appSettings.WcsApi.BaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(appSettings.WcsApi.TimeoutSeconds);
                    
                    if (!string.IsNullOrEmpty(appSettings.WcsApi.ApiKey))
                    {
                        client.DefaultRequestHeaders.Add("X-API-Key", appSettings.WcsApi.ApiKey);
                    }
                })
                .ConfigurePrimaryHttpMessageHandler(() =>
                {
                    return new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (m, c, ch, _) => true
                    };
                });

                // 注册旺店通WMS API适配器
                services.AddHttpClient<WdtWmsApiClient>((sp, client) =>
                {
                    client.BaseAddress = new Uri(appSettings.WdtWmsApi.BaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(appSettings.WdtWmsApi.TimeoutSeconds);
                })
                .ConfigurePrimaryHttpMessageHandler(() =>
                {
                    return new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (m, c, ch, _) => true
                    };
                })
                .AddTypedClient<WdtWmsApiClient>((client, sp) =>
                {
                    var loggerWdt = sp.GetRequiredService<ILogger<WdtWmsApiClient>>();
                    return new WdtWmsApiClient(
                        client,
                        loggerWdt,
                        appSettings.WdtWmsApi.AppKey,
                        appSettings.WdtWmsApi.AppSecret);
                });

                // 注册聚水潭ERP API适配器
                services.AddHttpClient<JushuitanErpApiClient>((sp, client) =>
                {
                    client.BaseAddress = new Uri(appSettings.JushuitanErpApi.BaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(appSettings.JushuitanErpApi.TimeoutSeconds);
                })
                .ConfigurePrimaryHttpMessageHandler(() =>
                {
                    return new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (m, c, ch, _) => true
                    };
                })
                .AddTypedClient<JushuitanErpApiClient>((client, sp) =>
                {
                    var loggerJst = sp.GetRequiredService<ILogger<JushuitanErpApiClient>>();
                    return new JushuitanErpApiClient(
                        client,
                        loggerJst,
                        appSettings.JushuitanErpApi.PartnerKey,
                        appSettings.JushuitanErpApi.PartnerSecret,
                        appSettings.JushuitanErpApi.Token);
                });

                // 注册邮政处理中心API适配器
                services.AddHttpClient<PostProcessingCenterApiClient>((sp, client) =>
                {
                    client.BaseAddress = new Uri(appSettings.PostProcessingCenterApi.BaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(appSettings.PostProcessingCenterApi.TimeoutSeconds);
                })
                .ConfigurePrimaryHttpMessageHandler(() =>
                {
                    // Use SocketsHttpHandler for better SSL/TLS control and connection management
                    var handler = new SocketsHttpHandler
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
                    return handler;
                });

                // 注册邮政分揽投机构API适配器
                services.AddHttpClient<PostCollectionApiClient>((sp, client) =>
                {
                    client.BaseAddress = new Uri(appSettings.PostCollectionApi.BaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(appSettings.PostCollectionApi.TimeoutSeconds);
                })
                .ConfigurePrimaryHttpMessageHandler(() =>
                {
                    // Use SocketsHttpHandler for better SSL/TLS control and connection management
                    var handler = new SocketsHttpHandler
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
                    return handler;
                });

                // 注册所有适配器到DI容器
                services.AddSingleton<IWcsApiAdapter>(sp => sp.GetRequiredService<WcsApiClient>());
                services.AddSingleton<IWcsApiAdapter>(sp => sp.GetRequiredService<WdtWmsApiClient>());
                services.AddSingleton<IWcsApiAdapter>(sp => sp.GetRequiredService<JushuitanErpApiClient>());
                services.AddSingleton<IWcsApiAdapter>(sp => sp.GetRequiredService<PostProcessingCenterApiClient>());
                services.AddSingleton<IWcsApiAdapter>(sp => sp.GetRequiredService<PostCollectionApiClient>());

                // 注册适配器工厂 - 根据配置选择唯一激活的适配器
                services.AddSingleton<IWcsApiAdapterFactory>(sp =>
                {
                    var adapters = sp.GetServices<IWcsApiAdapter>();
                    var loggerFactory = sp.GetRequiredService<ILogger<WcsApiAdapterFactory>>();
                    return new WcsApiAdapterFactory(adapters, appSettings.ActiveApiAdapter, loggerFactory);
                });

                // 注册仓储
                services.AddScoped<IRuleRepository, LiteDbRuleRepository>();
                services.AddScoped<IChuteRepository, LiteDbChuteRepository>();
                services.AddScoped<IWcsApiConfigRepository, LiteDbWcsApiConfigRepository>();
                services.AddScoped<IPerformanceMetricRepository, LiteDbPerformanceMetricRepository>();
                // IMonitoringAlertRepository 现在根据数据库配置在上面注册（MySQL或SQLite）
                // IMonitoringAlertRepository is now registered above based on database configuration (MySQL or SQLite)
                services.AddScoped<IApiCommunicationLogRepository, ApiCommunicationLogRepository>();

                // 添加内存缓存（带可配置的绝对过期和滑动过期）
                // 从配置读取缓存大小限制（以条目数为单位），如果未配置则使用默认值
                var cacheSizeLimit = configuration.GetValue<long?>("Cache:SizeLimit") ?? 1024;
                services.AddMemoryCache(options =>
                {
                    options.SizeLimit = cacheSizeLimit; // 设置缓存大小限制
                    options.CompactionPercentage = 0.25; // 压缩百分比
                });

                // 注册应用服务
                services.AddScoped<PerformanceMetricService>();
                services.AddScoped<IRuleEngineService, RuleEngineService>();
                services.AddScoped<IParcelProcessingService, ParcelProcessingService>();
                services.AddScoped<RuleValidationService>();
                services.AddScoped<IDataAnalysisService, ZakYip.Sorting.RuleEngine.Infrastructure.Services.DataAnalysisService>();
                services.AddScoped<IMonitoringService, ZakYip.Sorting.RuleEngine.Infrastructure.Services.MonitoringService>();
                
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
                services.AddHostedService<ParcelQueueProcessorService>();
                services.AddHostedService<DataCleanupService>();
                services.AddHostedService<DataArchiveService>();
                services.AddHostedService<MySqlAutoTuningService>();
                services.AddHostedService<ShardingTableManagementService>();
                services.AddHostedService<LogFileCleanupService>();
                services.AddHostedService<ZakYip.Sorting.RuleEngine.Infrastructure.BackgroundServices.ConfigurationCachePreloadService>();
                services.AddHostedService<ZakYip.Sorting.RuleEngine.Infrastructure.BackgroundServices.MonitoringAlertService>();

                // 添加健康检查
                services.AddHealthChecks()
                    .AddCheck<ZakYip.Sorting.RuleEngine.Service.HealthChecks.MySqlHealthCheck>("mysql", tags: new[] { "database", "mysql" })
                    .AddCheck<ZakYip.Sorting.RuleEngine.Service.HealthChecks.SqliteHealthCheck>("sqlite", tags: new[] { "database", "sqlite" })
                    .AddCheck<ZakYip.Sorting.RuleEngine.Service.HealthChecks.MemoryCacheHealthCheck>("memory_cache", tags: new[] { "cache" })
                    .AddCheck<ZakYip.Sorting.RuleEngine.Service.HealthChecks.WcsApiHealthCheck>("wcs_api", tags: new[] { "external", "api" });

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
                    endpoints.MapGet("/health", () => Results.Ok(new
                    {
                        status = "healthy",
                        timestamp = DateTime.Now
                    }))
                    .WithName("HealthCheck");

                    // 版本信息端点
                    endpoints.MapGet("/version", () => Results.Ok(new
                    {
                        version = "1.12.0",
                        name = "ZakYip.Sorting.RuleEngine.Core",
                        description = "分拣规则引擎核心系统"
                    }))
                    .WithName("Version");
                    
                    // 详细健康检查端点 - 包含所有组件状态
                    endpoints.MapHealthChecks("/health/detail", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
                    {
                        ResponseWriter = async (context, report) =>
                        {
                            context.Response.ContentType = "application/json";
                            var result = new
                            {
                                status = report.Status.ToString(),
                                timestamp = DateTime.Now,
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
                            await context.Response.WriteAsJsonAsync(result);
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

            // 配置监听URL（仅当未通过命令行参数指定时才使用配置文件中的URL）
            // Configure listen URLs (only use config file URLs if not specified via command line)
            webBuilder.ConfigureAppConfiguration((context, config) =>
            {
                var tempConfig = config.Build();
                var appSettings = tempConfig.GetSection("AppSettings").Get<AppSettings>() 
                    ?? new AppSettings();
                var urls = tempConfig["urls"];
                if (string.IsNullOrEmpty(urls) && 
                    appSettings.MiniApi?.Urls != null && 
                    appSettings.MiniApi.Urls.Length > 0)
                {
                    webBuilder.UseUrls(appSettings.MiniApi.Urls);
                }
            });
        })
        .Build();

#if DEBUG
    logger.Info("控制台模式运行（DEBUG模式）");
#else
    logger.Info("Windows服务模式已启用");
#endif

    // 在Windows平台上检查并配置防火墙
    if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
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
                    
                    var success = firewallManager.EnsureFirewallConfigured(ports);
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
                    var adapterSuccess = firewallManager.ConfigureNetworkAdapters();
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
    }

    host.Run();
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

// <summary>
// 配置MySQL数据库上下文
// Configure MySQL database context
// </summary>
static void ConfigureMySqlDbContext(DbContextOptionsBuilder options, string connectionString, ServerVersion serverVersion)
{
    options.UseMySql(connectionString, serverVersion);
    
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
        
        // 生产环境安全配置：禁止敏感数据日志和详细错误
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
