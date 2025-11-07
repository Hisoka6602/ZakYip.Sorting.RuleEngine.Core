using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Constants;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.PostCollection;

/// <summary>
/// 邮政分揽投机构API客户端实现
/// Postal Collection/Delivery Institution API client implementation
/// 参考: https://gist.github.com/Hisoka6602/dc321e39f3dbece14129d28e65480a8e (PostInApi)
/// 使用SOAP协议进行通信
/// </summary>
public class PostCollectionApiClient : IWcsApiAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PostCollectionApiClient> _logger;
    private static long _sequenceNumber = 1;
    private readonly object _sequenceLock = new();

    // Configuration parameters - should be injected via options pattern in production
    private const string WorkshopCode = "WS20140010";
    private const string DeviceId = "20140010";
    private const string CompanyName = "广东泽业科技有限公司";
    private const string DeviceBarcode = "141562320001131";
    private const string OrganizationNumber = "20140011";
    private const string EmployeeNumber = "00818684";

    public PostCollectionApiClient(
        HttpClient httpClient,
        ILogger<PostCollectionApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    private long GetNextSequenceNumber()
    {
        lock (_sequenceLock)
        {
            return _sequenceNumber++;
        }
    }

    /// <summary>
    /// 扫描包裹到邮政分揽投机构系统
    /// Scan parcel to register it in the postal collection institution system
    /// 对应参考代码中的 SubmitScanInfo 方法 (getYJSM)
    /// </summary>
    public async Task<WcsApiResponse> ScanParcelAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        var requestTime = DateTime.Now;
        
        try
        {
            // Skip processing for "NoRead" barcodes
            if (barcode.Contains("NoRead", StringComparison.InvariantCultureIgnoreCase))
            {
                _logger.LogDebug("跳过NoRead条码扫描: {Barcode}", barcode);
                return new WcsApiResponse
                {
                    Success = true,
                    Code = ApiConstants.HttpStatusCodes.Success,
                    Message = "NoRead barcode skipped",
                    Data = "NoRead barcode skipped",
                    RequestTime = requestTime,
                    ResponseTime = DateTime.Now,
                    DurationMs = 0
                };
            }

            _logger.LogDebug("开始扫描包裹到邮政分揽投机构，条码: {Barcode}", barcode);

            // 构造SOAP请求 - getYJSM方法
            var soapRequest = $@"
<soapenv:Envelope xmlns:web=""http://serverNs.webservice.pcs.jdpt.chinapost.cn/"" xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"">
    <soapenv:Header />
    <soapenv:Body>
        <web:getYJSM>
            <arg0>#HEAD::{DeviceId}::{barcode}::{EmployeeNumber}::{DateTime.Now:yyyyMMddHHmmss}::2::001::0000::0000::0::0::0::0::0::0::0||#END</arg0>
        </web:getYJSM>
    </soapenv:Body>
</soapenv:Envelope>";

            var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");

            // 发送SOAP请求
            var response = await _httpClient.PostAsync("", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            responseContent = Regex.Unescape(responseContent);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "扫描包裹成功（邮政分揽投机构），条码: {Barcode}, 状态码: {StatusCode}",
                    barcode, response.StatusCode);

                return new WcsApiResponse
                {
                    Success = true,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = "Parcel scanned successfully at collection institution",
                    Data = responseContent,
                    RequestBody = soapRequest,
                    ResponseBody = responseContent,
                    RequestTime = requestTime,
                    ResponseTime = DateTime.Now,
                    ResponseStatusCode = (int)response.StatusCode
                };
            }
            else
            {
                _logger.LogWarning(
                    "扫描包裹失败（邮政分揽投机构），条码: {Barcode}, 状态码: {StatusCode}, 响应: {Response}",
                    barcode, response.StatusCode, responseContent);

                return new WcsApiResponse
                {
                    Success = false,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = $"Scan Error: {response.StatusCode}",
                    Data = responseContent,
                    RequestBody = soapRequest,
                    ResponseBody = responseContent,
                    ErrorMessage = $"Scan Error: {response.StatusCode}",
                    RequestTime = requestTime,
                    ResponseTime = DateTime.Now,
                    ResponseStatusCode = (int)response.StatusCode
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "扫描包裹异常（邮政分揽投机构），条码: {Barcode}", barcode);

            return new WcsApiResponse
            {
                Success = false,
                Code = ApiConstants.HttpStatusCodes.Error,
                Message = ex.Message,
                Data = ex.ToString(),
                ErrorMessage = ex.Message,
                RequestTime = requestTime,
                ResponseTime = DateTime.Now
            };
        }
    }

    /// <summary>
    /// 请求格口号（查询包裹信息并返回格口）
    /// Request a chute/gate number for the parcel (query parcel info and return chute)
    /// 对应参考代码中的 UploadData 方法 (getLTGKCX)
    /// </summary>
    public async Task<WcsApiResponse> RequestChuteAsync(
        string parcelId,
        DwsData dwsData,
        OcrData? ocrData = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var requestTime = DateTime.Now;
        
        try
        {
            _logger.LogDebug("开始请求格口（邮政分揽投机构），包裹ID: {ParcelId}, 条码: {Barcode}", parcelId, dwsData.Barcode);

            // 先提交扫描信息（对应参考代码中UploadData内部调用SubmitScanInfo）
            await ScanParcelAsync(dwsData.Barcode, cancellationToken);

            var seqNum = GetNextSequenceNumber();
            var yearMonth = DateTime.Now.ToString("yyyyMM");
            var sequenceId = $"{yearMonth}{WorkshopCode}FJ{seqNum.ToString().PadLeft(9, '0')}";

            // 构造SOAP请求 - getLTGKCX方法（查询格口）
            var soapRequest = $@"
<soapenv:Envelope xmlns:web=""http://serverNs.webservice.pcs.jdpt.chinapost.cn/"" xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"">
    <soapenv:Header />
    <soapenv:Body>
        <web:getLTGKCX>
            <arg0>#HEAD::{sequenceId}::{DeviceId}::{dwsData.Barcode}::0:: :: :: ::{DateTime.Now:yyyy-MM-dd HH:mm:ss}::{EmployeeNumber}::{OrganizationNumber}::{CompanyName}::{DeviceBarcode}::||#END</arg0>
        </web:getLTGKCX>
    </soapenv:Body>
</soapenv:Envelope>";

            var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");

            // 发送SOAP请求
            var response = await _httpClient.PostAsync("", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            responseContent = Regex.Unescape(responseContent);
            
            stopwatch.Stop();

            // 解析响应，提取格口信息
            var chute = ExtractChuteFromResponse(responseContent);
            var isSuccess = !string.IsNullOrEmpty(chute);

            if (response.IsSuccessStatusCode && isSuccess)
            {
                _logger.LogInformation(
                    "请求格口成功（邮政分揽投机构），包裹ID: {ParcelId}, 条码: {Barcode}, 格口: {Chute}, 状态码: {StatusCode}, 耗时: {Duration}ms",
                    parcelId, dwsData.Barcode, chute, response.StatusCode, stopwatch.ElapsedMilliseconds);

                return new WcsApiResponse
                {
                    Success = true,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = "Chute requested successfully from collection institution",
                    Data = responseContent,
                    ResponseBody = responseContent,
                    ParcelId = parcelId,
                    RequestBody = soapRequest,
                    RequestTime = requestTime,
                    ResponseTime = DateTime.Now,
                    ResponseStatusCode = (int)response.StatusCode,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    OcrData = ocrData
                };
            }
            else
            {
                _logger.LogWarning(
                    "请求格口失败（邮政分揽投机构），包裹ID: {ParcelId}, 条码: {Barcode}, 状态码: {StatusCode}, 响应: {Response}, 耗时: {Duration}ms",
                    parcelId, dwsData.Barcode, response.StatusCode, responseContent, stopwatch.ElapsedMilliseconds);

                return new WcsApiResponse
                {
                    Success = false,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = $"Chute Request Error: {response.StatusCode}",
                    Data = responseContent,
                    ResponseBody = responseContent,
                    ErrorMessage = $"Chute Request Error: {response.StatusCode}",
                    ParcelId = parcelId,
                    RequestBody = soapRequest,
                    RequestTime = requestTime,
                    ResponseTime = DateTime.Now,
                    ResponseStatusCode = (int)response.StatusCode,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    OcrData = ocrData
                };
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "请求格口异常（邮政分揽投机构），包裹ID: {ParcelId}, 耗时: {Duration}ms", parcelId, stopwatch.ElapsedMilliseconds);

            return new WcsApiResponse
            {
                Success = false,
                Code = ApiConstants.HttpStatusCodes.Error,
                Message = ex.Message,
                Data = ex.ToString(),
                ErrorMessage = ex.Message,
                ParcelId = parcelId,
                RequestTime = requestTime,
                ResponseTime = DateTime.Now,
                DurationMs = stopwatch.ElapsedMilliseconds,
                OcrData = ocrData
            };
        }
    }

    /// <summary>
    /// 从SOAP响应中提取格口信息
    /// Extract chute information from SOAP response
    /// </summary>
    private string? ExtractChuteFromResponse(string responseContent)
    {
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            return null;
        }

        var pattern = @"#HEAD::(.*?)::\|\|#END";
        var match = Regex.Match(responseContent, pattern);
        if (match.Success)
        {
            var content = match.Groups[1].Value;
            var parts = content.Split(new string[] { "::" }, StringSplitOptions.None);

            if (parts.Length > 7 && parts[7].Length >= 4)
            {
                // 格口在第8个字段（索引7），取前4位
                return parts[7][..4];
            }
        }

        return null;
    }

    /// <summary>
    /// 上传图片到邮政分揽投机构
    /// Upload image to postal collection institution
    /// 注意：根据要求，如果不存在或未实现可以留空
    /// </summary>
    public async Task<WcsApiResponse> UploadImageAsync(
        string barcode,
        byte[] imageData,
        string contentType = ConfigurationDefaults.ImageFile.DefaultContentType,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("上传图片功能（邮政分揽投机构）当前未实现，条码: {Barcode}", barcode);
        
        await Task.CompletedTask;

        return new WcsApiResponse
        {
            Success = true,
            Code = ApiConstants.HttpStatusCodes.Success,
            Message = "邮政分揽投机构图片上传功能未实现",
            Data = "{\"info\":\"Feature not implemented\"}",
            ParcelId = barcode,
            RequestTime = DateTime.Now,
            ResponseTime = DateTime.Now,
            DurationMs = 0
        };
    }
}
