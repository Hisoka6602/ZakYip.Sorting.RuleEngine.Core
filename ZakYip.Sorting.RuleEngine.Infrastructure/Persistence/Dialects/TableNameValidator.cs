using System.Text.RegularExpressions;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Dialects;

/// <summary>
/// 数据库表名验证工具类
/// Database table name validation utility class
/// </summary>
internal static partial class TableNameValidator
{
    // 预编译正则表达式以提高性能
    // Pre-compiled regex for better performance
    [GeneratedRegex(@"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled)]
    private static partial Regex GetTableNameRegex();

    private static readonly Regex TableNameRegex = GetTableNameRegex();

    /// <summary>
    /// 验证表名，防止SQL注入
    /// Validate table name to prevent SQL injection
    /// </summary>
    /// <param name="tableName">表名</param>
    /// <param name="maxLength">最大长度，默认64</param>
    /// <exception cref="ArgumentException">表名无效时抛出</exception>
    public static void Validate(string tableName, int maxLength = 64)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            throw new ArgumentException("Table name cannot be null, empty, or whitespace.", nameof(tableName));
        }

        // 只允许字母、数字、下划线，且必须以字母或下划线开头
        // Only allow letters, numbers, underscores, and must start with letter or underscore
        if (!TableNameRegex.IsMatch(tableName))
        {
            throw new ArgumentException(
                $"Invalid table name: {tableName}. Table names must start with a letter or underscore and contain only letters, numbers, and underscores.",
                nameof(tableName));
        }

        // 限制长度
        // Limit length
        if (tableName.Length > maxLength)
        {
            throw new ArgumentException(
                $"Table name is too long: {tableName}. Maximum length is {maxLength} characters.",
                nameof(tableName));
        }
    }
}
