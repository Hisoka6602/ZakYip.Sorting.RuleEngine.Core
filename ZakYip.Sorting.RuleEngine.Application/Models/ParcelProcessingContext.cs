using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Application.Models;

/// <summary>
/// 包裹处理上下文
/// </summary>
public class ParcelProcessingContext
{
    public required string ParcelId { get; init; }
    public required string CartNumber { get; init; }
    public string? Barcode { get; init; }
    public long SequenceNumber { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? DwsReceivedAt { get; set; }
    public DwsData? DwsData { get; set; }
    public ThirdPartyResponse? ThirdPartyResponse { get; set; }
}
