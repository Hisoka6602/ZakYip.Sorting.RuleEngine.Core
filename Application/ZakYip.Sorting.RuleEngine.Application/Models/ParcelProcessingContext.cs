using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Application.Models;

/// <summary>
/// 包裹处理上下文，封装处理过程中产生的所有数据。/ Parcel processing context that encapsulates all data generated during handling.
/// </summary>
public class ParcelProcessingContext
{
    /// <summary>
    /// 包裹唯一标识。/ Unique identifier of the parcel.
    /// </summary>
    public required string ParcelId { get; init; }

    /// <summary>
    /// 小车编号。/ Cart number assigned to the parcel.
    /// </summary>
    public required string CartNumber { get; init; }

    /// <summary>
    /// 包裹条码，可为空。/ Parcel barcode, optional when missing.
    /// </summary>
    public string? Barcode { get; init; }

    /// <summary>
    /// 包裹进入系统的顺序号。/ Sequential number representing the parcel order in the system.
    /// </summary>
    public long SequenceNumber { get; init; }

    /// <summary>
    /// 创建时间戳。/ Timestamp indicating when the parcel context was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// 接收到DWS数据的时间，可为空。/ Time when DWS data was received, if available.
    /// </summary>
    public DateTime? DwsReceivedAt { get; set; }

    /// <summary>
    /// DWS测量数据，可为空。/ DWS measurement data when provided.
    /// </summary>
    public DwsData? DwsData { get; set; }

    /// <summary>
    /// 第三方WCS API响应，可为空。/ Response from the external WCS API when available.
    /// </summary>
    public WcsApiResponse? WcsApiResponse { get; set; }
}
