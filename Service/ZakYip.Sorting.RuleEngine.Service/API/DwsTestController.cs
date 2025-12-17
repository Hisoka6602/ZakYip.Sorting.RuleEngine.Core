using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ZakYip.Sorting.RuleEngine.Service.API;

/// <summary>
/// DWS测试控制器
/// DWS Test Controller
/// </summary>
[ApiController]
[Route("api/Dws/Test")]
[Produces("application/json")]
[SwaggerTag("DWS管理 / DWS Management")]
public class DwsTestController : ControllerBase
{
    private readonly ILogger<DwsTestController> _logger;

    public DwsTestController(ILogger<DwsTestController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 发送测试DWS数据模板字符串
    /// Send test DWS data template string
    /// </summary>
    /// <param name="request">测试请求</param>
    /// <returns>发送结果</returns>
    /// <response code="200">测试数据发送成功</response>
    /// <response code="400">请求参数错误</response>
    /// <response code="500">服务器内部错误</response>
    /// <remarks>
    /// 示例请求:
    /// 
    ///     POST /api/Dws/Test/send-template
    ///     {
    ///        "template": "{Code},{Weight},{Length},{Width},{Height},{Volume},{Timestamp}",
    ///        "delimiter": ",",
    ///        "testData": {
    ///          "code": "TEST001",
    ///          "weight": 2500.5,
    ///          "length": 300,
    ///          "width": 200,
    ///          "height": 150,
    ///          "volume": 9000,
    ///          "timestamp": "2023-11-01T10:30:00"
    ///        }
    ///     }
    /// 
    /// 发送格式示例: TEST001,2500.5,300,200,150,9000,2023-11-01T10:30:00
    /// </remarks>
    [HttpPost("send-template")]
    [SwaggerOperation(
        Summary = "发送测试DWS数据模板字符串",
        Description = "发送测试DWS数据模板到DWS设备，用于测试DWS通信。根据模板和测试数据生成格式化字符串。",
        OperationId = "SendTestDwsTemplate",
        Tags = new[] { "DWS管理 / DWS Management" }
    )]
    [SwaggerResponse(200, "测试数据发送成功", typeof(DwsTestResponse))]
    [SwaggerResponse(400, "请求参数错误", typeof(DwsTestResponse))]
    [SwaggerResponse(500, "服务器内部错误", typeof(DwsTestResponse))]
    public IActionResult SendTestTemplate(
        [FromBody, SwaggerRequestBody("测试DWS模板请求", Required = true)] DwsTestRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Template))
            {
                return BadRequest(new DwsTestResponse
                {
                    Success = false,
                    Message = "模板不能为空",
                    FormattedData = ""
                });
            }

            // 格式化模板数据
            var formattedData = FormatDwsData(request.Template, request.Delimiter, request.TestData);
            
            _logger.LogInformation(
                "发送测试DWS模板数据 - Template: {Template}, FormattedData: {FormattedData}",
                request.Template, formattedData);

            return Ok(new DwsTestResponse
            {
                Success = true,
                Message = $"测试数据已准备发送: {formattedData}",
                FormattedData = formattedData,
                Template = request.Template,
                Delimiter = request.Delimiter
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送测试DWS模板数据失败");
            return StatusCode(500, new DwsTestResponse
            {
                Success = false,
                Message = ex.Message,
                FormattedData = ""
            });
        }
    }

    /// <summary>
    /// 格式化DWS数据
    /// Format DWS data according to template
    /// </summary>
    private static string FormatDwsData(string template, string delimiter, DwsTestData? testData)
    {
        if (testData == null)
        {
            return template;
        }

        // 替换模板中的占位符
        var formatted = template
            .Replace("{Code}", testData.Code ?? "")
            .Replace("{Weight}", testData.Weight?.ToString() ?? "0")
            .Replace("{Length}", testData.Length?.ToString() ?? "0")
            .Replace("{Width}", testData.Width?.ToString() ?? "0")
            .Replace("{Height}", testData.Height?.ToString() ?? "0")
            .Replace("{Volume}", testData.Volume?.ToString() ?? "0")
            .Replace("{Timestamp}", testData.Timestamp ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        return formatted;
    }
}

/// <summary>
/// DWS测试请求 / DWS test request
/// </summary>
[SwaggerSchema(Description = "DWS测试请求")]
public class DwsTestRequest
{
    /// <summary>
    /// 数据模板 / Data template
    /// </summary>
    /// <example>{Code},{Weight},{Length},{Width},{Height},{Volume},{Timestamp}</example>
    public required string Template { get; set; }
    
    /// <summary>
    /// 分隔符 / Delimiter
    /// </summary>
    /// <example>,</example>
    public string Delimiter { get; set; } = ",";
    
    /// <summary>
    /// 测试数据 / Test data
    /// </summary>
    public DwsTestData? TestData { get; set; }
}

/// <summary>
/// DWS测试数据 / DWS test data
/// </summary>
[SwaggerSchema(Description = "DWS测试数据")]
public class DwsTestData
{
    /// <summary>
    /// 条码 / Barcode
    /// </summary>
    /// <example>TEST001</example>
    public string? Code { get; set; }
    
    /// <summary>
    /// 重量（克） / Weight (grams)
    /// </summary>
    /// <example>2500.5</example>
    public decimal? Weight { get; set; }
    
    /// <summary>
    /// 长度（毫米） / Length (mm)
    /// </summary>
    /// <example>300</example>
    public decimal? Length { get; set; }
    
    /// <summary>
    /// 宽度（毫米） / Width (mm)
    /// </summary>
    /// <example>200</example>
    public decimal? Width { get; set; }
    
    /// <summary>
    /// 高度（毫米） / Height (mm)
    /// </summary>
    /// <example>150</example>
    public decimal? Height { get; set; }
    
    /// <summary>
    /// 体积（立方厘米） / Volume (cubic cm)
    /// </summary>
    /// <example>9000</example>
    public decimal? Volume { get; set; }
    
    /// <summary>
    /// 时间戳 / Timestamp
    /// </summary>
    /// <example>2023-11-01T10:30:00</example>
    public string? Timestamp { get; set; }
}

/// <summary>
/// DWS测试响应 / DWS test response
/// </summary>
[SwaggerSchema(Description = "DWS测试响应")]
public class DwsTestResponse
{
    /// <summary>
    /// 是否成功 / Success flag
    /// </summary>
    public required bool Success { get; init; }
    
    /// <summary>
    /// 消息 / Message
    /// </summary>
    public required string Message { get; init; }
    
    /// <summary>
    /// 格式化后的数据 (按模板格式) / Formatted data (according to template)
    /// </summary>
    /// <example>TEST001,2500.5,300,200,150,9000,2023-11-01T10:30:00</example>
    public required string FormattedData { get; init; }
    
    /// <summary>
    /// 使用的模板 / Template used
    /// </summary>
    public string? Template { get; init; }
    
    /// <summary>
    /// 使用的分隔符 / Delimiter used
    /// </summary>
    public string? Delimiter { get; init; }
}
