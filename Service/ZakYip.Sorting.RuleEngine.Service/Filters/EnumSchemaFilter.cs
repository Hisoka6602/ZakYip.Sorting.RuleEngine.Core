using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.ComponentModel;
using System.Reflection;

namespace ZakYip.Sorting.RuleEngine.Service.Filters;

/// <summary>
/// Swagger枚举架构过滤器
/// 为枚举类型添加描述信息，使Swagger UI能够显示每个枚举值的名称和含义
/// Swagger enum schema filter
/// Add description information for enum types so Swagger UI can display the meaning of each enum value
/// </summary>
public class EnumSchemaFilter : ISchemaFilter
{
    /// <summary>
    /// 应用枚举架构过滤器
    /// Apply enum schema filter
    /// </summary>
    /// <param name="schema">OpenAPI架构 / OpenAPI schema</param>
    /// <param name="context">架构过滤器上下文 / Schema filter context</param>
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type.IsEnum)
        {
            schema.Enum.Clear();
            
            var enumValues = Enum.GetValues(context.Type);
            var enumDescriptions = new List<string>();

            foreach (var enumValue in enumValues)
            {
                var enumValueName = enumValue.ToString();
                if (enumValueName == null) continue;

                var memberInfo = context.Type.GetMember(enumValueName).FirstOrDefault();
                var descriptionAttribute = memberInfo?.GetCustomAttribute<DescriptionAttribute>();
                
                // 使用Description属性作为描述，如果没有则使用枚举名称
                // Use Description attribute as description, or use enum name if not available
                var description = descriptionAttribute?.Description ?? enumValueName;
                
                // 获取枚举的数值
                // Get numeric value of the enum
                var enumNumericValue = Convert.ToInt32(enumValue);
                
                // 添加字符串形式的枚举名称到schema的enum列表（而不是数字）
                // Add string form of enum name to schema enum list (instead of numbers)
                schema.Enum.Add(new OpenApiString(enumValueName));
                
                // 构建枚举值的描述：名称 (数值) - 描述
                // Build enum value description: Name (Value) - Description
                enumDescriptions.Add($"{enumValueName} ({enumNumericValue}) - {description}");
            }

            // 将所有枚举值的描述添加到schema的description中
            // Add all enum value descriptions to schema description
            if (enumDescriptions.Any())
            {
                var originalDescription = schema.Description ?? string.Empty;
                schema.Description = !string.IsNullOrEmpty(originalDescription)
                    ? $"{originalDescription}\n\n可选值 / Available values:\n" + string.Join("\n", enumDescriptions)
                    : "可选值 / Available values:\n" + string.Join("\n", enumDescriptions);
            }

            // 设置枚举类型为字符串（配合 StringEnumConverter）
            // Set enum type as string (works with StringEnumConverter)
            schema.Type = "string";
            schema.Format = null;
        }
    }
}
