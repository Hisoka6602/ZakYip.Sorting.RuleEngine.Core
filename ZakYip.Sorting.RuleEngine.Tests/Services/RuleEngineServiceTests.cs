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
}
