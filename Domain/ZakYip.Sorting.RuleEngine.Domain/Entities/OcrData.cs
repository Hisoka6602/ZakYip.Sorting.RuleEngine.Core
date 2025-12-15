using ZakYip.Sorting.RuleEngine.Domain.Services;

namespace ZakYip.Sorting.RuleEngine.Domain.Entities;
/// <summary>
/// OCR识别数据实体
/// </summary>
public class OcrData
{
    /// <summary>
    /// 三段码（完整）
    /// </summary>
    public string ThreeSegmentCode { get; set; } = string.Empty;
    /// 第一段码
    public string FirstSegmentCode { get; set; } = string.Empty;
    /// 第二段码
    public string SecondSegmentCode { get; set; } = string.Empty;
    /// 第三段码
    public string ThirdSegmentCode { get; set; } = string.Empty;
    /// 收件人地址
    public string RecipientAddress { get; set; } = string.Empty;
    /// 寄件人地址
    public string SenderAddress { get; set; } = string.Empty;
    /// 收件人电话后缀
    public string RecipientPhoneSuffix { get; set; } = string.Empty;
    /// 寄件人电话后缀
    public string SenderPhoneSuffix { get; set; } = string.Empty;
    /// OCR识别时间
    public DateTime RecognizedAt { get; set; } = SystemClockProvider.LocalNow;
}
