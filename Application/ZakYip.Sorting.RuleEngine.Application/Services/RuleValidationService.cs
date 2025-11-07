using System.Text.RegularExpressions;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;

namespace ZakYip.Sorting.RuleEngine.Application.Services;

/// <summary>
/// 规则验证服务
/// 用于验证规则表达式的安全性，防止代码注入
/// </summary>
public class RuleValidationService
{
    /// <summary>
    /// 危险关键字列表（防止代码注入）
    /// </summary>
    private static readonly string[] DangerousKeywords = new[]
    {
        "eval", "exec", "execute", "system", "cmd", "shell", "script",
        "process", "runtime", "assembly", "reflection", "type.gettype",
        "activator.createinstance", "invoke", "method.invoke",
        "dynamic", "compile", "codedom", "javascript:", "vbscript:",
        "__import__", "import ", "require(", "loadlibrary",
        "file.", "directory.", "path.", "io.", "streamreader", "streamwriter",
        "sqlconnection", "sqlcommand", "database", "drop ", "delete ",
        "insert ", "update ", "select ", "union ", "create table",
        "alter table", "truncate", "<script", "javascript:", "onerror",
        "onload", "onclick", "href=\"javascript"
    };

    /// <summary>
    /// 允许的表达式模式（白名单）
    /// </summary>
    private static readonly Dictionary<MatchingMethodType, Regex[]> AllowedPatterns = new()
    {
        {
            MatchingMethodType.BarcodeRegex, new[]
            {
                new Regex(@"^(STARTSWITH|CONTAINS|NOTCONTAINS|ALLDIGITS|ALPHANUMERIC|LENGTH|REGEX):", RegexOptions.Compiled),
                new Regex(@"^[A-Za-z0-9\s\-_:^$.*+?{}\[\]()]+$", RegexOptions.Compiled)
            }
        },
        {
            MatchingMethodType.WeightMatch, new[]
            {
                new Regex(@"^Weight\s*[><=]+\s*\d+(\.\d+)?(\s*(and|or|AND|OR)\s*Weight\s*[><=]+\s*\d+(\.\d+)?)*$", RegexOptions.Compiled)
            }
        },
        {
            MatchingMethodType.VolumeMatch, new[]
            {
                new Regex(@"^(Length|Width|Height|Volume)\s*[><=]+\s*\d+(\.\d+)?(\s*(and|or|AND|OR)\s*(Length|Width|Height|Volume)\s*[><=]+\s*\d+(\.\d+)?)*$", RegexOptions.Compiled)
            }
        },
        {
            MatchingMethodType.OcrMatch, new[]
            {
                new Regex(@"^(firstSegmentCode|secondSegmentCode|thirdSegmentCode|recipientPhoneSuffix|senderPhoneSuffix|recipientAddress|senderAddress)\s*=\s*.+$", RegexOptions.Compiled)
            }
        },
        {
            MatchingMethodType.ApiResponseMatch, new[]
            {
                new Regex(@"^(STRING|STRINGREVERSE|REGEX|JSON):.+$", RegexOptions.Compiled)
            }
        }
    };

    /// <summary>
    /// 验证规则的安全性
    /// </summary>
    /// <param name="rule">待验证的规则</param>
    /// <returns>验证结果和错误信息</returns>
    public (bool IsValid, string? ErrorMessage) ValidateRule(SortingRule rule)
    {
        // 步骤1：检查规则ID和名称
        if (string.IsNullOrWhiteSpace(rule.RuleId))
        {
            return (false, "规则ID不能为空");
        }

        if (string.IsNullOrWhiteSpace(rule.RuleName))
        {
            return (false, "规则名称不能为空");
        }

        // 步骤2：检查目标格口
        if (string.IsNullOrWhiteSpace(rule.TargetChute))
        {
            return (false, "目标格口不能为空");
        }

        // 步骤3：检查条件表达式是否为空
        if (string.IsNullOrWhiteSpace(rule.ConditionExpression))
        {
            return (false, "条件表达式不能为空");
        }

        // 步骤4：检查表达式长度限制（防止过长的表达式）
        if (rule.ConditionExpression.Length > 2000)
        {
            return (false, "条件表达式长度不能超过2000个字符");
        }

        // 步骤5：检查危险关键字
        var expressionLower = rule.ConditionExpression.ToLower();
        foreach (var keyword in DangerousKeywords)
        {
            if (expressionLower.Contains(keyword.ToLower()))
            {
                return (false, $"条件表达式包含危险关键字: {keyword}");
            }
        }

        // 步骤6：检查是否包含非法字符
        if (ContainsIllegalCharacters(rule.ConditionExpression))
        {
            return (false, "条件表达式包含非法字符");
        }

        // 步骤7：根据匹配方法类型验证表达式格式
        if (rule.MatchingMethod != MatchingMethodType.LegacyExpression &&
            rule.MatchingMethod != MatchingMethodType.LowCodeExpression)
        {
            var formatValidation = ValidateExpressionFormat(rule.ConditionExpression, rule.MatchingMethod);
            if (!formatValidation.IsValid)
            {
                return formatValidation;
            }
        }

        // 步骤8：验证优先级范围
        if (rule.Priority < 0 || rule.Priority > 9999)
        {
            return (false, "优先级必须在0到9999之间");
        }

        return (true, null);
    }

    /// <summary>
    /// 验证表达式格式是否符合匹配方法类型
    /// </summary>
    /// <param name="expression">表达式</param>
    /// <param name="matchingMethod">匹配方法类型</param>
    /// <returns>验证结果</returns>
    private (bool IsValid, string? ErrorMessage) ValidateExpressionFormat(
        string expression,
        MatchingMethodType matchingMethod)
    {
        if (!AllowedPatterns.ContainsKey(matchingMethod))
        {
            return (true, null); // 未定义验证规则的类型，跳过格式验证
        }

        var patterns = AllowedPatterns[matchingMethod];
        foreach (var pattern in patterns)
        {
            if (pattern.IsMatch(expression))
            {
                return (true, null);
            }
        }

        return (false, $"条件表达式格式不符合{matchingMethod}类型的要求");
    }

    /// <summary>
    /// 检查是否包含非法字符
    /// </summary>
    /// <param name="expression">表达式</param>
    /// <returns>是否包含非法字符</returns>
    private bool ContainsIllegalCharacters(string expression)
    {
        // 检查是否包含控制字符（除了常见的空格、换行、制表符）
        foreach (var ch in expression)
        {
            if (char.IsControl(ch) && ch != '\r' && ch != '\n' && ch != '\t')
            {
                return true;
            }

            // 检查是否包含潜在危险的特殊字符
            if (ch == ';' || ch == '`' || ch == '|' || ch == '&')
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 批量验证规则
    /// </summary>
    /// <param name="rules">规则列表</param>
    /// <returns>验证结果字典</returns>
    public Dictionary<string, (bool IsValid, string? ErrorMessage)> ValidateRules(IEnumerable<SortingRule> rules)
    {
        var results = new Dictionary<string, (bool IsValid, string? ErrorMessage)>();
        
        foreach (var rule in rules)
        {
            results[rule.RuleId] = ValidateRule(rule);
        }

        return results;
    }
}
