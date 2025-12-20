using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Service.API;

namespace ZakYip.Sorting.RuleEngine.Tests.Controllers;

/// <summary>
/// 包裹控制器测试
/// Parcel controller tests
/// </summary>
public class ParcelControllerTests
{
    private readonly Mock<IParcelInfoRepository> _mockParcelRepository;
    private readonly Mock<IParcelLifecycleNodeRepository> _mockLifecycleRepository;
    private readonly Mock<ILogger<ParcelController>> _mockLogger;
    private readonly ParcelController _controller;

    public ParcelControllerTests()
    {
        _mockParcelRepository = new Mock<IParcelInfoRepository>();
        _mockLifecycleRepository = new Mock<IParcelLifecycleNodeRepository>();
        _mockLogger = new Mock<ILogger<ParcelController>>();
        _controller = new ParcelController(
            _mockParcelRepository.Object,
            _mockLifecycleRepository.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetParcel_ExistingParcel_ReturnsOkResult()
    {
        // Arrange
        var parcel = new ParcelInfo
        {
            ParcelId = "TEST001",
            CartNumber = "CART001",
            Status = ParcelStatus.Completed
        };

        _mockParcelRepository.Setup(x => x.GetByIdAsync("TEST001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(parcel);

        // Act
        var result = await _controller.GetParcel("TEST001");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedParcel = Assert.IsType<ParcelInfo>(okResult.Value);
        Assert.Equal("TEST001", returnedParcel.ParcelId);
    }

    [Fact]
    public async Task GetParcel_NonExistentParcel_ReturnsNotFound()
    {
        // Arrange
        _mockParcelRepository.Setup(x => x.GetByIdAsync("NONEXISTENT", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParcelInfo?)null);

        // Act
        var result = await _controller.GetParcel("NONEXISTENT");

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task SearchParcels_ValidParameters_ReturnsOkResult()
    {
        // Arrange
        var parcels = new List<ParcelInfo>
        {
            new() { ParcelId = "P1", CartNumber = "C1", Status = ParcelStatus.Pending },
            new() { ParcelId = "P2", CartNumber = "C2", Status = ParcelStatus.Pending }
        };

        _mockParcelRepository.Setup(x => x.SearchAsync(
            ParcelStatus.Pending, null, null, null, null, 1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync((parcels, 2));

        // Act
        var result = await _controller.SearchParcels(status: ParcelStatus.Pending);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ParcelSearchResponse>(okResult.Value);
        Assert.Equal(2, response.TotalCount);
        Assert.Equal(2, response.Items.Count);
    }

    [Fact]
    public async Task SearchParcels_InvalidPage_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.SearchParcels(page: 0);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task SearchParcels_InvalidPageSize_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.SearchParcels(pageSize: 2000);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetParcelLifecycle_ExistingParcel_ReturnsLifecycleNodes()
    {
        // Arrange
        var parcel = new ParcelInfo { ParcelId = "TEST001", CartNumber = "CART001" };
        var nodes = new List<ParcelLifecycleNodeEntity>
        {
            new() { ParcelId = "TEST001", Stage = ParcelLifecycleStage.Created, EventTime = DateTime.Now.AddHours(-2) },
            new() { ParcelId = "TEST001", Stage = ParcelLifecycleStage.DwsReceived, EventTime = DateTime.Now.AddHours(-1) },
            new() { ParcelId = "TEST001", Stage = ParcelLifecycleStage.Landed, EventTime = DateTime.Now }
        };

        _mockParcelRepository.Setup(x => x.GetByIdAsync("TEST001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(parcel);
        _mockLifecycleRepository.Setup(x => x.GetByParcelIdAsync("TEST001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(nodes);

        // Act
        var result = await _controller.GetParcelLifecycle("TEST001");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedNodes = Assert.IsAssignableFrom<IReadOnlyList<ParcelLifecycleNodeEntity>>(okResult.Value);
        Assert.Equal(3, returnedNodes.Count);
    }

    [Fact]
    public async Task GetParcelLifecycle_NonExistentParcel_ReturnsNotFound()
    {
        // Arrange
        _mockParcelRepository.Setup(x => x.GetByIdAsync("NONEXISTENT", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParcelInfo?)null);

        // Act
        var result = await _controller.GetParcelLifecycle("NONEXISTENT");

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }
}
