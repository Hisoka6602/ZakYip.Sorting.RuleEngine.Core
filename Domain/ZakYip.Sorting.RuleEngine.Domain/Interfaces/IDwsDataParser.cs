using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// DWS数据解析器接口
/// DWS data parser interface
/// </summary>
public interface IDwsDataParser
{
    /// <summary>
    /// 解析DWS数据
    /// Parse DWS data
    /// </summary>
    /// <param name="rawData">原始数据 / Raw data</param>
    /// <param name="template">数据模板 / Data template</param>
    /// <returns>解析后的DWS数据，解析失败返回null / Parsed DWS data, returns null if parsing fails</returns>
    DwsData? Parse(string rawData, DwsDataTemplate template);
}
