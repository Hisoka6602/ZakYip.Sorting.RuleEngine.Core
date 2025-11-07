namespace ZakYip.Sorting.RuleEngine.Service.Hubs;

/// <summary>
/// 包裹创建结果
/// </summary>
public class ParcelCreationResult
{
    public bool Success { get; set; }
    public required string ParcelId { get; set; }
    public required string Message { get; set; }
}
