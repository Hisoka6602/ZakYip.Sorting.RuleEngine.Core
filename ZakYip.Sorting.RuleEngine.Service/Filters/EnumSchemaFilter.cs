using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.ComponentModel;
using System.Reflection;

namespace ZakYip.Sorting.RuleEngine.Service.Filters;

/// <summary>
/// Swagger枚举架构过滤器
/// 为枚举类型添加描述信息，使Swagger UI能够显示每个枚举值的含义
/// </summary>
public class EnumSchemaFilter : ISchemaFilter
{
    /// <summary>
    /// 应用枚举架构过滤器
    /// </summary>
    /// <param name="schema">OpenAPI架构</param>
    /// <param name="context">架构过滤器上下文</param>
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
                var description = descriptionAttribute?.Description ?? enumValueName;
                
                // 获取枚举的数值
                var enumNumericValue = Convert.ToInt32(enumValue);
                
                // 添加到schema的enum列表
                schema.Enum.Add(new OpenApiInteger(enumNumericValue));
                
                // 构建枚举值的描述：数值 = 名称 (描述)
                enumDescriptions.Add($"{enumNumericValue} = {enumValueName} ({description})");
            }

            // 将所有枚举值的描述添加到schema的description中
            if (enumDescriptions.Any())
            {
                var originalDescription = schema.Description ?? string.Empty;
                if (!string.IsNullOrEmpty(originalDescription))
                {
                    schema.Description = $"{originalDescription}\n\n可选值:\n" + string.Join("\n", enumDescriptions);
                }
                else
                {
                    schema.Description = "可选值:\n" + string.Join("\n", enumDescriptions);
                }
            }

            // 设置枚举类型
            schema.Type = "integer";
            schema.Format = "int32";
        }
    }
}
