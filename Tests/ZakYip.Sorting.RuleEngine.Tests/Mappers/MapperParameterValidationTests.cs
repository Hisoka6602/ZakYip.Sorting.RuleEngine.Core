using Xunit;
using ZakYip.Sorting.RuleEngine.Application.Mappers;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Requests;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Tests.Mocks;

namespace ZakYip.Sorting.RuleEngine.Tests.Mappers;

/// <summary>
/// 映射器参数验证测试 / Mapper parameter validation tests
/// Tests that all mapper methods properly validate null parameters
/// </summary>
public class MapperParameterValidationTests
{
    [Fact]
    public void EntityToDtoMapper_ToResponseDto_SortingRule_Should_ThrowArgumentNullException_When_EntityIsNull()
    {
        // Arrange
        SortingRule? nullEntity = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullEntity!.ToResponseDto());
    }

    [Fact]
    public void EntityToDtoMapper_ToResponseDto_Chute_Should_ThrowArgumentNullException_When_EntityIsNull()
    {
        // Arrange
        Chute? nullEntity = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullEntity!.ToResponseDto());
    }

    [Fact]
    public void SorterConfigMapper_ToResponseDto_Should_ThrowArgumentNullException_When_EntityIsNull()
    {
        // Arrange
        SorterConfig? nullEntity = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullEntity!.ToResponseDto());
    }

    [Fact]
    public void SorterConfigMapper_ToEntity_Should_ThrowArgumentNullException_When_RequestIsNull()
    {
        // Arrange
        SorterConfigUpdateRequest? nullRequest = null;
        var clock = new MockSystemClock();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullRequest!.ToEntity(clock));
    }

    [Fact]
    public void DwsConfigMapper_ToResponseDto_Should_ThrowArgumentNullException_When_EntityIsNull()
    {
        // Arrange
        DwsConfig? nullEntity = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => DwsConfigMapper.ToResponseDto(nullEntity!));
    }

    [Fact]
    public void DwsConfigMapper_ToEntity_Should_ThrowArgumentNullException_When_RequestIsNull()
    {
        // Arrange
        DwsConfigUpdateRequest? nullRequest = null;
        var clock = new MockSystemClock();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => DwsConfigMapper.ToEntity(nullRequest!, clock));
    }

    [Fact]
    public void DwsDataTemplateMapper_ToResponseDto_Should_ThrowArgumentNullException_When_EntityIsNull()
    {
        // Arrange
        DwsDataTemplate? nullEntity = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => DwsDataTemplateMapper.ToResponseDto(nullEntity!));
    }

    [Fact]
    public void DwsDataTemplateMapper_ToEntity_Should_ThrowArgumentNullException_When_RequestIsNull()
    {
        // Arrange
        DwsDataTemplateUpdateRequest? nullRequest = null;
        var clock = new MockSystemClock();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => DwsDataTemplateMapper.ToEntity(nullRequest!, clock));
    }
}
