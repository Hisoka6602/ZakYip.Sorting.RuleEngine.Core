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
                var xmlSummary = GetXmlDocumentation(memberInfo);
                
                // 优先使用Description属性，然后使用XML注释
                var description = descriptionAttribute?.Description ?? xmlSummary ?? enumValueName;
                
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

    /// <summary>
    /// 从XML文档注释中获取描述
    /// </summary>
    /// <param name="memberInfo">成员信息</param>
    /// <returns>XML注释中的summary内容</returns>
    private string? GetXmlDocumentation(MemberInfo? memberInfo)
    {
        if (memberInfo == null) return null;

        // 这里简化处理，实际的XML文档读取由Swagger的XML注释功能处理
        // 如果需要更详细的XML文档读取，可以在这里实现
        return null;
    }
}
