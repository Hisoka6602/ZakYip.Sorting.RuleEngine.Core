using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Services;

/// <summary>
/// DWS数据解析器实现
/// DWS data parser implementation
/// </summary>
public class DwsDataParser : IDwsDataParser
{
    private readonly ISystemClock _clock;

    public DwsDataParser(ISystemClock clock)
    {
        _clock = clock;
    }

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

    public DwsData? Parse(string rawData, DwsDataTemplate template)
    {
        if (string.IsNullOrWhiteSpace(rawData) || template == null)
        {
            return null;
        }

        try
        {
            var templatePattern = Regex.Escape(template.Template)
                .Replace("\\{", "{")
                .Replace("\\}", "}");

            foreach (var mapping in FieldMappings)
            {
                templatePattern = templatePattern.Replace($"{{{mapping.Key}}}", $"(?<{mapping.Value}>[^,]+)");
            }

            var match = Regex.Match(rawData, $"^{templatePattern}$");
            if (!match.Success)
            {
                return null;
            }

            var dwsData = new DwsData
            {
                ReceivedAt = _clock.LocalNow
            };

            if (match.Groups["Barcode"].Success)
            {
                dwsData.Barcode = match.Groups["Barcode"].Value;
            }

            if (match.Groups["Weight"].Success && decimal.TryParse(match.Groups["Weight"].Value, out var weight))
            {
                dwsData.Weight = weight;
            }

            if (match.Groups["Length"].Success && decimal.TryParse(match.Groups["Length"].Value, out var length))
            {
                dwsData.Length = length;
            }

            if (match.Groups["Width"].Success && decimal.TryParse(match.Groups["Width"].Value, out var width))
            {
                dwsData.Width = width;
            }

            if (match.Groups["Height"].Success && decimal.TryParse(match.Groups["Height"].Value, out var height))
            {
                dwsData.Height = height;
            }

            if (match.Groups["Volume"].Success && decimal.TryParse(match.Groups["Volume"].Value, out var volume))
            {
                dwsData.Volume = volume;
            }

            return dwsData;
        }
        catch
        {
            return null;
        }
    }
}
