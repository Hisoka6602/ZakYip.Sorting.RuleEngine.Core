using System.Text.Json;
using System.Text.RegularExpressions;
using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Services;

/// <summary>
/// DWS数据解析器 - 根据模板解析DWS数据
/// DWS data parser - Parses DWS data according to template
/// </summary>
public interface IDwsDataParser
{
    /// <summary>
    /// 解析DWS数据
    /// Parse DWS data
    /// </summary>
    DwsData? Parse(string rawData, DwsDataTemplate template);
}

/// <summary>
/// DWS数据解析器实现
/// DWS data parser implementation
/// </summary>
public class DwsDataParser : IDwsDataParser
{
    private static readonly Dictionary<string, string> FieldMappings = new()
    {
        { "Code", "Barcode" },
        { "Barcode", "Barcode" },
        { "Weight", "Weight" },
        { "Length", "Length" },
        { "Width", "Width" },
        { "Height", "Height" },
        { "Volume", "Volume" },
        { "Timestamp", "Timestamp" }
    };

    // Unix时间戳阈值常量 / Unix timestamp threshold constants
    private const long UnixTimestampMillisecondsThreshold = 1000000000000; // ~2001年 / ~Year 2001
    private const long UnixTimestampSecondsThreshold = 1000000000; // 2001-09-09

    public DwsData? Parse(string rawData, DwsDataTemplate template)
    {
        if (string.IsNullOrWhiteSpace(rawData))
        {
            return null;
        }

        try
        {
            if (template.IsJsonFormat)
            {
                return ParseJson(rawData);
            }
            else
            {
                return ParseTemplate(rawData, template);
            }
        }
        catch
        {
            return null;
        }
    }

    private DwsData? ParseJson(string jsonData)
    {
        try
        {
            return JsonSerializer.Deserialize<DwsData>(jsonData, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }

    private DwsData? ParseTemplate(string rawData, DwsDataTemplate template)
    {
        // 提取模板中的字段名
        // Extract field names from template
        var fieldPattern = @"\{(\w+)\}";
        var matches = Regex.Matches(template.Template, fieldPattern);
        var fieldNames = matches.Select(m => m.Groups[1].Value).ToList();

        if (fieldNames.Count == 0)
        {
            return null;
        }

        // 按分隔符分割原始数据
        // Split raw data by delimiter
        var delimiter = string.IsNullOrEmpty(template.Delimiter) ? "," : template.Delimiter;
        var values = rawData.Split(delimiter, StringSplitOptions.None);

        if (values.Length != fieldNames.Count)
        {
            return null;
        }

        // 构建字段值字典
        // Build field value dictionary
        var fieldValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < fieldNames.Count; i++)
        {
            fieldValues[fieldNames[i]] = values[i].Trim();
        }

        // 映射到DwsData对象
        // Map to DwsData object
        return MapToDwsData(fieldValues);
    }

    private DwsData MapToDwsData(Dictionary<string, string> fieldValues)
    {
        var dwsData = new DwsData();

        foreach (var kvp in fieldValues)
        {
            if (FieldMappings.TryGetValue(kvp.Key, out var propertyName))
            {
                var value = kvp.Value;
                
                switch (propertyName)
                {
                    case "Barcode":
                        dwsData.Barcode = value;
                        break;
                    case "Weight":
                        dwsData.Weight = ParseDecimal(value);
                        break;
                    case "Length":
                        dwsData.Length = ParseDecimal(value);
                        break;
                    case "Width":
                        dwsData.Width = ParseDecimal(value);
                        break;
                    case "Height":
                        dwsData.Height = ParseDecimal(value);
                        break;
                    case "Volume":
                        dwsData.Volume = ParseDecimal(value);
                        break;
                    case "Timestamp":
                        dwsData.ScannedAt = ParseTimestamp(value);
                        break;
                }
            }
        }

        return dwsData;
    }

    private decimal ParseDecimal(string value)
    {
        return decimal.TryParse(value, out var result) ? result : 0;
    }

    private DateTime ParseTimestamp(string value)
    {
        // 尝试多种时间戳格式
        // Try multiple timestamp formats
        
        // Unix timestamp (milliseconds)
        if (long.TryParse(value, out var unixMs) && unixMs > UnixTimestampMillisecondsThreshold)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(unixMs).DateTime;
        }

        // Unix timestamp (seconds)
        if (long.TryParse(value, out var unixSec) && unixSec > UnixTimestampSecondsThreshold)
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixSec).DateTime;
        }

        // ISO 8601 format
        if (DateTime.TryParse(value, out var dateTime))
        {
            return dateTime;
        }

        return DateTime.Now;
    }
}
