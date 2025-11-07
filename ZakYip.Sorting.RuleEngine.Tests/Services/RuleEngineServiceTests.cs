using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using ZakYip.Sorting.RuleEngine.Application.Services;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Tests.Services;

/// <summary>
/// 规则引擎服务单元测试
/// Unit tests for RuleEngineService
/// </summary>
public class RuleEngineServiceTests
{
    private readonly Mock<IRuleRepository> _mockRuleRepository;
    private readonly Mock<ILogger<RuleEngineService>> _mockLogger;
    private readonly IMemoryCache _memoryCache;
    private readonly RuleEngineService _service;

    public RuleEngineServiceTests()
    {
        _mockRuleRepository = new Mock<IRuleRepository>();
        _mockLogger = new Mock<ILogger<RuleEngineService>>();
        var mockPerfLogger = new Mock<ILogger<PerformanceMetricService>>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        var performanceService = new PerformanceMetricService(mockPerfLogger.Object);
        _service = new RuleEngineService(_mockRuleRepository.Object, _mockLogger.Object, _memoryCache, performanceService);
    }

    [Fact]
    public async Task EvaluateRulesAsync_WeightCondition_ReturnsCorrectChute()
    {
        // Arrange
        var rules = new List<SortingRule>
        {
            new SortingRule
            {
                RuleId = "R1",
                RuleName = "重量规则",
                ConditionExpression = "Weight > 1000",
                TargetChute = "CHUTE-A01",
                Priority = 1,
                IsEnabled = true
            }
        };

        _mockRuleRepository.Setup(r => r.GetEnabledRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        var parcelInfo = new ParcelInfo
        {
            ParcelId = "PKG001",
            CartNumber = "CART001"
        };

        var dwsData = new DwsData
        {
            Weight = 1500, // 大于1000
            Length = 300,
            Width = 200,
            Height = 150
        };

        // Act
        var result = await _service.EvaluateRulesAsync(parcelInfo, dwsData, null);

        // Assert
        Assert.Equal("CHUTE-A01", result);
    }

    [Fact]
    public async Task EvaluateRulesAsync_BarcodeContains_ReturnsCorrectChute()
    {
        // Arrange
        var rules = new List<SortingRule>
        {
            new SortingRule
            {
                RuleId = "R2",
                RuleName = "条码规则",
                ConditionExpression = "Barcode CONTAINS 'SF'",
                TargetChute = "CHUTE-B01",
                Priority = 1,
                IsEnabled = true
            }
        };

        _mockRuleRepository.Setup(r => r.GetEnabledRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        var parcelInfo = new ParcelInfo
        {
            ParcelId = "PKG002",
            CartNumber = "CART002",
            Barcode = "SF123456789"
        };

        var dwsData = new DwsData
        {
            Weight = 500,
            Length = 200,
            Width = 150,
            Height = 100
        };

        // Act
        var result = await _service.EvaluateRulesAsync(parcelInfo, dwsData, null);

        // Assert
        Assert.Equal("CHUTE-B01", result);
    }

    [Fact]
    public async Task EvaluateRulesAsync_NoMatchingRule_ReturnsNull()
    {
        // Arrange
        var rules = new List<SortingRule>
        {
            new SortingRule
            {
                RuleId = "R3",
                RuleName = "测试规则",
                ConditionExpression = "Weight > 5000",
                TargetChute = "CHUTE-C01",
                Priority = 1,
                IsEnabled = true
            }
        };

        _mockRuleRepository.Setup(r => r.GetEnabledRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        var parcelInfo = new ParcelInfo
        {
            ParcelId = "PKG003",
            CartNumber = "CART003"
        };

        var dwsData = new DwsData
        {
            Weight = 1000, // 小于5000，不匹配
            Length = 200,
            Width = 150,
            Height = 100
        };

        // Act
        var result = await _service.EvaluateRulesAsync(parcelInfo, dwsData, null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task EvaluateRulesAsync_CachingWorks_RepositoryCalledOnce()
    {
        // Arrange
        var rules = new List<SortingRule>
        {
            new SortingRule
            {
                RuleId = "R4",
                RuleName = "默认规则",
                ConditionExpression = "DEFAULT",
                TargetChute = "CHUTE-DEFAULT",
                Priority = 100,
                IsEnabled = true
            }
        };

        _mockRuleRepository.Setup(r => r.GetEnabledRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        var parcelInfo = new ParcelInfo { ParcelId = "PKG004", CartNumber = "CART004" };
        var dwsData = new DwsData { Weight = 1000 };

        // Act - 调用两次
        await _service.EvaluateRulesAsync(parcelInfo, dwsData, null);
        await _service.EvaluateRulesAsync(parcelInfo, dwsData, null);

        // Assert - 仓储只应该被调用一次（第二次使用缓存）
        _mockRuleRepository.Verify(r => r.GetEnabledRulesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EvaluateRulesAsync_PriorityOrdering_ReturnsHighestPriorityMatch()
    {
        // Arrange - 两个规则都匹配，但优先级不同（按优先级排序）
        var rules = new List<SortingRule>
        {
            new SortingRule
            {
                RuleId = "R6",
                RuleName = "高优先级规则",
                ConditionExpression = "Weight > 500",
                TargetChute = "CHUTE-HIGH",
                Priority = 1,
                IsEnabled = true
            },
            new SortingRule
            {
                RuleId = "R5",
                RuleName = "低优先级规则",
                ConditionExpression = "DEFAULT",
                TargetChute = "CHUTE-LOW",
                Priority = 100,
                IsEnabled = true
            }
        };

        // 确保规则按优先级排序（仓储应该返回已排序的规则）
        _mockRuleRepository.Setup(r => r.GetEnabledRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules.OrderBy(r => r.Priority));

        var parcelInfo = new ParcelInfo { ParcelId = "PKG005", CartNumber = "CART005" };
        var dwsData = new DwsData { Weight = 1000 };

        // Act
        var result = await _service.EvaluateRulesAsync(parcelInfo, dwsData, null);

        // Assert - 应该返回高优先级的规则
        Assert.Equal("CHUTE-HIGH", result);
    }

    #region Boundary Condition Tests

    /// <summary>
    /// 测试空规则列表返回null
    /// </summary>
    [Fact]
    public async Task EvaluateRulesAsync_EmptyRulesList_ReturnsNull()
    {
        // Arrange
        _mockRuleRepository.Setup(r => r.GetEnabledRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SortingRule>());

        var parcelInfo = new ParcelInfo { ParcelId = "PKG001", CartNumber = "CART001" };
        var dwsData = new DwsData { Weight = 1000 };

        // Act
        var result = await _service.EvaluateRulesAsync(parcelInfo, dwsData, null);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// 测试所有规则都不匹配返回null
    /// </summary>
    [Fact]
    public async Task EvaluateRulesAsync_NoMatchingRules_ReturnsNull()
    {
        // Arrange
        var rules = new List<SortingRule>
        {
            new SortingRule
            {
                RuleId = "R1",
                RuleName = "不匹配规则",
                ConditionExpression = "Weight > 10000",
                TargetChute = "CHUTE-A01",
                Priority = 1,
                IsEnabled = true
            }
        };

        _mockRuleRepository.Setup(r => r.GetEnabledRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        var parcelInfo = new ParcelInfo { ParcelId = "PKG001", CartNumber = "CART001" };
        var dwsData = new DwsData { Weight = 100 }; // 远小于10000

        // Act
        var result = await _service.EvaluateRulesAsync(parcelInfo, dwsData, null);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// 测试零重量边界值
    /// </summary>
    [Fact]
    public async Task EvaluateRulesAsync_ZeroWeight_HandlesCorrectly()
    {
        // Arrange
        var rules = new List<SortingRule>
        {
            new SortingRule
            {
                RuleId = "R1",
                RuleName = "零重量规则",
                ConditionExpression = "Weight == 0",
                TargetChute = "CHUTE-ZERO",
                Priority = 1,
                IsEnabled = true
            }
        };

        _mockRuleRepository.Setup(r => r.GetEnabledRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        var parcelInfo = new ParcelInfo { ParcelId = "PKG001", CartNumber = "CART001" };
        var dwsData = new DwsData { Weight = 0 };

        // Act
        var result = await _service.EvaluateRulesAsync(parcelInfo, dwsData, null);

        // Assert
        Assert.Equal("CHUTE-ZERO", result);
    }

    /// <summary>
    /// 测试最大重量边界值
    /// </summary>
    [Fact]
    public async Task EvaluateRulesAsync_MaxWeight_HandlesCorrectly()
    {
        // Arrange
        var rules = new List<SortingRule>
        {
            new SortingRule
            {
                RuleId = "R1",
                RuleName = "最大重量规则",
                ConditionExpression = "Weight >= 999999999",
                TargetChute = "CHUTE-MAX",
                Priority = 1,
                IsEnabled = true
            }
        };

        _mockRuleRepository.Setup(r => r.GetEnabledRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        var parcelInfo = new ParcelInfo { ParcelId = "PKG001", CartNumber = "CART001" };
        var dwsData = new DwsData { Weight = 999999999 };

        // Act
        var result = await _service.EvaluateRulesAsync(parcelInfo, dwsData, null);

        // Assert
        Assert.Equal("CHUTE-MAX", result);
    }

    /// <summary>
    /// 测试空条码处理
    /// </summary>
    [Fact]
    public async Task EvaluateRulesAsync_EmptyBarcode_HandlesCorrectly()
    {
        // Arrange
        var rules = new List<SortingRule>
        {
            new SortingRule
            {
                RuleId = "R1",
                RuleName = "空条码规则",
                ConditionExpression = "Barcode == ''",
                TargetChute = "CHUTE-NOBARCODE",
                Priority = 1,
                IsEnabled = true
            }
        };

        _mockRuleRepository.Setup(r => r.GetEnabledRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        var parcelInfo = new ParcelInfo 
        { 
            ParcelId = "PKG001", 
            CartNumber = "CART001",
            Barcode = ""
        };
        var dwsData = new DwsData { Weight = 1000 };

        // Act
        var result = await _service.EvaluateRulesAsync(parcelInfo, dwsData, null);

        // Assert
        Assert.Equal("CHUTE-NOBARCODE", result);
    }

    /// <summary>
    /// 测试null DWS数据处理
    /// </summary>
    [Fact]
    public async Task EvaluateRulesAsync_NullDwsData_HandlesCorrectly()
    {
        // Arrange
        var rules = new List<SortingRule>
        {
            new SortingRule
            {
                RuleId = "R1",
                RuleName = "条码规则",
                ConditionExpression = "Barcode CONTAINS 'TEST'",
                TargetChute = "CHUTE-TEST",
                Priority = 1,
                IsEnabled = true
            }
        };

        _mockRuleRepository.Setup(r => r.GetEnabledRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        var parcelInfo = new ParcelInfo 
        { 
            ParcelId = "PKG001", 
            CartNumber = "CART001",
            Barcode = "TEST123"
        };

        // Act - 传入null DWS数据
        var result = await _service.EvaluateRulesAsync(parcelInfo, null, null);

        // Assert
        Assert.Equal("CHUTE-TEST", result);
    }

    /// <summary>
    /// 测试null ThirdPartyResponse数据处理
    /// </summary>
    [Fact]
    public async Task EvaluateRulesAsync_NullThirdPartyResponse_HandlesCorrectly()
    {
        // Arrange
        var rules = new List<SortingRule>
        {
            new SortingRule
            {
                RuleId = "R1",
                RuleName = "重量规则",
                ConditionExpression = "Weight > 500",
                TargetChute = "CHUTE-HEAVY",
                Priority = 1,
                IsEnabled = true
            }
        };

        _mockRuleRepository.Setup(r => r.GetEnabledRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        var parcelInfo = new ParcelInfo { ParcelId = "PKG001", CartNumber = "CART001" };
        var dwsData = new DwsData { Weight = 1000 };

        // Act - 传入null WcsApiResponse
        var result = await _service.EvaluateRulesAsync(parcelInfo, dwsData, null);

        // Assert
        Assert.Equal("CHUTE-HEAVY", result);
    }

    #endregion

    #region Exception Handling Tests

    /// <summary>
    /// 测试仓储异常处理
    /// </summary>
    [Fact]
    public async Task EvaluateRulesAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        _mockRuleRepository.Setup(r => r.GetEnabledRulesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        var parcelInfo = new ParcelInfo { ParcelId = "PKG001", CartNumber = "CART001" };
        var dwsData = new DwsData { Weight = 1000 };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => 
            _service.EvaluateRulesAsync(parcelInfo, dwsData, null));
    }

    /// <summary>
    /// 测试取消令牌处理
    /// </summary>
    [Fact]
    public async Task EvaluateRulesAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockRuleRepository.Setup(r => r.GetEnabledRulesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var parcelInfo = new ParcelInfo { ParcelId = "PKG001", CartNumber = "CART001" };
        var dwsData = new DwsData { Weight = 1000 };

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _service.EvaluateRulesAsync(parcelInfo, dwsData, null, cts.Token));
    }

    #endregion

    #region Concurrency Tests

    /// <summary>
    /// 测试并发缓存访问
    /// </summary>
    [Fact]
    public async Task EvaluateRulesAsync_ConcurrentAccess_HandlesCorrectly()
    {
        // Arrange
        var rules = new List<SortingRule>
        {
            new SortingRule
            {
                RuleId = "R1",
                RuleName = "并发测试规则",
                ConditionExpression = "Weight > 500",
                TargetChute = "CHUTE-CONCURRENT",
                Priority = 1,
                IsEnabled = true
            }
        };

        _mockRuleRepository.Setup(r => r.GetEnabledRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        var parcelInfo = new ParcelInfo { ParcelId = "PKG001", CartNumber = "CART001" };
        var dwsData = new DwsData { Weight = 1000 };

        // Act - 创建多个并发任务
        var tasks = Enumerable.Range(0, 10).Select(_ =>
            _service.EvaluateRulesAsync(parcelInfo, dwsData, null));

        var results = await Task.WhenAll(tasks);

        // Assert - 所有结果应该一致
        Assert.All(results, r => Assert.Equal("CHUTE-CONCURRENT", r));
    }

    #endregion
}
