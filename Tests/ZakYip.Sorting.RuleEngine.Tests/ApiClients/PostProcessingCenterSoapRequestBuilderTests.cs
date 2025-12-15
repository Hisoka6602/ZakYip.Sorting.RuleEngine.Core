using System.Xml.Linq;
using ZakYip.Sorting.RuleEngine.Tests.Mocks;
using Xunit;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.Shared;

namespace ZakYip.Sorting.RuleEngine.Tests.ApiClients;

public class PostProcessingCenterSoapRequestBuilderTests
{
    private readonly PostalSoapRequestBuilder _soapRequestBuilder;

    public PostProcessingCenterSoapRequestBuilderTests()
    {
        _soapRequestBuilder = new PostalSoapRequestBuilder();
    }

    [Fact]
    public void BuildScanRequest_ShouldGenerateValidSoapEnvelope()
    {
        // Arrange
        var parameters = new PostalScanRequestParameters
        {
            DeviceId = "20140010",
            Barcode = "TEST123456",
            EmployeeNumber = "00818684",
            ScanTime = new DateTime(2024, 1, 15, 10, 30, 45)
        };

        // Act
        var result = _soapRequestBuilder.BuildScanRequest(parameters);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("soapenv:Envelope", result);
        Assert.Contains("web:getYJSM", result);
        
        // Verify the structure is valid XML
        var xml = XDocument.Parse(result);
        Assert.NotNull(xml);
    }

    [Fact]
    public void BuildScanRequest_ShouldContainCorrectArgument()
    {
        // Arrange
        var parameters = new PostalScanRequestParameters
        {
            DeviceId = "20140010",
            Barcode = "TEST123456",
            EmployeeNumber = "00818684",
            ScanTime = new DateTime(2024, 1, 15, 10, 30, 45)
        };

        // Act
        var result = _soapRequestBuilder.BuildScanRequest(parameters);

        // Assert
        Assert.Contains("#HEAD::", result);
        Assert.Contains("20140010", result);
        Assert.Contains("TEST123456", result);
        Assert.Contains("00818684", result);
        Assert.Contains("20240115103045", result);
        Assert.Contains("||#END", result);
    }

    [Fact]
    public void BuildScanRequest_ShouldUseDefaultValues()
    {
        // Arrange
        var parameters = new PostalScanRequestParameters
        {
            DeviceId = "20140010",
            Barcode = "TEST123456",
            EmployeeNumber = "00818684",
            ScanTime = new DateTime(2024, 1, 15, 10, 30, 45)
            // ScanType, OperationType, StartCode, EndCode, AdditionalFields use defaults
        };

        // Act
        var result = _soapRequestBuilder.BuildScanRequest(parameters);

        // Assert
        // Default values: ScanType=2, OperationType=001, StartCode=0000, EndCode=0000
        Assert.Contains("::2::", result);
        Assert.Contains("::001::", result);
        Assert.Contains("::0000::", result);
    }

    [Fact]
    public void BuildChuteQueryRequest_ShouldGenerateValidSoapEnvelope()
    {
        // Arrange
        var parameters = new PostalChuteQueryRequestParameters
        {
            SequenceId = "202401WS20140010FJ000000001",
            DeviceId = "20140010",
            Barcode = "TEST123456",
            ScanTime = new DateTime(2024, 1, 15, 10, 30, 45),
            EmployeeNumber = "00818684",
            OrganizationNumber = "20140011",
            CompanyName = "广东泽业科技有限公司",
            DeviceBarcode = "141562320001131"
        };

        // Act
        var result = _soapRequestBuilder.BuildChuteQueryRequest(parameters);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("soapenv:Envelope", result);
        Assert.Contains("web:getLTGKCX", result);
        
        // Verify the structure is valid XML
        var xml = XDocument.Parse(result);
        Assert.NotNull(xml);
    }

    [Fact]
    public void BuildChuteQueryRequest_ShouldContainCorrectArgument()
    {
        // Arrange
        var parameters = new PostalChuteQueryRequestParameters
        {
            SequenceId = "202401WS20140010FJ000000001",
            DeviceId = "20140010",
            Barcode = "TEST123456",
            ScanTime = new DateTime(2024, 1, 15, 10, 30, 45),
            EmployeeNumber = "00818684",
            OrganizationNumber = "20140011",
            CompanyName = "广东泽业科技有限公司",
            DeviceBarcode = "141562320001131"
        };

        // Act
        var result = _soapRequestBuilder.BuildChuteQueryRequest(parameters);

        // Assert
        Assert.Contains("#HEAD::", result);
        Assert.Contains("202401WS20140010FJ000000001", result);
        Assert.Contains("20140010", result);
        Assert.Contains("TEST123456", result);
        Assert.Contains("2024-01-15 10:30:45", result);
        Assert.Contains("00818684", result);
        Assert.Contains("20140011", result);
        Assert.Contains("广东泽业科技有限公司", result);
        Assert.Contains("141562320001131", result);
        Assert.Contains("||#END", result);
    }

    [Fact]
    public void BuildChuteQueryRequest_ShouldUseDefaultValues()
    {
        // Arrange
        var parameters = new PostalChuteQueryRequestParameters
        {
            SequenceId = "202401WS20140010FJ000000001",
            DeviceId = "20140010",
            Barcode = "TEST123456",
            ScanTime = new DateTime(2024, 1, 15, 10, 30, 45),
            EmployeeNumber = "00818684",
            OrganizationNumber = "20140011",
            CompanyName = "广东泽业科技有限公司",
            DeviceBarcode = "141562320001131"
            // Flag, ReservedField1-4 use defaults
        };

        // Act
        var result = _soapRequestBuilder.BuildChuteQueryRequest(parameters);

        // Assert
        // Default values: Flag=0, ReservedFields=" "
        Assert.Contains("::0::", result);
    }

    [Fact]
    public void BuildScanRequest_WithCustomValues_ShouldUseProvidedValues()
    {
        // Arrange
        var parameters = new PostalScanRequestParameters
        {
            DeviceId = "20140010",
            Barcode = "TEST123456",
            EmployeeNumber = "00818684",
            ScanTime = new DateTime(2024, 1, 15, 10, 30, 45),
            ScanType = "3",
            OperationType = "002",
            StartCode = "1111",
            EndCode = "2222",
            AdditionalFields = new[] { "1", "2", "3", "4", "5", "6", "7" }
        };

        // Act
        var result = _soapRequestBuilder.BuildScanRequest(parameters);

        // Assert
        Assert.Contains("::3::", result);
        Assert.Contains("::002::", result);
        Assert.Contains("::1111::", result);
        Assert.Contains("::2222::", result);
        Assert.Contains("::1::2::3::4::5::6::7", result);
    }
}
