namespace ZakYip.Sorting.RuleEngine.Domain.DTOs;

/// <summary>
/// 强类型API响应基类
/// Strongly-typed base API response
/// </summary>
/// <typeparam name="TData">响应数据类型</typeparam>
public class StronglyTypedApiResponseDto<TData>
{
    /// <summary>
    /// 响应是否成功
    /// Whether the response is successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 响应代码
    /// Response code
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 响应消息
    /// Response message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 强类型响应数据
    /// Strongly-typed response data
    /// </summary>
    public TData? Data { get; set; }

    /// <summary>
    /// 错误信息
    /// Error message
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 请求时间
    /// Request time
    /// </summary>
    public DateTime RequestTime { get; set; }

    /// <summary>
    /// 响应时间
    /// Response time
    /// </summary>
    public DateTime? ResponseTime { get; set; }

    /// <summary>
    /// 耗时（毫秒）
    /// Duration in milliseconds
    /// </summary>
    public long DurationMs { get; set; }
}

/// <summary>
/// 格口请求响应数据
/// Chute request response data
/// </summary>
public class ChuteRequestResponseData
{
    /// <summary>
    /// 格口号
    /// Chute number
    /// </summary>
    public string? ChuteNumber { get; set; }

    /// <summary>
    /// 格口名称
    /// Chute name
    /// </summary>
    public string? ChuteName { get; set; }

    /// <summary>
    /// 包裹ID
    /// Parcel ID
    /// </summary>
    public string? ParcelId { get; set; }

    /// <summary>
    /// 条码
    /// Barcode
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// 附加信息
    /// Additional information
    /// </summary>
    public Dictionary<string, object>? AdditionalInfo { get; set; }
}

/// <summary>
/// 扫描包裹响应数据
/// Scan parcel response data
/// </summary>
public class ScanParcelResponseData
{
    /// <summary>
    /// 包裹ID
    /// Parcel ID
    /// </summary>
    public string? ParcelId { get; set; }

    /// <summary>
    /// 条码
    /// Barcode
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// 扫描时间
    /// Scan time
    /// </summary>
    public DateTime? ScanTime { get; set; }

    /// <summary>
    /// 是否已注册
    /// Is registered
    /// </summary>
    public bool IsRegistered { get; set; }
}

/// <summary>
/// 图片上传响应数据
/// Image upload response data
/// </summary>
public class ImageUploadResponseData
{
    /// <summary>
    /// 图片ID
    /// Image ID
    /// </summary>
    public string? ImageId { get; set; }

    /// <summary>
    /// 图片URL
    /// Image URL
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// 上传时间
    /// Upload time
    /// </summary>
    public DateTime? UploadTime { get; set; }

    /// <summary>
    /// 文件大小（字节）
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }
}

/// <summary>
/// 批量操作请求
/// Batch operation request
/// </summary>
/// <typeparam name="TRequest">请求类型</typeparam>
public class BatchOperationRequest<TRequest>
{
    /// <summary>
    /// 批量请求列表
    /// Batch request list
    /// </summary>
    public List<TRequest> Requests { get; set; } = new();

    /// <summary>
    /// 是否并行处理
    /// Whether to process in parallel
    /// </summary>
    public bool ProcessInParallel { get; set; } = true;

    /// <summary>
    /// 最大并行度
    /// Maximum degree of parallelism
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = 4;
}

/// <summary>
/// 批量操作响应
/// Batch operation response
/// </summary>
/// <typeparam name="TResponse">响应类型</typeparam>
public class BatchOperationResponse<TResponse>
{
    /// <summary>
    /// 成功的响应列表
    /// Successful responses
    /// </summary>
    public List<TResponse> SuccessfulResponses { get; set; } = new();

    /// <summary>
    /// 失败的响应列表
    /// Failed responses
    /// </summary>
    public List<TResponse> FailedResponses { get; set; } = new();

    /// <summary>
    /// 总数
    /// Total count
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 成功数
    /// Success count
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// 失败数
    /// Failed count
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// 总耗时（毫秒）
    /// Total duration in milliseconds
    /// </summary>
    public long TotalDurationMs { get; set; }
}
