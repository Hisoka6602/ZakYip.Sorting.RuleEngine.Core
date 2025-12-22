using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using TouchSocket.Core;
using TouchSocket.Sockets;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Downstream;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Adapters.Sorter;

/// <summary>
/// 基于TouchSocket的分拣机TCP适配器（JSON协议）
/// 支持自动重连和高性能消息发送
/// 兼容 ZakYip.WheelDiverterSorter 通信协议
/// </summary>
/// <remarks>
/// 协议：发送JSON格式的 ChuteAssignmentNotification
/// Compatible with: ZakYip.WheelDiverterSorter.Communication.Models
/// </remarks>
public class TouchSocketSorterAdapter : ISorterAdapter, IDisposable
{
    private readonly ILogger<TouchSocketSorterAdapter> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ISystemClock _clock;
    private readonly string _host;
    private readonly int _port;
    private TcpClient? _tcpClient;
    private readonly object _lockObj = new();
    private readonly int _reconnectIntervalMs;
    private readonly int _receiveBufferSize;
    private readonly int _sendBufferSize;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,  // 使用[JsonPropertyName]指定的名称 / Use names specified by [JsonPropertyName]
        WriteIndented = false
    };

    public string AdapterName => "TouchSocket-Sorter";
    public string ProtocolType => "TCP";

    public TouchSocketSorterAdapter(
        string host,
        int port,
        ILogger<TouchSocketSorterAdapter> logger,
        IServiceScopeFactory serviceScopeFactory,
        ISystemClock clock,
        int reconnectIntervalMs = 5000,
        int receiveBufferSize = 8192,
        int sendBufferSize = 8192)
    {
        _host = host;
        _port = port;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _clock = clock;
        _reconnectIntervalMs = reconnectIntervalMs;
        _receiveBufferSize = receiveBufferSize;
        _sendBufferSize = sendBufferSize;
    }

    /// <summary>
    /// 发送格口号到分拣机（JSON协议）
    /// Send chute number to sorter (JSON protocol)
    /// </summary>
    /// <remarks>
    /// 发送 JSON 格式的 ChuteAssignmentNotification，兼容 WheelDiverterSorter
    /// Sends JSON format ChuteAssignmentNotification, compatible with WheelDiverterSorter
    /// </remarks>
    public async Task<bool> SendChuteNumberAsync(string parcelId, string chuteNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureConnectedAsync(cancellationToken);

            if (_tcpClient?.Online != true)
            {
                _logger.LogWarning("TCP连接未建立，无法发送数据 / TCP not connected, cannot send data");
                await LogCommunicationAsync(
                    CommunicationType.Tcp,
                    CommunicationDirection.Outbound,
                    $"包裹ID: {parcelId}, 格口: {chuteNumber}",
                    parcelId: parcelId,
                    remoteAddress: $"{_host}:{_port}",
                    isSuccess: false,
                    errorMessage: "TCP连接未建立");
                return false;
            }

            // 解析 parcelId 为 long 类型
            // Parse parcelId to long type
            if (!long.TryParse(parcelId, out var parcelIdLong))
            {
                parcelIdLong = Math.Abs(parcelId.GetHashCode());
            }
            
            // 解析 chuteNumber 为 long 类型
            // Parse chuteNumber to long type  
            long chuteId = ParseChuteNumber(chuteNumber);

            // 构造 JSON 格式的格口分配通知（兼容 WheelDiverterSorter）
            // Construct JSON format chute assignment notification
            var notification = new ChuteAssignmentNotification
            {
                ParcelId = parcelIdLong,
                ChuteId = chuteId,
                AssignedAt = _clock.UtcNow,
                Metadata = new Dictionary<string, string>
                {
                    { "Source", "RuleEngine" },
                    { "OriginalParcelId", parcelId },
                    { "OriginalChuteNumber", chuteNumber }
                }
            };

            var json = JsonSerializer.Serialize(notification, JsonOptions);
            var message = json + "\n";  // 添加终止符 / Add terminator
            var data = Encoding.UTF8.GetBytes(message);

            await _tcpClient.SendAsync(data);

            _logger.LogInformation(
                "TCP发送成功（JSON协议）/ TCP send successful (JSON protocol): ParcelId={ParcelId}, ChuteId={ChuteId}",
                parcelIdLong, chuteId);
            
            await LogCommunicationAsync(
                CommunicationType.Tcp,
                CommunicationDirection.Outbound,
                json,  // 记录JSON内容 / Log JSON content
                parcelId: parcelId,
                remoteAddress: $"{_host}:{_port}",
                isSuccess: true);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TCP发送失败 / TCP send failed: ParcelId={ParcelId}", parcelId);
            await LogCommunicationAsync(
                CommunicationType.Tcp,
                CommunicationDirection.Outbound,
                $"包裹ID: {parcelId}, 格口: {chuteNumber}",
                parcelId: parcelId,
                remoteAddress: $"{_host}:{_port}",
                isSuccess: false,
                errorMessage: ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 解析格口号为数字ID
    /// Parse chute number to numeric ID
    /// </summary>
    /// <remarks>
    /// 支持格式：
    /// - 纯数字："1", "999" -> 1, 999
    /// - 字母数字："A01", "B02" -> 1, 2
    /// - 其他格式：使用哈希值
    /// </remarks>
    private static long ParseChuteNumber(string chuteNumber)
    {
        // 尝试直接解析为数字
        if (long.TryParse(chuteNumber, out var numericId))
        {
            return numericId;
        }

        // 尝试提取末尾的数字部分（如"A01" -> 1, "CHUTE-999" -> 999）
        var digits = new string(chuteNumber.Where(char.IsDigit).ToArray());
        if (!string.IsNullOrEmpty(digits) && long.TryParse(digits, out var extractedId))
        {
            return extractedId;
        }

        // 使用哈希值作为兜底
        return Math.Abs(chuteNumber.GetHashCode()) % 10000;
    }

    /// <summary>
    /// 检查连接状态
    /// </summary>
    public Task<bool> IsConnectedAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_tcpClient?.Online == true);
    }

    /// <summary>
    /// 确保TCP连接已建立
    /// </summary>
    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        lock (_lockObj)
        {
            if (_tcpClient?.Online == true)
                return;
        }

        try
        {
            _tcpClient?.Close();
            _tcpClient?.Dispose();

            _tcpClient = new TcpClient();
            await _tcpClient.SetupAsync(new TouchSocketConfig()
                .SetRemoteIPHost(new IPHost($"{_host}:{_port}"))
                .SetTcpDataHandlingAdapter(() => new TerminatorPackageAdapter("\n")));

            await _tcpClient.ConnectAsync(_reconnectIntervalMs); // 自动重连间隔
            
            _logger.LogInformation("TCP连接已建立，地址: {Host}:{Port}", _host, _port);
            await LogCommunicationAsync(
                CommunicationType.Tcp,
                CommunicationDirection.Outbound,
                $"TCP连接已建立: {_host}:{_port}",
                remoteAddress: $"{_host}:{_port}",
                isSuccess: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TCP连接失败，地址: {Host}:{Port}", _host, _port);
            await LogCommunicationAsync(
                CommunicationType.Tcp,
                CommunicationDirection.Outbound,
                $"TCP连接失败: {_host}:{_port}",
                remoteAddress: $"{_host}:{_port}",
                isSuccess: false,
                errorMessage: ex.Message);
            throw;
        }
    }

    public void Dispose()
    {
        _tcpClient?.Close();
        _tcpClient?.Dispose();
    }

    /// <summary>
    /// 记录通信日志（使用IServiceScopeFactory避免DI生命周期违规）
    /// Log communication (using IServiceScopeFactory to avoid DI lifetime violation)
    /// </summary>
    private async Task LogCommunicationAsync(
        CommunicationType type,
        CommunicationDirection direction,
        string message,
        string? parcelId = null,
        string? remoteAddress = null,
        bool isSuccess = true,
        string? errorMessage = null)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ICommunicationLogRepository>();
        await repository.LogCommunicationAsync(type, direction, message, parcelId, remoteAddress, isSuccess, errorMessage);
    }
}
