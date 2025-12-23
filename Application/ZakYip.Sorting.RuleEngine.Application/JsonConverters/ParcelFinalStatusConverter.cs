using System.Text.Json;
using System.Text.Json.Serialization;
using ZakYip.Sorting.RuleEngine.Domain.Enums;

namespace ZakYip.Sorting.RuleEngine.Application.JsonConverters;

/// <summary>
/// ParcelFinalStatus 枚举的自定义 JSON 转换器，支持数字和字符串双模式
/// Custom JSON converter for ParcelFinalStatus enum, supports both number and string formats
/// </summary>
/// <remarks>
/// 下游系统可能发送数字格式（0, 1, 2, 3）或字符串格式（"Success", "Timeout", "Lost", "ExecutionError"）
/// Downstream systems may send number format (0, 1, 2, 3) or string format ("Success", "Timeout", "Lost", "ExecutionError")
/// </remarks>
public class ParcelFinalStatusConverter : JsonConverter<ParcelFinalStatus>
{
    /// <summary>
    /// 从 JSON 读取并转换为枚举值
    /// Read from JSON and convert to enum value
    /// </summary>
    public override ParcelFinalStatus Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                // 数字格式：0=Success, 1=Timeout, 2=Lost, 3=ExecutionError
                // Number format: 0=Success, 1=Timeout, 2=Lost, 3=ExecutionError
                var numValue = reader.GetInt32();
                if (!Enum.IsDefined(typeof(ParcelFinalStatus), numValue))
                {
                    throw new JsonException($"无效的 FinalStatus 数字值: {numValue} / Invalid FinalStatus number value: {numValue}");
                }
                return (ParcelFinalStatus)numValue;

            case JsonTokenType.String:
                // 字符串格式：直接解析枚举名称
                // String format: parse enum name directly
                var strValue = reader.GetString();
                if (string.IsNullOrWhiteSpace(strValue))
                {
                    throw new JsonException("FinalStatus 字符串值不能为空 / FinalStatus string value cannot be empty");
                }
                
                if (Enum.TryParse<ParcelFinalStatus>(strValue, ignoreCase: true, out var result))
                {
                    return result;
                }
                
                throw new JsonException($"无效的 FinalStatus 字符串值: {strValue} / Invalid FinalStatus string value: {strValue}");

            default:
                throw new JsonException($"无效的 JSON token 类型: {reader.TokenType}，FinalStatus 必须是数字或字符串 / Invalid JSON token type: {reader.TokenType}, FinalStatus must be number or string");
        }
    }

    /// <summary>
    /// 将枚举值写入 JSON（始终使用字符串格式）
    /// Write enum value to JSON (always use string format)
    /// </summary>
    public override void Write(
        Utf8JsonWriter writer,
        ParcelFinalStatus value,
        JsonSerializerOptions options)
    {
        // 始终输出字符串格式，保持一致性
        // Always output string format for consistency
        writer.WriteStringValue(value.ToString());
    }
}
