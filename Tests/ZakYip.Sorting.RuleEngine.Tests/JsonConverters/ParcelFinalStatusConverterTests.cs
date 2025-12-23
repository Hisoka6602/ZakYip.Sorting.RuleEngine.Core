using System.Text.Json;
using Xunit;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Downstream;
using ZakYip.Sorting.RuleEngine.Domain.Enums;

namespace ZakYip.Sorting.RuleEngine.Tests.JsonConverters;

/// <summary>
/// ParcelFinalStatusConverter 测试
/// ParcelFinalStatusConverter tests
/// </summary>
public class ParcelFinalStatusConverterTests
{
    /// <summary>
    /// 测试从数字格式反序列化（下游系统发送数字）
    /// Test deserialization from number format (downstream system sends numbers)
    /// </summary>
    [Theory]
    [InlineData(0, ParcelFinalStatus.Success)]
    [InlineData(1, ParcelFinalStatus.Timeout)]
    [InlineData(2, ParcelFinalStatus.Lost)]
    [InlineData(3, ParcelFinalStatus.ExecutionError)]
    public void Should_Deserialize_FinalStatus_From_Number(int numberValue, ParcelFinalStatus expectedStatus)
    {
        // Arrange: 模拟下游系统发送的 JSON（FinalStatus 为数字）
        var json = $$"""
        {
            "Type": "SortingCompleted",
            "ParcelId": 12345,
            "ActualChuteId": 101,
            "CompletedAt": "2025-12-23T11:33:03Z",
            "IsSuccess": true,
            "FinalStatus": {{numberValue}}
        }
        """;

        // Act: 反序列化
        var notification = JsonSerializer.Deserialize<SortingCompletedNotificationDto>(json);

        // Assert: 验证 FinalStatus 正确解析
        Assert.NotNull(notification);
        Assert.Equal(expectedStatus, notification.FinalStatus);
    }

    /// <summary>
    /// 测试从字符串格式反序列化（兼容模式）
    /// Test deserialization from string format (compatibility mode)
    /// </summary>
    [Theory]
    [InlineData("Success", ParcelFinalStatus.Success)]
    [InlineData("Timeout", ParcelFinalStatus.Timeout)]
    [InlineData("Lost", ParcelFinalStatus.Lost)]
    [InlineData("ExecutionError", ParcelFinalStatus.ExecutionError)]
    [InlineData("success", ParcelFinalStatus.Success)]  // 忽略大小写
    [InlineData("TIMEOUT", ParcelFinalStatus.Timeout)]  // 忽略大小写
    public void Should_Deserialize_FinalStatus_From_String(string stringValue, ParcelFinalStatus expectedStatus)
    {
        // Arrange: 模拟字符串格式的 JSON
        var json = $$"""
        {
            "Type": "SortingCompleted",
            "ParcelId": 12345,
            "ActualChuteId": 101,
            "CompletedAt": "2025-12-23T11:33:03Z",
            "IsSuccess": true,
            "FinalStatus": "{{stringValue}}"
        }
        """;

        // Act: 反序列化
        var notification = JsonSerializer.Deserialize<SortingCompletedNotificationDto>(json);

        // Assert: 验证 FinalStatus 正确解析
        Assert.NotNull(notification);
        Assert.Equal(expectedStatus, notification.FinalStatus);
    }

    /// <summary>
    /// 测试序列化为字符串格式
    /// Test serialization to string format
    /// </summary>
    [Fact]
    public void Should_Serialize_FinalStatus_As_String()
    {
        // Arrange: 创建通知对象
        var notification = new SortingCompletedNotificationDto
        {
            ParcelId = 12345,
            ActualChuteId = 101,
            CompletedAt = DateTimeOffset.Parse("2025-12-23T11:33:03Z"),
            IsSuccess = true,
            FinalStatus = ParcelFinalStatus.Success
        };

        // Act: 序列化
        var json = JsonSerializer.Serialize(notification);

        // Assert: 验证 FinalStatus 输出为字符串格式
        Assert.Contains("\"FinalStatus\":\"Success\"", json);
        Assert.DoesNotContain("\"FinalStatus\":0", json);
    }

    /// <summary>
    /// 测试无效数字值抛出异常
    /// Test invalid number value throws exception
    /// </summary>
    [Fact]
    public void Should_Throw_Exception_For_Invalid_Number()
    {
        // Arrange: 无效的数字值
        var json = """
        {
            "Type": "SortingCompleted",
            "ParcelId": 12345,
            "ActualChuteId": 101,
            "CompletedAt": "2025-12-23T11:33:03Z",
            "IsSuccess": true,
            "FinalStatus": 999
        }
        """;

        // Act & Assert: 应该抛出异常
        Assert.Throws<JsonException>(() => 
            JsonSerializer.Deserialize<SortingCompletedNotificationDto>(json));
    }

    /// <summary>
    /// 测试无效字符串值抛出异常
    /// Test invalid string value throws exception
    /// </summary>
    [Fact]
    public void Should_Throw_Exception_For_Invalid_String()
    {
        // Arrange: 无效的字符串值
        var json = """
        {
            "Type": "SortingCompleted",
            "ParcelId": 12345,
            "ActualChuteId": 101,
            "CompletedAt": "2025-12-23T11:33:03Z",
            "IsSuccess": true,
            "FinalStatus": "InvalidStatus"
        }
        """;

        // Act & Assert: 应该抛出异常
        Assert.Throws<JsonException>(() => 
            JsonSerializer.Deserialize<SortingCompletedNotificationDto>(json));
    }

    /// <summary>
    /// 测试实际的错误场景（模拟原始错误日志）
    /// Test actual error scenario (simulate original error log)
    /// </summary>
    [Fact]
    public void Should_Handle_Real_World_Scenario_With_Number_FinalStatus()
    {
        // Arrange: 模拟实际下游系统的 JSON 响应（BytePositionInLine: 173 对应 FinalStatus 为数字）
        var json = """
        {
            "Type": "SortingCompleted",
            "ParcelId": 123456789,
            "ActualChuteId": 42,
            "CompletedAt": "2025-12-23T11:33:03.3244+08:00",
            "IsSuccess": true,
            "FinalStatus": 0,
            "FailureReason": null,
            "AffectedParcelIds": null
        }
        """;

        // Act: 反序列化应该成功
        var notification = JsonSerializer.Deserialize<SortingCompletedNotificationDto>(json);

        // Assert: 验证解析成功
        Assert.NotNull(notification);
        Assert.Equal(123456789, notification.ParcelId);
        Assert.Equal(42, notification.ActualChuteId);
        Assert.True(notification.IsSuccess);
        Assert.Equal(ParcelFinalStatus.Success, notification.FinalStatus);  // ✅ 0 映射到 Success
        Assert.Null(notification.FailureReason);
        Assert.Null(notification.AffectedParcelIds);
    }
}
