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

    /// <summary>
    /// 第一段码
    /// </summary>
    public string FirstSegmentCode { get; set; } = string.Empty;

    /// <summary>
    /// 第二段码
    /// </summary>
    public string SecondSegmentCode { get; set; } = string.Empty;

    /// <summary>
    /// 第三段码
    /// </summary>
    public string ThirdSegmentCode { get; set; } = string.Empty;

    /// <summary>
    /// 收件人地址
    /// </summary>
    public string RecipientAddress { get; set; } = string.Empty;

    /// <summary>
    /// 寄件人地址
    /// </summary>
    public string SenderAddress { get; set; } = string.Empty;

    /// <summary>
    /// 收件人电话后缀
    /// </summary>
    public string RecipientPhoneSuffix { get; set; } = string.Empty;

    /// <summary>
    /// 寄件人电话后缀
    /// </summary>
    public string SenderPhoneSuffix { get; set; } = string.Empty;

    /// <summary>
    /// OCR识别时间
    /// </summary>
    public DateTime RecognizedAt { get; set; } = DateTime.Now;
}
