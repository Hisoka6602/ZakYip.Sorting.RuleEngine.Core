using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.Shared;

/// <summary>
/// 通用SOAP请求构建器，用于优雅地构建SOAP XML请求
/// Shared SOAP request builder for elegant construction of SOAP XML requests
/// 用于邮政分揽投机构和邮政处理中心
/// </summary>
public class PostalSoapRequestBuilder
{
    private const string SoapEnvelopeNamespace = "http://schemas.xmlsoap.org/soap/envelope/";
    private const string WebServiceNamespace = "http://serverNs.webservice.pcs.jdpt.chinapost.cn/";

    /// <summary>
    /// 构建扫描包裹的SOAP请求 (getYJSM)
    /// Build SOAP request for scanning parcel
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string BuildScanRequest(PostalScanRequestParameters parameters)
    {
        var arg0 = new StringBuilder()
            .Append("#HEAD::")
            .Append(parameters.DeviceId).Append("::")
            .Append(parameters.Barcode).Append("::")
            .Append(parameters.EmployeeNumber).Append("::")
            .Append(parameters.ScanTime.ToString("yyyyMMddHHmmss")).Append("::")
            .Append(parameters.ScanType).Append("::")
            .Append(parameters.OperationType).Append("::")
            .Append(parameters.StartCode).Append("::")
            .Append(parameters.EndCode).Append("::")
            .Append(string.Join("::", parameters.AdditionalFields))
            .Append("||#END")
            .ToString();

        return BuildSoapEnvelope("getYJSM", arg0);
    }

    /// <summary>
    /// 构建查询格口的SOAP请求 (getLTGKCX)
    /// Build SOAP request for querying chute
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string BuildChuteQueryRequest(PostalChuteQueryRequestParameters parameters)
    {
        var arg0 = new StringBuilder()
            .Append("#HEAD::")
            .Append(parameters.SequenceId).Append("::")
            .Append(parameters.DeviceId).Append("::")
            .Append(parameters.Barcode).Append("::")
            .Append(parameters.Flag).Append("::")
            .Append(parameters.ReservedField1).Append("::")
            .Append(parameters.ReservedField2).Append("::")
            .Append(parameters.ReservedField3).Append("::")
            .Append(parameters.ScanTime.ToString("yyyy-MM-dd HH:mm:ss")).Append("::")
            .Append(parameters.EmployeeNumber).Append("::")
            .Append(parameters.OrganizationNumber).Append("::")
            .Append(parameters.CompanyName).Append("::")
            .Append(parameters.DeviceBarcode).Append("::")
            .Append(parameters.ReservedField4)
            .Append("||#END")
            .ToString();

        return BuildSoapEnvelope("getLTGKCX", arg0);
    }

    /// <summary>
    /// 构建SOAP信封
    /// Build SOAP envelope
    /// </summary>
    private string BuildSoapEnvelope(string methodName, string arg0Value)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "    ",
            OmitXmlDeclaration = true,
            Encoding = Encoding.UTF8
        };

        var sb = new StringBuilder();
        using (var writer = XmlWriter.Create(sb, settings))
        {
            // <soapenv:Envelope>
            writer.WriteStartElement("soapenv", "Envelope", SoapEnvelopeNamespace);
            writer.WriteAttributeString("xmlns", "web", null, WebServiceNamespace);

            // <soapenv:Header />
            writer.WriteStartElement("Header", SoapEnvelopeNamespace);
            writer.WriteEndElement();

            // <soapenv:Body>
            writer.WriteStartElement("Body", SoapEnvelopeNamespace);

            // <web:methodName>
            writer.WriteStartElement("web", methodName, WebServiceNamespace);

            // <arg0>value</arg0>
            writer.WriteElementString("arg0", arg0Value);

            // </web:methodName>
            writer.WriteEndElement();

            // </soapenv:Body>
            writer.WriteEndElement();

            // </soapenv:Envelope>
            writer.WriteEndElement();

            writer.Flush();
        }

        return sb.ToString();
    }
}

/// <summary>
/// 扫描请求参数
/// Scan request parameters
/// </summary>
public class PostalScanRequestParameters
{
    public required string DeviceId { get; init; }
    public required string Barcode { get; init; }
    public required string EmployeeNumber { get; init; }
    public required DateTime ScanTime { get; init; }
    public string ScanType { get; init; } = "2";
    public string OperationType { get; init; } = "001";
    public string StartCode { get; init; } = "0000";
    public string EndCode { get; init; } = "0000";
    public string[] AdditionalFields { get; init; } = new[] { "0", "0", "0", "0", "0", "0", "0" };
}

/// <summary>
/// 格口查询请求参数
/// Chute query request parameters
/// </summary>
public class PostalChuteQueryRequestParameters
{
    public required string SequenceId { get; init; }
    public required string DeviceId { get; init; }
    public required string Barcode { get; init; }
    public string Flag { get; init; } = "0";
    public string ReservedField1 { get; init; } = " ";
    public string ReservedField2 { get; init; } = " ";
    public string ReservedField3 { get; init; } = " ";
    public required DateTime ScanTime { get; init; }
    public required string EmployeeNumber { get; init; }
    public required string OrganizationNumber { get; init; }
    public required string CompanyName { get; init; }
    public required string DeviceBarcode { get; init; }
    public string ReservedField4 { get; init; } = "";
}
