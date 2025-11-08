# API Client Enhancements and Monitoring Improvements

## Overview

This document provides comprehensive documentation for the API client enhancements, monitoring improvements, and notification system implemented in the ZakYip Sorting Rule Engine system.

## API Client Enhancements

### Strongly-Typed Response Models

#### StronglyTypedApiResponseDto<TData>
Generic base class for all strongly-typed API responses.

**Type Parameters:**
- `TData`: The type of the response data

**Properties:**
- `Success` (bool): Indicates whether the API call was successful
- `Code` (string): Response code from the API
- `Message` (string): Human-readable response message
- `Data` (TData?): Strongly-typed response data
- `ErrorMessage` (string?): Error message if the call failed
- `RequestTime` (DateTime): When the request was initiated
- `ResponseTime` (DateTime?): When the response was received
- `DurationMs` (long): Duration of the API call in milliseconds

#### Response Data Models

**ChuteRequestResponseData**
Represents the response data for chute allocation requests.

Properties:
- `ChuteNumber` (string?): The allocated chute/gate number
- `ChuteName` (string?): The name of the allocated chute
- `ParcelId` (string?): The parcel identifier
- `Barcode` (string?): The barcode of the parcel
- `AdditionalInfo` (Dictionary<string, object>?): Additional contextual information

**ScanParcelResponseData**
Represents the response data for parcel scanning operations.

Properties:
- `ParcelId` (string?): The scanned parcel identifier
- `Barcode` (string?): The barcode that was scanned
- `ScanTime` (DateTime?): When the parcel was scanned
- `IsRegistered` (bool): Whether the parcel is registered in the system

**ImageUploadResponseData**
Represents the response data for image upload operations.

Properties:
- `ImageId` (string?): Unique identifier for the uploaded image
- `ImageUrl` (string?): URL where the image can be accessed
- `UploadTime` (DateTime?): When the image was uploaded
- `FileSize` (long): Size of the uploaded image in bytes

### Batch Operations

#### BatchOperationRequest<TRequest>
Generic container for batch operation requests.

**Type Parameters:**
- `TRequest`: The type of individual requests in the batch

**Properties:**
- `Requests` (List<TRequest>): List of individual requests to process
- `ProcessInParallel` (bool): Whether to process requests in parallel (default: true)
- `MaxDegreeOfParallelism` (int): Maximum number of parallel operations (default: 4)

#### BatchOperationResponse<TResponse>
Generic container for batch operation results.

**Type Parameters:**
- `TResponse`: The type of individual responses in the batch

**Properties:**
- `SuccessfulResponses` (List<TResponse>): List of successful responses
- `FailedResponses` (List<TResponse>): List of failed responses
- `TotalCount` (int): Total number of operations attempted
- `SuccessCount` (int): Number of successful operations
- `FailedCount` (int): Number of failed operations
- `TotalDurationMs` (long): Total time taken for all operations in milliseconds

### Enhanced API Adapter Interface

#### IEnhancedWcsApiAdapter
Extends `IWcsApiAdapter` with strongly-typed responses and batch operations.

**Methods:**

**ScanParcelStronglyTypedAsync**
```csharp
Task<StronglyTypedApiResponseDto<ScanParcelResponseData>> ScanParcelStronglyTypedAsync(
    string barcode,
    CancellationToken cancellationToken = default);
```
Scans a parcel and returns a strongly-typed response.

**RequestChuteStronglyTypedAsync**
```csharp
Task<StronglyTypedApiResponseDto<ChuteRequestResponseData>> RequestChuteStronglyTypedAsync(
    string parcelId,
    DwsData dwsData,
    OcrData? ocrData = null,
    CancellationToken cancellationToken = default);
```
Requests a chute allocation and returns a strongly-typed response.

**UploadImageStronglyTypedAsync**
```csharp
Task<StronglyTypedApiResponseDto<ImageUploadResponseData>> UploadImageStronglyTypedAsync(
    string barcode,
    byte[] imageData,
    string contentType = ConfigurationDefaults.ImageFile.DefaultContentType,
    CancellationToken cancellationToken = default);
```
Uploads an image and returns a strongly-typed response.

**BatchRequestChuteAsync**
```csharp
Task<BatchOperationResponse<WcsApiResponse>> BatchRequestChuteAsync(
    BatchOperationRequest<(string ParcelId, DwsData DwsData, OcrData? OcrData)> requests,
    CancellationToken cancellationToken = default);
```
Processes multiple chute allocation requests in a batch.

**BatchUploadImageAsync**
```csharp
Task<BatchOperationResponse<WcsApiResponse>> BatchUploadImageAsync(
    BatchOperationRequest<(string Barcode, byte[] ImageData, string ContentType)> requests,
    CancellationToken cancellationToken = default);
```
Uploads multiple images in a batch.

## Resilience Policies

### ApiResiliencePolicies
Static class providing factory methods for creating resilience pipelines using Polly v8.

#### Retry Pipeline
**CreateRetryPipeline**
```csharp
static ResiliencePipeline<HttpResponseMessage> CreateRetryPipeline(
    int maxRetryAttempts = 3,
    double baseDelay = 1.0)
```
Creates a retry pipeline with exponential backoff.

Parameters:
- `maxRetryAttempts`: Maximum number of retry attempts (default: 3)
- `baseDelay`: Base delay in seconds before first retry (default: 1.0)

Behavior:
- Uses exponential backoff strategy
- Retries on HTTP request exceptions and timeout rejections
- Retries on unsuccessful HTTP status codes
- Logs each retry attempt with delay information

#### Circuit Breaker Pipeline
**CreateCircuitBreakerPipeline**
```csharp
static ResiliencePipeline<HttpResponseMessage> CreateCircuitBreakerPipeline(
    double failureThreshold = 0.5,
    int samplingDuration = 30,
    int minimumThroughput = 10,
    int breakDuration = 60)
```
Creates a circuit breaker pipeline to prevent cascading failures.

Parameters:
- `failureThreshold`: Failure ratio threshold (0.0-1.0, default: 0.5 = 50%)
- `samplingDuration`: Time window for sampling in seconds (default: 30)
- `minimumThroughput`: Minimum requests before opening circuit (default: 10)
- `breakDuration`: Duration to keep circuit open in seconds (default: 60)

States:
- **Closed**: Normal operation, requests pass through
- **Open**: Too many failures detected, requests are blocked
- **Half-Open**: Testing if service has recovered

#### Timeout Pipeline
**CreateTimeoutPipeline**
```csharp
static ResiliencePipeline<HttpResponseMessage> CreateTimeoutPipeline(
    int timeoutSeconds = 30)
```
Creates a timeout pipeline to prevent hanging requests.

Parameters:
- `timeoutSeconds`: Maximum time to wait for a response (default: 30)

#### Complete Resilience Pipeline
**CreateCompleteResiliencePipeline**
```csharp
static ResiliencePipeline<HttpResponseMessage> CreateCompleteResiliencePipeline(
    int maxRetryAttempts = 3,
    int timeoutSeconds = 30,
    double circuitBreakerFailureThreshold = 0.5,
    int circuitBreakerDuration = 60)
```
Creates a complete resilience pipeline combining timeout, retry, and circuit breaker.

Execution Order:
1. Timeout (outermost)
2. Retry (middle)
3. Circuit Breaker (innermost)

#### Lightweight Resilience Pipeline
**CreateLightweightResiliencePipeline**
```csharp
static ResiliencePipeline<HttpResponseMessage> CreateLightweightResiliencePipeline(
    int maxRetryAttempts = 3,
    int timeoutSeconds = 30)
```
Creates a lightweight resilience pipeline with only timeout and retry (no circuit breaker).

## Monitoring and Alerting

### Notification Services

#### IAlertNotificationService
Main interface for sending alert notifications through multiple channels.

**Methods:**

**SendAlertAsync**
```csharp
Task<bool> SendAlertAsync(
    MonitoringAlert alert,
    CancellationToken cancellationToken = default);
```
Sends an alert notification through appropriate channels based on severity.

Severity-based routing:
- **Critical**: Email + SMS + WeChat Work
- **Warning**: Email + WeChat Work
- **Info**: WeChat Work only

**SendBatchAlertsAsync**
```csharp
Task<(int SuccessCount, int FailedCount)> SendBatchAlertsAsync(
    List<MonitoringAlert> alerts,
    CancellationToken cancellationToken = default);
```
Sends multiple alert notifications in batch.

#### IEmailNotificationService
Interface for email notification service.

**Methods:**

**SendEmailAsync**
```csharp
Task<bool> SendEmailAsync(
    string to,
    string subject,
    string body,
    bool isHtml = true,
    CancellationToken cancellationToken = default);
```
Sends an email to a single recipient.

**SendAlertEmailAsync**
```csharp
Task<bool> SendAlertEmailAsync(
    MonitoringAlert alert,
    List<string> recipients,
    CancellationToken cancellationToken = default);
```
Sends an alert email to multiple recipients with HTML formatting.

Email Format:
- Color-coded alert box based on severity
- Alert type, severity, and timestamp
- Current value and threshold (if applicable)
- Resource identifier (if applicable)

#### ISmsNotificationService
Interface for SMS notification service.

**Methods:**

**SendSmsAsync**
```csharp
Task<bool> SendSmsAsync(
    string phoneNumber,
    string message,
    CancellationToken cancellationToken = default);
```
Sends an SMS to a single phone number.

**SendAlertSmsAsync**
```csharp
Task<bool> SendAlertSmsAsync(
    MonitoringAlert alert,
    List<string> phoneNumbers,
    CancellationToken cancellationToken = default);
```
Sends an alert SMS to multiple phone numbers.

SMS Format:
```
【告警通知】{Title}
级别: {Severity}
时间: {AlertTime}
消息: {Message}
```

#### IWeChatWorkNotificationService
Interface for WeChat Work (企业微信) notification service.

**Methods:**

**SendTextMessageAsync**
```csharp
Task<bool> SendTextMessageAsync(
    string content,
    CancellationToken cancellationToken = default);
```
Sends a plain text message.

**SendMarkdownMessageAsync**
```csharp
Task<bool> SendMarkdownMessageAsync(
    string content,
    CancellationToken cancellationToken = default);
```
Sends a Markdown-formatted message.

**SendAlertMessageAsync**
```csharp
Task<bool> SendAlertMessageAsync(
    MonitoringAlert alert,
    CancellationToken cancellationToken = default);
```
Sends an alert message with Markdown formatting.

Markdown Format:
```markdown
## {Title}

> **告警级别**: {Severity}
> **告警类型**: {Type}
> **告警时间**: {AlertTime}

**告警消息**: {Message}

**当前值**: {CurrentValue}
**阈值**: {ThresholdValue}
**资源ID**: {ResourceId}

请及时处理！
```

### Existing Monitoring Features

The system already includes comprehensive monitoring capabilities:

#### Real-time Monitoring
- **Parcel processing rate**: Parcels per minute
- **Active chutes**: Number of currently active chutes
- **Chute usage rate**: Percentage utilization of chutes
- **Error rate**: Percentage of failed operations
- **Database status**: Health of database connections
- **System health**: Overall system health assessment

#### Alert Types
- `ParcelProcessing`: Processing rate alerts
- `ChuteUsage`: Chute utilization alerts
- `ErrorRate`: Error rate threshold alerts
- `DatabaseCircuitBreaker`: Database connectivity alerts
- `SystemPerformance`: Performance degradation alerts

#### Alert Severities
- `Critical`: Requires immediate attention
- `Warning`: Requires attention
- `Info`: Informational only

#### Thresholds
- Chute usage warning: 80%
- Chute usage critical: 95%
- Error rate warning: 5%
- Error rate critical: 15%
- Processing rate low threshold: 10 parcels/minute

## Usage Examples

### Using Strongly-Typed Responses

```csharp
// Inject IEnhancedWcsApiAdapter
private readonly IEnhancedWcsApiAdapter _apiClient;

// Scan parcel with strongly-typed response
var scanResponse = await _apiClient.ScanParcelStronglyTypedAsync("BARCODE123");
if (scanResponse.Success && scanResponse.Data != null)
{
    Console.WriteLine($"Parcel {scanResponse.Data.ParcelId} is registered: {scanResponse.Data.IsRegistered}");
}

// Request chute with strongly-typed response
var dwsData = new DwsData { /* ... */ };
var chuteResponse = await _apiClient.RequestChuteStronglyTypedAsync("PARCEL001", dwsData);
if (chuteResponse.Success && chuteResponse.Data != null)
{
    Console.WriteLine($"Allocated chute: {chuteResponse.Data.ChuteNumber}");
}
```

### Using Batch Operations

```csharp
// Batch request chutes
var batchRequest = new BatchOperationRequest<(string, DwsData, OcrData?)>
{
    Requests = new List<(string, DwsData, OcrData?)>
    {
        ("PARCEL001", dwsData1, null),
        ("PARCEL002", dwsData2, null),
        ("PARCEL003", dwsData3, null)
    },
    ProcessInParallel = true,
    MaxDegreeOfParallelism = 4
};

var batchResponse = await _apiClient.BatchRequestChuteAsync(batchRequest);
Console.WriteLine($"Success: {batchResponse.SuccessCount}, Failed: {batchResponse.FailedCount}");
Console.WriteLine($"Total duration: {batchResponse.TotalDurationMs}ms");
```

### Using Resilience Policies

```csharp
// Create HTTP client with resilience
var pipeline = ApiResiliencePolicies.CreateCompleteResiliencePipeline(
    maxRetryAttempts: 3,
    timeoutSeconds: 30,
    circuitBreakerFailureThreshold: 0.5,
    circuitBreakerDuration: 60
);

// Execute request with resilience
var response = await pipeline.ExecuteAsync(async ct =>
{
    return await httpClient.SendAsync(request, ct);
});
```

### Configuring Notifications

```csharp
// Configure notification services in DI container
services.AddScoped<IEmailNotificationService, EmailNotificationService>();
services.AddScoped<ISmsNotificationService, SmsNotificationService>();
services.AddScoped<IWeChatWorkNotificationService, WeChatWorkNotificationService>();
services.AddScoped<IAlertNotificationService, AlertNotificationService>();

// Notification service will automatically route alerts based on severity
var alert = new MonitoringAlert
{
    Type = AlertType.ErrorRate,
    Severity = AlertSeverity.Critical,
    Title = "High Error Rate Detected",
    Message = "Error rate has exceeded 15%",
    CurrentValue = 18.5m,
    ThresholdValue = 15.0m
};

await alertNotificationService.SendAlertAsync(alert);
```

## Configuration

### Notification Configuration
To enable notifications, configure the following in `appsettings.json`:

```json
{
  "Notifications": {
    "Email": {
      "Enabled": true,
      "SmtpServer": "smtp.example.com",
      "SmtpPort": 587,
      "Username": "notifications@example.com",
      "Password": "password",
      "FromAddress": "notifications@example.com",
      "Recipients": [
        "admin1@example.com",
        "admin2@example.com"
      ]
    },
    "Sms": {
      "Enabled": true,
      "Provider": "Aliyun",
      "AccessKeyId": "your-access-key",
      "AccessKeySecret": "your-secret",
      "SignName": "YourApp",
      "TemplateCode": "SMS_123456",
      "PhoneNumbers": [
        "13800138000",
        "13800138001"
      ]
    },
    "WeChat": {
      "Enabled": true,
      "WebhookUrl": "https://qyapi.weixin.qq.com/cgi-bin/webhook/send?key=your-key"
    }
  }
}
```

### Resilience Configuration
```json
{
  "ApiResilience": {
    "MaxRetryAttempts": 3,
    "TimeoutSeconds": 30,
    "CircuitBreakerFailureThreshold": 0.5,
    "CircuitBreakerDuration": 60
  }
}
```

## Testing

### Test Coverage
The implementation includes comprehensive unit tests:

- **EnhancedWcsApiClientTests**: 10 test methods covering strongly-typed responses and batch operations
- **NotificationServicesTests**: 15 test methods covering all notification channels
- **ApiResiliencePoliciesTests**: 9 test methods covering resilience policies

Total: **34 new test methods** ensuring reliability and correctness.

## Performance Considerations

### Batch Operations
- Use `ProcessInParallel = true` for I/O-bound operations
- Adjust `MaxDegreeOfParallelism` based on available resources
- Monitor performance metrics to optimize batch sizes

### Resilience Policies
- Adjust retry attempts based on API characteristics
- Set appropriate timeout values to avoid resource exhaustion
- Monitor circuit breaker state to detect systemic issues

### Notification Throttling
- Implement rate limiting to prevent notification spam
- Batch similar alerts within time windows
- Use severity-based prioritization

## Future Enhancements

1. **Configuration-driven notification routing**
   - Dynamic channel selection based on alert type and severity
   - User preference management

2. **Advanced resilience strategies**
   - Fallback strategies for degraded operation
   - Bulkhead isolation for resource protection

3. **Enhanced monitoring**
   - Predictive alerting using ML
   - Anomaly detection
   - Trend analysis and recommendations

4. **Notification templating**
   - Customizable message templates
   - Multi-language support
   - Rich media notifications

## Conclusion

These enhancements provide a robust, resilient, and well-monitored API client infrastructure with comprehensive alerting capabilities. The strongly-typed responses improve type safety and developer experience, while batch operations enhance efficiency. The resilience policies ensure reliability, and the multi-channel notification system ensures timely awareness of system health issues.
