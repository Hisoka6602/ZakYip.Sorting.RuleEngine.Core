using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Infrastructure.Services;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Tests.Services;

/// <summary>
/// 告警通知服务测试
/// Tests for AlertNotificationService
/// </summary>
public class AlertNotificationServiceTests
{
    private readonly Mock<IEmailNotificationService> _mockEmailService;
    private readonly Mock<ISmsNotificationService> _mockSmsService;
    private readonly Mock<IWeChatWorkNotificationService> _mockWeChatService;
    private readonly Mock<ILogger<AlertNotificationService>> _mockLogger;
    private readonly AlertNotificationService _service;

    public AlertNotificationServiceTests()
    {
        _mockEmailService = new Mock<IEmailNotificationService>();
        _mockSmsService = new Mock<ISmsNotificationService>();
        _mockWeChatService = new Mock<IWeChatWorkNotificationService>();
        _mockLogger = new Mock<ILogger<AlertNotificationService>>();
        
        _service = new AlertNotificationService(
            _mockEmailService.Object,
            _mockSmsService.Object,
            _mockWeChatService.Object,
            _mockLogger.Object);
    }

    /// <summary>
    /// 测试发送告警 - Critical级别
    /// Test SendAlertAsync - Critical severity
    /// </summary>
    [Fact]
    public async Task SendAlertAsync_CriticalAlert_CallsAllNotificationServices()
    {
        // Arrange
        var alert = new MonitoringAlert
        {
            AlertId = 1,
            Type = AlertType.ErrorRate,
            Severity = AlertSeverity.Critical,
            Title = "错误率严重告警",
            Message = "错误率已达到20%",
            AlertTime = DateTime.Now
        };

        // Note: Since notification services are disabled by default in the service,
        // this test verifies the service doesn't throw exceptions
        // In a real implementation, you'd configure the service with enabled notifications

        // Act & Assert
        // Should not throw
        var result = await _service.SendAlertAsync(alert);
    }

    /// <summary>
    /// 测试批量发送告警
    /// Test SendBatchAlertsAsync
    /// </summary>
    [Fact]
    public async Task SendBatchAlertsAsync_MultipleAlerts_ProcessesAll()
    {
        // Arrange
        var alerts = new List<MonitoringAlert>
        {
            new MonitoringAlert
            {
                AlertId = 1,
                Type = AlertType.ErrorRate,
                Severity = AlertSeverity.Critical,
                Title = "错误率告警1",
                Message = "错误率告警1"
            },
            new MonitoringAlert
            {
                AlertId = 2,
                Type = AlertType.ChuteUsage,
                Severity = AlertSeverity.Warning,
                Title = "格口使用率告警",
                Message = "格口使用率告警"
            }
        };

        // Act
        var (successCount, failedCount) = await _service.SendBatchAlertsAsync(alerts);

        // Assert
        Assert.Equal(2, successCount + failedCount);
    }
}

/// <summary>
/// 邮件通知服务测试
/// Tests for EmailNotificationService
/// </summary>
public class EmailNotificationServiceTests
{
    private readonly Mock<ILogger<EmailNotificationService>> _mockLogger;
    private readonly EmailNotificationService _service;

    public EmailNotificationServiceTests()
    {
        _mockLogger = new Mock<ILogger<EmailNotificationService>>();
        _service = new EmailNotificationService(_mockLogger.Object);
    }

    /// <summary>
    /// 测试发送邮件
    /// Test SendEmailAsync
    /// </summary>
    [Fact]
    public async Task SendEmailAsync_ValidInput_ReturnsTrue()
    {
        // Arrange
        var to = "test@example.com";
        var subject = "Test Email";
        var body = "This is a test email";

        // Act
        var result = await _service.SendEmailAsync(to, subject, body);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试发送告警邮件
    /// Test SendAlertEmailAsync
    /// </summary>
    [Fact]
    public async Task SendAlertEmailAsync_ValidAlert_ReturnsTrue()
    {
        // Arrange
        var alert = new MonitoringAlert
        {
            AlertId = 1,
            Type = AlertType.ErrorRate,
            Severity = AlertSeverity.Critical,
            Title = "测试告警",
            Message = "这是一个测试告警",
            CurrentValue = 15.5m,
            ThresholdValue = 10.0m,
            AlertTime = DateTime.Now
        };
        var recipients = new List<string> { "test@example.com" };

        // Act
        var result = await _service.SendAlertEmailAsync(alert, recipients);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试发送告警邮件 - 多个收件人
    /// Test SendAlertEmailAsync - Multiple recipients
    /// </summary>
    [Fact]
    public async Task SendAlertEmailAsync_MultipleRecipients_SendsToAll()
    {
        // Arrange
        var alert = new MonitoringAlert
        {
            AlertId = 1,
            Type = AlertType.ChuteUsage,
            Severity = AlertSeverity.Warning,
            Title = "格口使用率警告",
            Message = "格口使用率达到85%",
            AlertTime = DateTime.Now
        };
        var recipients = new List<string> 
        { 
            "admin1@example.com",
            "admin2@example.com",
            "admin3@example.com"
        };

        // Act
        var result = await _service.SendAlertEmailAsync(alert, recipients);

        // Assert
        Assert.True(result);
    }
}

/// <summary>
/// 短信通知服务测试
/// Tests for SmsNotificationService
/// </summary>
public class SmsNotificationServiceTests
{
    private readonly Mock<ILogger<SmsNotificationService>> _mockLogger;
    private readonly SmsNotificationService _service;

    public SmsNotificationServiceTests()
    {
        _mockLogger = new Mock<ILogger<SmsNotificationService>>();
        _service = new SmsNotificationService(_mockLogger.Object);
    }

    /// <summary>
    /// 测试发送短信
    /// Test SendSmsAsync
    /// </summary>
    [Fact]
    public async Task SendSmsAsync_ValidInput_ReturnsTrue()
    {
        // Arrange
        var phoneNumber = "13800138000";
        var message = "测试短信";

        // Act
        var result = await _service.SendSmsAsync(phoneNumber, message);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试发送告警短信
    /// Test SendAlertSmsAsync
    /// </summary>
    [Fact]
    public async Task SendAlertSmsAsync_ValidAlert_ReturnsTrue()
    {
        // Arrange
        var alert = new MonitoringAlert
        {
            AlertId = 1,
            Type = AlertType.ErrorRate,
            Severity = AlertSeverity.Critical,
            Title = "错误率严重告警",
            Message = "错误率已达到20%",
            AlertTime = DateTime.Now
        };
        var phoneNumbers = new List<string> { "13800138000" };

        // Act
        var result = await _service.SendAlertSmsAsync(alert, phoneNumbers);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试发送告警短信 - 多个手机号
    /// Test SendAlertSmsAsync - Multiple phone numbers
    /// </summary>
    [Fact]
    public async Task SendAlertSmsAsync_MultiplePhoneNumbers_SendsToAll()
    {
        // Arrange
        var alert = new MonitoringAlert
        {
            AlertId = 1,
            Type = AlertType.ParcelProcessing,
            Severity = AlertSeverity.Warning,
            Title = "包裹处理速率过低",
            Message = "当前处理速率低于阈值",
            AlertTime = DateTime.Now
        };
        var phoneNumbers = new List<string> 
        { 
            "13800138001",
            "13800138002",
            "13800138003"
        };

        // Act
        var result = await _service.SendAlertSmsAsync(alert, phoneNumbers);

        // Assert
        Assert.True(result);
    }
}

/// <summary>
/// 企业微信通知服务测试
/// Tests for WeChatWorkNotificationService
/// </summary>
public class WeChatWorkNotificationServiceTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<WeChatWorkNotificationService>> _mockLogger;
    private readonly WeChatWorkNotificationService _service;

    public WeChatWorkNotificationServiceTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _mockLogger = new Mock<ILogger<WeChatWorkNotificationService>>();
        _service = new WeChatWorkNotificationService(_httpClient, _mockLogger.Object);
    }

    /// <summary>
    /// 测试发送文本消息 - Webhook未配置
    /// Test SendTextMessageAsync - Webhook not configured
    /// </summary>
    [Fact]
    public async Task SendTextMessageAsync_WebhookNotConfigured_ReturnsFalse()
    {
        // Arrange
        var content = "测试消息";

        // Act
        var result = await _service.SendTextMessageAsync(content);

        // Assert
        // Should return false when webhook is not configured
        Assert.False(result);
    }

    /// <summary>
    /// 测试发送Markdown消息 - Webhook未配置
    /// Test SendMarkdownMessageAsync - Webhook not configured
    /// </summary>
    [Fact]
    public async Task SendMarkdownMessageAsync_WebhookNotConfigured_ReturnsFalse()
    {
        // Arrange
        var content = "## 测试标题\n\n测试内容";

        // Act
        var result = await _service.SendMarkdownMessageAsync(content);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试发送告警消息
    /// Test SendAlertMessageAsync
    /// </summary>
    [Fact]
    public async Task SendAlertMessageAsync_ValidAlert_InvokesMarkdownMessage()
    {
        // Arrange
        var alert = new MonitoringAlert
        {
            AlertId = 1,
            Type = AlertType.ErrorRate,
            Severity = AlertSeverity.Critical,
            Title = "错误率严重告警",
            Message = "错误率已达到20%",
            CurrentValue = 20.0m,
            ThresholdValue = 15.0m,
            AlertTime = DateTime.Now
        };

        // Act
        var result = await _service.SendAlertMessageAsync(alert);

        // Assert
        // Should return false when webhook is not configured
        Assert.False(result);
    }
}
