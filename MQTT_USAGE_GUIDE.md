# MQTT 适配器使用指南 / MQTT Adapter Usage Guide

本文档介绍如何在ZakYip分拣规则引擎中使用MQTT通信适配器。
This document explains how to use the MQTT communication adapters in the ZakYip Sorting Rule Engine.

## 概述 / Overview

系统现在支持通过MQTT协议与分拣机和DWS设备通信。MQTT适配器提供以下功能：
The system now supports communication with sorting machines and DWS devices via MQTT protocol. The MQTT adapters provide:

- ✅ 自动重连 / Automatic reconnection
- ✅ QoS控制 / QoS control
- ✅ JSON消息格式 / JSON message format
- ✅ 可选认证 / Optional authentication
- ✅ 完整日志记录 / Complete logging

## MQTT分拣机适配器 / MQTT Sorter Adapter

### 使用示例 / Usage Example

```csharp
using ZakYip.Sorting.RuleEngine.Infrastructure.Adapters.Sorter;

// 创建适配器 / Create adapter
var adapter = new MqttSorterAdapter(
    brokerHost: "mqtt.example.com",      // MQTT代理地址 / MQTT broker host
    brokerPort: 1883,                     // MQTT端口 / MQTT port
    publishTopic: "sorter/chute",         // 发布主题 / Publish topic
    logger: loggerInstance,               // 日志记录器 / Logger
    communicationLogRepository: logRepo,   // 通信日志仓储 / Log repository
    clientId: "sorter-client-001",        // 客户端ID（可选） / Client ID (optional)
    username: "mqtt-user",                // 用户名（可选） / Username (optional)
    password: "mqtt-password"             // 密码（可选） / Password (optional)
);

// 发送格口号 / Send chute number
bool success = await adapter.SendChuteNumberAsync(
    parcelId: "PKG-12345",
    chuteNumber: "CHUTE-08"
);

// 检查连接状态 / Check connection status
bool isConnected = await adapter.IsConnectedAsync();

// 释放资源 / Dispose
adapter.Dispose();
```

### 消息格式 / Message Format

发送到分拣机的消息采用JSON格式：
Messages sent to the sorter use JSON format:

```json
{
  "ParcelId": "PKG-12345",
  "ChuteNumber": "CHUTE-08",
  "Timestamp": "2025-11-09T07:00:00Z"
}
```

## MQTT DWS适配器 / MQTT DWS Adapter

### 使用示例 / Usage Example

```csharp
using ZakYip.Sorting.RuleEngine.Infrastructure.Adapters.Dws;

// 创建适配器 / Create adapter
var adapter = new MqttDwsAdapter(
    brokerHost: "mqtt.example.com",      // MQTT代理地址 / MQTT broker host
    brokerPort: 1883,                     // MQTT端口 / MQTT port
    subscribeTopic: "dws/data",           // 订阅主题 / Subscribe topic
    logger: loggerInstance,               // 日志记录器 / Logger
    communicationLogRepository: logRepo,   // 通信日志仓储 / Log repository
    clientId: "dws-client-001",           // 客户端ID（可选） / Client ID (optional)
    username: "mqtt-user",                // 用户名（可选） / Username (optional)
    password: "mqtt-password"             // 密码（可选） / Password (optional)
);

// 订阅DWS数据接收事件 / Subscribe to DWS data received event
adapter.OnDwsDataReceived += async (dwsData) =>
{
    Console.WriteLine($"Received DWS data for barcode: {dwsData.Barcode}");
    Console.WriteLine($"Weight: {dwsData.Weight}g, Volume: {dwsData.Volume}cm³");
    await Task.CompletedTask;
};

// 启动监听 / Start listening
await adapter.StartAsync();

// 停止监听 / Stop listening
await adapter.StopAsync();

// 释放资源 / Dispose
adapter.Dispose();
```

### 消息格式 / Message Format

从DWS设备接收的消息应采用JSON格式：
Messages received from DWS devices should use JSON format:

```json
{
  "Barcode": "1234567890123",
  "Weight": 1250.5,
  "Length": 300,
  "Width": 200,
  "Height": 150,
  "Volume": 9000,
  "ScannedAt": "2025-11-09T07:00:00Z"
}
```

## 配置示例 / Configuration Example

### appsettings.json

```json
{
  "MqttSettings": {
    "Sorter": {
      "BrokerHost": "mqtt.example.com",
      "BrokerPort": 1883,
      "PublishTopic": "sorter/chute",
      "ClientId": "sorter-client",
      "Username": "sorter-user",
      "Password": "sorter-password"
    },
    "Dws": {
      "BrokerHost": "mqtt.example.com",
      "BrokerPort": 1883,
      "SubscribeTopic": "dws/data",
      "ClientId": "dws-client",
      "Username": "dws-user",
      "Password": "dws-password"
    }
  }
}
```

## 依赖注册 / Dependency Registration

在 `Program.cs` 或 `Startup.cs` 中注册适配器：
Register adapters in `Program.cs` or `Startup.cs`:

```csharp
// 注册MQTT分拣机适配器 / Register MQTT sorter adapter
services.AddSingleton<ISorterAdapter>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<MqttSorterAdapter>>();
    var logRepo = provider.GetRequiredService<ICommunicationLogRepository>();
    var config = provider.GetRequiredService<IConfiguration>();
    
    return new MqttSorterAdapter(
        config["MqttSettings:Sorter:BrokerHost"],
        int.Parse(config["MqttSettings:Sorter:BrokerPort"]),
        config["MqttSettings:Sorter:PublishTopic"],
        logger,
        logRepo,
        config["MqttSettings:Sorter:ClientId"],
        config["MqttSettings:Sorter:Username"],
        config["MqttSettings:Sorter:Password"]
    );
});

// 注册MQTT DWS适配器 / Register MQTT DWS adapter
services.AddSingleton<IDwsAdapter>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<MqttDwsAdapter>>();
    var logRepo = provider.GetRequiredService<ICommunicationLogRepository>();
    var config = provider.GetRequiredService<IConfiguration>();
    
    return new MqttDwsAdapter(
        config["MqttSettings:Dws:BrokerHost"],
        int.Parse(config["MqttSettings:Dws:BrokerPort"]),
        config["MqttSettings:Dws:SubscribeTopic"],
        logger,
        logRepo,
        config["MqttSettings:Dws:ClientId"],
        config["MqttSettings:Dws:Username"],
        config["MqttSettings:Dws:Password"]
    );
});
```

## 故障排除 / Troubleshooting

### 连接失败 / Connection Failed

1. 检查MQTT代理地址和端口是否正确 / Check MQTT broker host and port
2. 验证防火墙设置允许MQTT通信 / Verify firewall allows MQTT communication
3. 确认用户名和密码正确（如果需要认证） / Confirm username and password (if authentication required)

### 消息未接收 / Messages Not Received

1. 验证订阅的主题名称正确 / Verify subscribe topic name is correct
2. 检查消息格式是否为有效的JSON / Check message format is valid JSON
3. 查看通信日志获取详细错误信息 / Check communication logs for detailed error messages

### 自动重连 / Automatic Reconnection

适配器默认每5秒尝试重新连接。如果连接断开，系统会自动尝试重连。
Adapters attempt reconnection every 5 seconds by default. If connection is lost, the system automatically tries to reconnect.

## 性能建议 / Performance Recommendations

1. **QoS级别** / **QoS Level**: 当前使用QoS 1（至少一次），适合大多数场景
   - Currently using QoS 1 (At Least Once), suitable for most scenarios

2. **消息大小** / **Message Size**: 保持消息简洁，避免大量额外数据
   - Keep messages concise, avoid excessive additional data

3. **连接池** / **Connection Pooling**: 为不同的设备使用不同的客户端ID
   - Use different client IDs for different devices

## 更多信息 / More Information

- MQTTnet文档 / MQTTnet Documentation: https://github.com/dotnet/MQTTnet
- MQTT协议规范 / MQTT Protocol Specification: https://mqtt.org/
- 项目主页 / Project Home: https://github.com/Hisoka6602/ZakYip.Sorting.RuleEngine.Core
