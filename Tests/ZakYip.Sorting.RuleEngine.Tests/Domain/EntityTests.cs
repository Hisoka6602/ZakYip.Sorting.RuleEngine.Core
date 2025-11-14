using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.ValueObjects;
using ZakYip.Sorting.RuleEngine.Tests.Helpers;

namespace ZakYip.Sorting.RuleEngine.Tests.Domain;

/// <summary>
/// 排序规则实体单元测试
/// Unit tests for SortingRule entity
/// </summary>
public class SortingRuleTests
{
    [Fact]
    public void SortingRule_Creation_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var rule = TestDataBuilder.CreateSortingRule(
            ruleId: "R001",
            ruleName: "Weight Rule",
            conditionExpression: "Weight > 1000",
            targetChute: "CHUTE-A01",
            priority: 1,
            matchingMethod: MatchingMethodType.WeightMatch
        );

        // Assert
        Assert.Equal("R001", rule.RuleId);
        Assert.Equal("Weight Rule", rule.RuleName);
        Assert.Equal("Weight > 1000", rule.ConditionExpression);
        Assert.Equal("CHUTE-A01", rule.TargetChute);
        Assert.Equal(1, rule.Priority);
        Assert.Equal(MatchingMethodType.WeightMatch, rule.MatchingMethod);
        Assert.True(rule.IsEnabled);
    }

    [Fact]
    public void SortingRule_DefaultValues_AreSet()
    {
        // Arrange & Act
        var rule = new SortingRule
        {
            RuleId = "R001",
            RuleName = "Test",
            ConditionExpression = "TEST",
            TargetChute = "CHUTE-A01",
            Priority = 1
        };

        // Assert - Check that default properties are initialized
        Assert.NotNull(rule.RuleId);
        Assert.NotNull(rule.RuleName);
    }

    [Fact]
    public void SortingRule_Timestamps_CanBeSet()
    {
        // Arrange
        var now = DateTime.Now;
        var rule = TestDataBuilder.CreateSortingRule();

        // Act
        rule.CreatedAt = now;
        rule.UpdatedAt = now.AddMinutes(5);

        // Assert
        Assert.Equal(now, rule.CreatedAt);
        Assert.True(rule.UpdatedAt > rule.CreatedAt);
    }

    [Fact]
    public void SortingRule_MatchingMethodTypes_CanBeAssigned()
    {
        // Arrange
        var rule = TestDataBuilder.CreateSortingRule();

        // Act & Assert - Test different matching method types
        rule.MatchingMethod = MatchingMethodType.BarcodeRegex;
        Assert.Equal(MatchingMethodType.BarcodeRegex, rule.MatchingMethod);

        rule.MatchingMethod = MatchingMethodType.OcrMatch;
        Assert.Equal(MatchingMethodType.OcrMatch, rule.MatchingMethod);

        rule.MatchingMethod = MatchingMethodType.ApiResponseMatch;
        Assert.Equal(MatchingMethodType.ApiResponseMatch, rule.MatchingMethod);
    }
}

/// <summary>
/// DWS数据实体单元测试
/// Unit tests for DwsData entity
/// </summary>
public class DwsDataTests
{
    [Fact]
    public void DwsData_Creation_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var dwsData = TestDataBuilder.CreateDwsData(
            barcode: "BC123456",
            weight: 1500,
            length: 400,
            width: 300,
            height: 200
        );

        // Assert
        Assert.Equal("BC123456", dwsData.Barcode);
        Assert.Equal(1500, dwsData.Weight);
        Assert.Equal(400, dwsData.Length);
        Assert.Equal(300, dwsData.Width);
        Assert.Equal(200, dwsData.Height);
    }

    [Fact]
    public void DwsData_Volume_CanBeCalculated()
    {
        // Arrange & Act
        var dwsData = TestDataBuilder.CreateDwsData(
            length: 300,
            width: 200,
            height: 150
        );

        // Assert - Volume should be calculated from dimensions
        Assert.True(dwsData.Volume > 0);
        Assert.Equal(9000, dwsData.Volume); // (300 * 200 * 150) / 1000
    }

    [Fact]
    public void DwsData_CustomVolume_CanBeSet()
    {
        // Arrange & Act
        var dwsData = TestDataBuilder.CreateDwsData(
            volume: 15000
        );

        // Assert
        Assert.Equal(15000, dwsData.Volume);
    }
}

/// <summary>
/// 包裹信息实体单元测试
/// Unit tests for ParcelInfo entity
/// </summary>
public class ParcelInfoTests
{
    [Fact]
    public void ParcelInfo_Creation_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var parcel = TestDataBuilder.CreateParcelInfo(
            parcelId: "PKG001",
            cartNumber: "CART001",
            barcode: "BC123456",
            status: ParcelStatus.Processing
        );

        // Assert
        Assert.Equal("PKG001", parcel.ParcelId);
        Assert.Equal("CART001", parcel.CartNumber);
        Assert.Equal("BC123456", parcel.Barcode);
        Assert.Equal(ParcelStatus.Processing, parcel.Status);
    }

    [Fact]
    public void ParcelInfo_StatusTransitions_Work()
    {
        // Arrange
        var parcel = TestDataBuilder.CreateParcelInfo(status: ParcelStatus.Pending);

        // Act & Assert
        Assert.Equal(ParcelStatus.Pending, parcel.Status);

        parcel.Status = ParcelStatus.Processing;
        Assert.Equal(ParcelStatus.Processing, parcel.Status);

        parcel.Status = ParcelStatus.Completed;
        Assert.Equal(ParcelStatus.Completed, parcel.Status);
    }

    [Fact]
    public void ParcelInfo_ChuteNumber_CanBeAssigned()
    {
        // Arrange
        var parcel = TestDataBuilder.CreateParcelInfo();

        // Act
        parcel.ChuteNumber = "CHUTE-A01";

        // Assert
        Assert.Equal("CHUTE-A01", parcel.ChuteNumber);
    }
}

/// <summary>
/// OCR数据实体单元测试
/// Unit tests for OcrData entity
/// </summary>
public class OcrDataTests
{
    [Fact]
    public void OcrData_Creation_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var ocrData = TestDataBuilder.CreateOcrData(
            firstSegmentCode: "64",
            secondSegmentCode: "12",
            thirdSegmentCode: "34"
        );

        // Assert
        Assert.Equal("64", ocrData.FirstSegmentCode);
        Assert.Equal("12", ocrData.SecondSegmentCode);
        Assert.Equal("34", ocrData.ThirdSegmentCode);
        Assert.Equal("641234", ocrData.ThreeSegmentCode);
    }

    [Fact]
    public void OcrData_AddressFields_CanBeSet()
    {
        // Arrange & Act
        var ocrData = TestDataBuilder.CreateOcrData(
            recipientAddress: "123 Main St",
            senderAddress: "456 Oak Ave"
        );

        // Assert
        Assert.Equal("123 Main St", ocrData.RecipientAddress);
        Assert.Equal("456 Oak Ave", ocrData.SenderAddress);
    }

    [Fact]
    public void OcrData_PhoneSuffixes_CanBeSet()
    {
        // Arrange
        var ocrData = TestDataBuilder.CreateOcrData();

        // Assert
        Assert.NotNull(ocrData.RecipientPhoneSuffix);
        Assert.NotNull(ocrData.SenderPhoneSuffix);
    }
}

/// <summary>
/// 第三方响应实体单元测试
/// Unit tests for WcsApiResponse entity
/// </summary>
public class ThirdPartyResponseTests
{
    [Fact]
    public void ThirdPartyResponse_Creation_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var response = TestDataBuilder.CreateThirdPartyResponse(
            success: true,
            code: "200",
            message: "Success",
            data: "Test Data"
        );

        // Assert
        Assert.True(response.Success);
        Assert.Equal("200", response.Code);
        Assert.Equal("Success", response.Message);
        Assert.Equal("Test Data", response.Data);
    }

    [Fact]
    public void ThirdPartyResponse_WithOcrData_SetsOcrCorrectly()
    {
        // Arrange
        var ocrData = TestDataBuilder.CreateOcrData();

        // Act
        var response = TestDataBuilder.CreateThirdPartyResponse(
            ocrData: ocrData
        );

        // Assert
        Assert.NotNull(response.OcrData);
        Assert.Equal(ocrData.ThreeSegmentCode, response.OcrData.ThreeSegmentCode);
    }
}

/// <summary>
/// 测试数据构建器测试
/// Tests for TestDataBuilder helper
/// </summary>
public class TestDataBuilderTests
{
    [Fact]
    public void TestDataBuilder_CreateMultipleRules_CreatesCorrectCount()
    {
        // Arrange & Act
        var rules = TestDataBuilder.CreateMultipleRules(5);

        // Assert
        Assert.Equal(5, rules.Count);
        Assert.All(rules, r => Assert.NotNull(r.RuleId));
        Assert.All(rules, r => Assert.NotNull(r.RuleName));
    }

    [Fact]
    public void TestDataBuilder_CreateMultipleRules_HasUniquePriorities()
    {
        // Arrange & Act
        var rules = TestDataBuilder.CreateMultipleRules(3);

        // Assert
        Assert.Equal(1, rules[0].Priority);
        Assert.Equal(2, rules[1].Priority);
        Assert.Equal(3, rules[2].Priority);
    }

    [Fact]
    public void TestDataBuilder_CreateMultipleParcels_CreatesCorrectCount()
    {
        // Arrange & Act
        var parcels = TestDataBuilder.CreateMultipleParcels(10);

        // Assert
        Assert.Equal(10, parcels.Count);
        Assert.All(parcels, p => Assert.NotNull(p.ParcelId));
        Assert.All(parcels, p => Assert.NotNull(p.CartNumber));
    }

    [Fact]
    public void TestDataBuilder_CreateMultipleParcels_HasUniqueIds()
    {
        // Arrange & Act
        var parcels = TestDataBuilder.CreateMultipleParcels(5);

        // Assert
        var uniqueIds = parcels.Select(p => p.ParcelId).Distinct().Count();
        Assert.Equal(5, uniqueIds);
    }
}

/// <summary>
/// 通信日志实体单元测试
/// Unit tests for communication log entities
/// </summary>
public class CommunicationLogEntityTests
{
    [Fact]
    public void DwsCommunicationLog_CommunicationType_CanBeSet()
    {
        // Arrange & Act
        var log = new DwsCommunicationLog
        {
            CommunicationType = CommunicationType.Mqtt,
            DwsAddress = "192.168.1.100:1883",
            OriginalContent = "Test DWS Data",
            IsSuccess = true
        };

        // Assert
        Assert.Equal(CommunicationType.Mqtt, log.CommunicationType);
        Assert.Equal("192.168.1.100:1883", log.DwsAddress);
    }

    [Fact]
    public void DwsCommunicationLog_AllCommunicationTypes_CanBeAssigned()
    {
        // Arrange
        var log = new DwsCommunicationLog();

        // Act & Assert - Test all communication types
        log.CommunicationType = CommunicationType.Tcp;
        Assert.Equal(CommunicationType.Tcp, log.CommunicationType);

        log.CommunicationType = CommunicationType.SignalR;
        Assert.Equal(CommunicationType.SignalR, log.CommunicationType);

        log.CommunicationType = CommunicationType.Http;
        Assert.Equal(CommunicationType.Http, log.CommunicationType);

        log.CommunicationType = CommunicationType.Mqtt;
        Assert.Equal(CommunicationType.Mqtt, log.CommunicationType);
    }

    [Fact]
    public void SorterCommunicationLog_CommunicationType_CanBeSet()
    {
        // Arrange & Act
        var log = new SorterCommunicationLog
        {
            CommunicationType = CommunicationType.Tcp,
            SorterAddress = "192.168.1.50:5000",
            OriginalContent = "Test Sorter Data",
            IsSuccess = true
        };

        // Assert
        Assert.Equal(CommunicationType.Tcp, log.CommunicationType);
        Assert.Equal("192.168.1.50:5000", log.SorterAddress);
    }

    [Fact]
    public void ApiCommunicationLog_CommunicationType_DefaultsToHttp()
    {
        // Arrange & Act
        var log = new ApiCommunicationLog
        {
            ParcelId = "PKG001",
            RequestUrl = "http://api.example.com/chute",
            IsSuccess = true
        };

        // Assert - Should default to HTTP
        Assert.Equal(CommunicationType.Http, log.CommunicationType);
    }

    [Fact]
    public void ApiCommunicationLog_CommunicationType_CanBeChanged()
    {
        // Arrange
        var log = new ApiCommunicationLog
        {
            ParcelId = "PKG001"
        };

        // Act
        log.CommunicationType = CommunicationType.SignalR;

        // Assert
        Assert.Equal(CommunicationType.SignalR, log.CommunicationType);
    }

    [Fact]
    public void CommunicationLog_CommunicationType_CanBeSet()
    {
        // Arrange & Act
        var log = new CommunicationLog
        {
            CommunicationType = CommunicationType.Mqtt,
            Direction = CommunicationDirection.Inbound,
            Message = "Test communication",
            IsSuccess = true
        };

        // Assert
        Assert.Equal(CommunicationType.Mqtt, log.CommunicationType);
        Assert.Equal(CommunicationDirection.Inbound, log.Direction);
    }
}

// Image-related tests
public class DwsDataWithImagesTests
{
    [Fact]
    public void DwsData_ShouldInitializeImagesCollection()
    {
        // Act
        var dwsData = new DwsData();

        // Assert
        Assert.NotNull(dwsData.Images);
        Assert.Empty(dwsData.Images);
    }

    [Fact]
    public void DwsData_ShouldAllowAddingImages()
    {
        // Arrange
        var dwsData = new DwsData
        {
            Barcode = "TEST123",
            Weight = 100.5m,
            Length = 200,
            Width = 150,
            Height = 100,
            Volume = 3000000
        };

        var image1 = new ImageInfo { DeviceName = "Camera01", LocalPath = @"D:\images\image001.jpg" };
        var image2 = new ImageInfo { DeviceName = "Camera02", LocalPath = @"D:\images\image002.jpg" };

        // Act
        dwsData.Images.Add(image1);
        dwsData.Images.Add(image2);

        // Assert
        Assert.Equal(2, dwsData.Images.Count);
        Assert.Equal("Camera01", dwsData.Images[0].DeviceName);
        Assert.Equal(@"D:\images\image001.jpg", dwsData.Images[0].LocalPath);
        Assert.Equal("Camera02", dwsData.Images[1].DeviceName);
        Assert.Equal(@"D:\images\image002.jpg", dwsData.Images[1].LocalPath);
    }

    [Fact]
    public void DwsData_WithMultipleImages_ShouldMaintainOrder()
    {
        // Arrange
        var dwsData = new DwsData();
        var images = new[]
        {
            new ImageInfo { DeviceName = "Camera01", LocalPath = @"D:\img1.jpg" },
            new ImageInfo { DeviceName = "Camera02", LocalPath = @"D:\img2.jpg" },
            new ImageInfo { DeviceName = "Camera03", LocalPath = @"D:\img3.jpg" },
            new ImageInfo { DeviceName = "Camera04", LocalPath = @"D:\img4.jpg" },
            new ImageInfo { DeviceName = "Camera05", LocalPath = @"D:\img5.jpg" }
        };

        // Act
        foreach (var img in images)
        {
            dwsData.Images.Add(img);
        }

        // Assert
        Assert.Equal(5, dwsData.Images.Count);
        for (int i = 0; i < images.Length; i++)
        {
            Assert.Equal(images[i].DeviceName, dwsData.Images[i].DeviceName);
            Assert.Equal(images[i].LocalPath, dwsData.Images[i].LocalPath);
        }
    }
}
