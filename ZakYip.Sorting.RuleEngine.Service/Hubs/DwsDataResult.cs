namespace ZakYip.Sorting.RuleEngine.Service.Hubs;

/// <summary>
/// DWS数据接收结果
/// </summary>
public class DwsDataResult
{
    public bool Success { get; set; }
    public required string ParcelId { get; set; }
    public required string Message { get; set; }
}
