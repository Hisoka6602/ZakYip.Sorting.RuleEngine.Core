using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Application.Options;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.Communication.Clients;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Communication;

/// <summary>
/// 下游通信工厂实现
/// Downstream communication factory implementation
/// </summary>
/// <remarks>
/// 根据配置动态创建 Server 或 Client 模式的下游通信实例
/// Dynamically creates Server or Client mode downstream communication instances based on configuration
/// </remarks>
public sealed class DownstreamCommunicationFactory : IDownstreamCommunicationFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ISystemClock _systemClock;

    /// <summary>
    /// 初始化下游通信工厂
    /// Initialize downstream communication factory
    /// </summary>
    public DownstreamCommunicationFactory(
        ILoggerFactory loggerFactory,
        ISystemClock systemClock)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _systemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
    }

    /// <summary>
    /// 根据配置创建下游通信实例
    /// Create downstream communication instance based on configuration
    /// </summary>
    public IDownstreamCommunication Create(SorterConfig? config)
    {
        if (config == null || !config.IsEnabled)
        {
            var logger = _loggerFactory.CreateLogger<DownstreamCommunicationFactory>();
            logger.LogInformation(
                "Sorter配置不存在或已禁用，使用空对象实现 / Sorter config does not exist or is disabled, using null object implementation");
            return new NullDownstreamCommunication();
        }

        var mode = config.ConnectionMode.ToUpperInvariant();

        if (mode == "SERVER")
        {
            // Server模式：RuleEngine作为服务器
            // Server mode: RuleEngine acts as server
            var serverLogger = _loggerFactory.CreateLogger<DownstreamTcpJsonServer>();
            var server = new DownstreamTcpJsonServer(
                serverLogger,
                _systemClock,
                config.Host,
                config.Port);

            var factoryLogger = _loggerFactory.CreateLogger<DownstreamCommunicationFactory>();
            factoryLogger.LogInformation(
                "已创建下游通信 Server: Host={Host}, Port={Port}",
                config.Host, config.Port);
            return server;
        }
        else // "CLIENT"
        {
            // Client模式：RuleEngine作为客户端
            // Client mode: RuleEngine acts as client
            var clientLogger = _loggerFactory.CreateLogger<TouchSocketTcpDownstreamClient>();
            var connectionOptions = new ConnectionOptions
            {
                TcpServer = $"{config.Host}:{config.Port}",
                TimeoutMs = config.TimeoutSeconds * 1000,
                RetryCount = 3,
                RetryDelayMs = config.ReconnectIntervalSeconds * 1000
            };
            var client = new TouchSocketTcpDownstreamClient(
                clientLogger,
                connectionOptions,
                _systemClock);

            var factoryLogger = _loggerFactory.CreateLogger<DownstreamCommunicationFactory>();
            factoryLogger.LogInformation(
                "已创建下游通信 Client: Host={Host}, Port={Port}",
                config.Host, config.Port);
            return client;
        }
    }
}
