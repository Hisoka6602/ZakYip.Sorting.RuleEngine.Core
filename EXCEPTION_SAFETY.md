# 异常安全隔离文档 / Exception Safety Isolation Documentation

## 概述 / Overview

本文档说明了系统中如何实现异常安全隔离，确保所有可能抛出异常的方法都得到适当处理。

This document explains how exception safety isolation is implemented in the system, ensuring all potentially exception-throwing methods are properly handled.

## 设计原则 / Design Principles

### 1. 防御性编程 / Defensive Programming
- 所有外部调用（网络、数据库、文件系统）都使用try-catch包装
- All external calls (network, database, file system) are wrapped with try-catch
- 假设所有外部依赖可能失败
- Assume all external dependencies can fail

### 2. 优雅降级 / Graceful Degradation
- 系统在部分组件失败时仍能继续运行
- System continues to operate when partial components fail
- 主功能路径有降级策略
- Main functional paths have fallback strategies

### 3. 日志记录 / Logging
- 所有异常都被记录，包括堆栈跟踪
- All exceptions are logged with stack traces
- 使用结构化日志便于分析
- Use structured logging for analysis

### 4. 用户友好的错误消息 / User-Friendly Error Messages
- 内部异常不暴露给外部用户
- Internal exceptions are not exposed to external users
- 返回有意义的错误代码和消息
- Return meaningful error codes and messages

## 实现位置 / Implementation Locations

### 1. API端点 / API Endpoints

**位置**: 所有Web API控制器和Minimal API端点
**Location**: All Web API controllers and Minimal API endpoints

**示例 / Example**:
```csharp
app.MapGet("/api/interface/random", () =>
{
    try
    {
        var interfaceId = Random.Shared.Next(1, 51);
        return Results.Ok(new InterfaceResponse { ... });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Error generating interface ID",
            detail: ex.Message,
            statusCode: 500
        );
    }
});
```

### 2. 外部适配器 / External Adapters

**位置**: Infrastructure/Adapters
**Location**: Infrastructure/Adapters

**已实现 / Implemented**:
- `MqttSorterAdapter` - MQTT分拣机通信
- `MqttDwsAdapter` - MQTT DWS通信
- `TcpSorterAdapter` - TCP分拣机通信
- `TouchSocketDwsAdapter` - TCP DWS通信

**示例 / Example**:
```csharp
public async Task<bool> SendChuteNumberAsync(string parcelId, string chuteNumber)
{
    try
    {
        await EnsureConnectedAsync();
        // ... 发送逻辑 / Send logic
        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "发送格口号失败: {ParcelId}", parcelId);
        await LogFailureAsync(parcelId, ex.Message);
        return false;
    }
}
```

### 3. API客户端 / API Clients

**位置**: Infrastructure/ApiClients
**Location**: Infrastructure/ApiClients

**已实现 / Implemented**:
- `WcsApiClient` - WCS系统API
- `PostCollectionApiClient` - 邮政分揽投API
- `PostProcessingCenterApiClient` - 邮政处理中心API
- `JushuitanErpApiClient` - 聚水潭ERPAPI
- `WdtWmsApiClient` - 旺店通WMSAPI
- `WdtErpFlagshipApiClient` - 旺店通ERP旗舰版API

**示例 / Example**:
```csharp
public async Task<WcsApiResponse> RequestChuteAsync(...)
{
    var stopwatch = Stopwatch.StartNew();
    try
    {
        var response = await _httpClient.PostAsync(...);
        stopwatch.Stop();
        return CreateSuccessResponse(response, stopwatch.ElapsedMilliseconds);
    }
    catch (HttpRequestException ex)
    {
        stopwatch.Stop();
        _logger.LogError(ex, "HTTP请求失败");
        return CreateErrorResponse(ex.Message, stopwatch.ElapsedMilliseconds);
    }
    catch (TaskCanceledException ex)
    {
        stopwatch.Stop();
        _logger.LogWarning("请求超时");
        return CreateErrorResponse("请求超时", stopwatch.ElapsedMilliseconds);
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        _logger.LogError(ex, "请求异常");
        return CreateErrorResponse(ex.Message, stopwatch.ElapsedMilliseconds);
    }
}
```

### 4. 数据访问层 / Data Access Layer

**位置**: Infrastructure/Repositories
**Location**: Infrastructure/Repositories

**模式 / Pattern**:
- 数据库操作使用熔断器模式
- Database operations use circuit breaker pattern
- MySQL失败自动降级到SQLite
- Automatic fallback from MySQL to SQLite on failure

**示例 / Example**:
```csharp
public async Task<bool> SaveLogAsync(LogEntry log)
{
    try
    {
        await _mysqlContext.Logs.AddAsync(log);
        await _mysqlContext.SaveChangesAsync();
        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "MySQL保存失败，尝试SQLite");
        try
        {
            await _sqliteContext.Logs.AddAsync(log);
            await _sqliteContext.SaveChangesAsync();
            return true;
        }
        catch (Exception innerEx)
        {
            _logger.LogError(innerEx, "SQLite保存也失败");
            return false;
        }
    }
}
```

### 5. 后台服务 / Background Services

**位置**: Application/Services
**Location**: Application/Services

**已实现 / Implemented**:
- `ParcelQueueProcessorService` - 包裹队列处理
- `MonitoringAlertService` - 监控告警服务
- `DataCleanupService` - 数据清理服务
- `DataArchiveService` - 数据归档服务

**示例 / Example**:
```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        try
        {
            await ProcessWorkItemsAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // 正常取消，不记录
            break;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理工作项时发生错误");
            // 等待后重试
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
```

### 6. 数据模拟器 / Data Simulators

**位置**: Tests/ZakYip.Sorting.RuleEngine.DataSimulator
**Location**: Tests/ZakYip.Sorting.RuleEngine.DataSimulator

**已实现 / Implemented**:
- `MqttSorterSimulator` - MQTT分拣机模拟器
- `TcpSorterSimulator` - TCP分拣机模拟器
- `DwsSimulator` - DWS数据模拟器

**示例 / Example**:
```csharp
public async Task<bool> ConnectAsync()
{
    try
    {
        _mqttClient = factory.CreateMqttClient();
        var result = await _mqttClient.ConnectAsync(options);
        _isConnected = result.ResultCode == MqttClientConnectResultCode.Success;
        return _isConnected;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ 连接异常: {ex.Message}");
        _isConnected = false;
        return false;
    }
}
```

## 异常类型处理策略 / Exception Type Handling Strategy

### 1. 网络异常 / Network Exceptions

**类型 / Types**:
- `HttpRequestException` - HTTP请求失败
- `TaskCanceledException` - 请求超时
- `SocketException` - Socket连接失败
- `IOException` - I/O错误

**处理 / Handling**:
- 记录错误日志
- Log error
- 返回失败状态和错误消息
- Return failure status and error message
- 考虑重试（带指数退避）
- Consider retry (with exponential backoff)

### 2. 数据库异常 / Database Exceptions

**类型 / Types**:
- `DbUpdateException` - 数据库更新失败
- `SqlException` - SQL执行错误
- `TimeoutException` - 查询超时

**处理 / Handling**:
- 记录错误日志
- Log error
- 触发熔断器
- Trigger circuit breaker
- 降级到备用数据库（MySQL -> SQLite）
- Fallback to backup database (MySQL -> SQLite)

### 3. 序列化异常 / Serialization Exceptions

**类型 / Types**:
- `JsonException` - JSON解析失败
- `FormatException` - 格式错误

**处理 / Handling**:
- 记录错误日志和原始数据
- Log error and raw data
- 返回格式错误消息
- Return format error message

### 4. 验证异常 / Validation Exceptions

**类型 / Types**:
- `ArgumentException` - 参数错误
- `ArgumentNullException` - 参数为空
- `InvalidOperationException` - 无效操作

**处理 / Handling**:
- 返回400 Bad Request
- Return 400 Bad Request
- 提供详细的验证错误消息
- Provide detailed validation error messages

## 测试覆盖 / Test Coverage

### 单元测试 / Unit Tests

每个有异常处理的方法都应有对应测试：
Each method with exception handling should have corresponding tests:

```csharp
[Fact]
public async Task SendChuteNumberAsync_WhenConnectionFails_ReturnsFailure()
{
    // Arrange
    var adapter = CreateAdapter();
    
    // Act
    var result = await adapter.SendChuteNumberAsync("PKG123", "CHUTE01");
    
    // Assert
    Assert.False(result);
}
```

### 集成测试 / Integration Tests

测试真实场景下的异常处理：
Test exception handling in real scenarios:

```csharp
[Fact]
public async Task CompleteFlow_WhenMqttFails_FallsBackToTcp()
{
    // Arrange
    var service = CreateService();
    
    // Act
    var result = await service.ProcessParcelAsync("PKG123");
    
    // Assert
    Assert.True(result.Success);
    Assert.Equal("TCP", result.UsedProtocol);
}
```

## 监控和告警 / Monitoring and Alerting

### 异常指标 / Exception Metrics

系统监控以下异常指标：
The system monitors the following exception metrics:

- 异常总数 / Total exceptions
- 异常率（异常/请求总数）/ Exception rate (exceptions/total requests)
- 按类型分组的异常 / Exceptions grouped by type
- 按组件分组的异常 / Exceptions grouped by component

### 告警规则 / Alert Rules

触发告警的条件：
Conditions that trigger alerts:

- 错误率 > 5%：Warning
- Error rate > 5%: Warning
- 错误率 > 15%：Critical
- Error rate > 15%: Critical
- 连续失败 > 10次：Critical
- Consecutive failures > 10: Critical
- 重要组件异常：立即告警
- Critical component exception: Immediate alert

## 最佳实践 / Best Practices

1. **永远不要吞掉异常 / Never Swallow Exceptions**
   ```csharp
   // ❌ 错误 / Wrong
   try { ... } catch { }
   
   // ✅ 正确 / Correct
   try { ... } catch (Exception ex) { _logger.LogError(ex, "..."); }
   ```

2. **使用特定异常类型 / Use Specific Exception Types**
   ```csharp
   // ❌ 错误 / Wrong
   catch (Exception ex) { }
   
   // ✅ 正确 / Correct
   catch (HttpRequestException ex) { }
   catch (TaskCanceledException ex) { }
   catch (Exception ex) { }
   ```

3. **记录上下文信息 / Log Context Information**
   ```csharp
   _logger.LogError(ex, "处理包裹失败: ParcelId={ParcelId}, ChuteId={ChuteId}", 
       parcelId, chuteId);
   ```

4. **不要在循环中抛出异常 / Don't Throw in Loops**
   ```csharp
   // ❌ 错误 / Wrong
   foreach (var item in items)
   {
       ProcessItem(item); // 可能抛异常 / May throw
   }
   
   // ✅ 正确 / Correct
   foreach (var item in items)
   {
       try
       {
           ProcessItem(item);
       }
       catch (Exception ex)
       {
           _logger.LogError(ex, "处理项失败: {Item}", item);
       }
   }
   ```

5. **使用finally确保资源释放 / Use Finally for Resource Cleanup**
   ```csharp
   Connection? conn = null;
   try
   {
       conn = await OpenConnectionAsync();
       await ProcessAsync(conn);
   }
   catch (Exception ex)
   {
       _logger.LogError(ex, "处理失败");
   }
   finally
   {
       conn?.Dispose();
   }
   ```

## 总结 / Summary

系统通过以下措施实现了全面的异常安全隔离：

The system achieves comprehensive exception safety isolation through:

1. ✅ 所有外部调用都有异常处理 / All external calls have exception handling
2. ✅ 数据库操作有熔断和降级策略 / Database operations have circuit breaker and fallback
3. ✅ API端点返回标准化错误响应 / API endpoints return standardized error responses
4. ✅ 后台服务能从异常中恢复 / Background services recover from exceptions
5. ✅ 详细的日志记录便于故障排查 / Detailed logging for troubleshooting
6. ✅ 监控告警及时发现问题 / Monitoring and alerting detect issues promptly

所有可能抛出异常的方法都已得到适当保护，确保系统的健壮性和可靠性。

All potentially exception-throwing methods are properly protected, ensuring system robustness and reliability.
