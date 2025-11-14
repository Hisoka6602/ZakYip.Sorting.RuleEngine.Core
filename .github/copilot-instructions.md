# C# 编码规范 / C# Coding Standards

本文档定义了本项目的 C# 编码规范和最佳实践。所有代码和注释必须遵守这些规范。

This document defines the C# coding standards and best practices for this project. All code and comments must follow these standards.

## 1. 使用 required + init 实现更安全的对象创建 / Use required + init for Safer Object Creation

确保某些属性在对象创建时必须被设置。通过避免部分初始化的对象来减少错误。

Ensure certain properties must be set when creating objects. Reduce errors by avoiding partially initialized objects.

### 规则 / Rules:
- 对于必须在对象创建时提供的属性，使用 `required` 修饰符
- Use the `required` modifier for properties that must be provided at object creation
- 结合 `init` 访问器实现只读初始化
- Combine with `init` accessor for read-only initialization
- 适用于实体类、DTO、配置对象等
- Apply to entity classes, DTOs, configuration objects, etc.

### 示例 / Example:
```csharp
// ✓ 好的做法 / Good practice
public class SortingRule
{
    public required string RuleId { get; init; }
    public required string RuleName { get; init; }
    public string? Description { get; init; }
}

// ✗ 避免 / Avoid
public class SortingRule
{
    public string RuleId { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
```

## 2. 启用可空引用类型 / Enable Nullable Reference Types

立刻行动。让编译器对可能的空引用问题发出警告，在运行前发现问题。

Take action immediately. Let the compiler warn about possible null reference issues and find problems before runtime.

### 规则 / Rules:
- 在项目文件中启用 `<Nullable>enable</Nullable>`（已启用）
- Enable `<Nullable>enable</Nullable>` in project files (already enabled)
- 正确使用 `?` 标记可空引用类型
- Correctly use `?` to mark nullable reference types
- 避免不必要的 null 检查
- Avoid unnecessary null checks
- 使用 null 合并运算符 `??` 和 null 条件运算符 `?.`
- Use null coalescing operator `??` and null conditional operator `?.`

### 示例 / Example:
```csharp
// ✓ 好的做法 / Good practice
public string ProcessBarcode(string barcode, string? optionalSuffix)
{
    ArgumentNullException.ThrowIfNull(barcode);
    return optionalSuffix is null ? barcode : $"{barcode}{optionalSuffix}";
}

// ✗ 避免 / Avoid
public string ProcessBarcode(string barcode, string optionalSuffix)
{
    if (barcode == null) throw new ArgumentNullException(nameof(barcode));
    if (optionalSuffix == null) return barcode;
    return barcode + optionalSuffix;
}
```

## 3. 使用文件作用域类型实现真正封装 / Use File-Scoped Types for True Encapsulation

保持工具类在文件内私有，避免污染全局命名空间，帮助强制执行边界。

Keep utility classes private within files, avoid polluting the global namespace, and help enforce boundaries.

### 规则 / Rules:
- 使用 `file` 访问修饰符声明文件作用域类型
- Use the `file` access modifier to declare file-scoped types
- 适用于内部辅助类、扩展类、工具类
- Apply to internal helper classes, extension classes, utility classes
- 减少不必要的 public 类型暴露
- Reduce unnecessary public type exposure

### 示例 / Example:
```csharp
// ✓ 好的做法 / Good practice
public class RuleEngineService
{
    public void ProcessRules() => RuleHelper.Validate();
}

file static class RuleHelper
{
    public static void Validate() { /* ... */ }
}

// ✗ 避免 / Avoid
public class RuleEngineService
{
    public void ProcessRules() => RuleHelper.Validate();
}

internal static class RuleHelper
{
    public static void Validate() { /* ... */ }
}
```

## 4. 使用文件作用域命名空间 / Use File-Scoped Namespaces

减少缩进，提高代码可读性。

Reduce indentation and improve code readability.

### 规则 / Rules:
- 对单命名空间文件使用文件作用域命名空间声明
- Use file-scoped namespace declaration for single-namespace files
- 格式：`namespace YourNamespace;`（注意分号）
- Format: `namespace YourNamespace;` (note the semicolon)

### 示例 / Example:
```csharp
// ✓ 好的做法 / Good practice
namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

public class SortingRule
{
    public required string RuleId { get; init; }
}

// ✗ 避免 / Avoid
namespace ZakYip.Sorting.RuleEngine.Domain.Entities
{
    public class SortingRule
    {
        public required string RuleId { get; init; }
    }
}
```

## 5. 使用记录处理不可变数据 / Use Records for Immutable Data

Record 是 DTO 和只读数据的理想选择。

Records are ideal for DTOs and read-only data.

### 规则 / Rules:
- 对于只读数据传输对象（DTO）使用 `record` 或 `record struct`
- Use `record` or `record struct` for read-only data transfer objects (DTOs)
- 对于事件、消息、配置对象使用 record
- Use records for events, messages, configuration objects
- 利用记录的内置值相等性和解构功能
- Leverage built-in value equality and deconstruction of records
- 对于简单 DTO 使用位置参数语法
- Use positional parameter syntax for simple DTOs

### 示例 / Example:
```csharp
// ✓ 好的做法 / Good practice
public record RuleCreatedEvent(string RuleId, string RuleName, DateTime CreatedAt);

public record struct DimensionInfo(decimal Length, decimal Width, decimal Height);

// ✗ 避免 / Avoid
public class RuleCreatedEvent
{
    public string RuleId { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
```

## 6. 保持方法专注且小巧 / Keep Methods Focused and Small

一个方法 = 一个职责。较小的方法更易于阅读、测试和重用。

One method = one responsibility. Smaller methods are easier to read, test, and reuse.

### 规则 / Rules:
- 每个方法应该只做一件事
- Each method should do only one thing
- 方法长度建议不超过 50 行
- Recommended method length: no more than 50 lines
- 复杂逻辑拆分为多个私有辅助方法
- Split complex logic into multiple private helper methods
- 使用表达式主体成员语法简化简单方法
- Use expression-bodied member syntax for simple methods
- 方法名应清楚描述其功能
- Method names should clearly describe their function

### 示例 / Example:
```csharp
// ✓ 好的做法 / Good practice
public async Task<MatchResult> MatchRuleAsync(ParcelInfo parcel)
{
    var rules = await LoadActiveRulesAsync();
    var matchedRule = FindMatchingRule(parcel, rules);
    return CreateMatchResult(matchedRule, parcel);
}

private async Task<List<Rule>> LoadActiveRulesAsync() => 
    await _repository.GetActiveRulesAsync();

private Rule? FindMatchingRule(ParcelInfo parcel, List<Rule> rules) => 
    rules.FirstOrDefault(r => r.Matches(parcel));

// ✗ 避免 / Avoid
public async Task<MatchResult> MatchRuleAsync(ParcelInfo parcel)
{
    var rules = await _repository.GetAllAsync();
    var activeRules = rules.Where(r => r.IsEnabled).ToList();
    
    Rule? matchedRule = null;
    foreach (var rule in activeRules)
    {
        if (rule.Priority < 100)
        {
            if (rule.ConditionExpression.Contains(parcel.Destination))
            {
                matchedRule = rule;
                break;
            }
        }
    }
    
    var result = new MatchResult();
    if (matchedRule != null)
    {
        result.Success = true;
        result.RuleId = matchedRule.RuleId;
        result.TargetChute = matchedRule.TargetChute;
    }
    return result;
}
```

## 7. 不需要可变性时优先使用 readonly struct / Prefer readonly struct When Mutability Is Not Needed

防止意外更改并提高性能。

Prevent accidental modifications and improve performance.

### 规则 / Rules:
- 对于小型值类型（≤16 字节），使用 `readonly struct`
- Use `readonly struct` for small value types (≤16 bytes)
- 所有字段都应该是 readonly 的
- All fields should be readonly
- 适用于坐标、尺寸、范围等不可变值类型
- Apply to immutable value types like coordinates, dimensions, ranges
- 使用 `record struct` 结合 `readonly` 获得更多便利
- Combine with `record struct` for more convenience

### 示例 / Example:
```csharp
// ✓ 好的做法 / Good practice
public readonly struct Point
{
    public readonly double X;
    public readonly double Y;
    
    public Point(double x, double y)
    {
        X = x;
        Y = y;
    }
}

// 或使用 record struct / Or use record struct
public readonly record struct Dimensions(decimal Length, decimal Width, decimal Height);

// ✗ 避免 / Avoid
public struct Point
{
    public double X { get; set; }
    public double Y { get; set; }
}
```

## 8. 其他最佳实践 / Other Best Practices

### 8.1 使用表达式主体成员 / Use Expression-Bodied Members
对于简单的属性、方法和构造函数，使用表达式主体语法。

For simple properties, methods, and constructors, use expression body syntax.

```csharp
// ✓ 好的做法 / Good practice
public string FullName => $"{FirstName} {LastName}";
public int GetTotal() => Items.Sum(i => i.Price);

// ✗ 避免 / Avoid
public string FullName 
{ 
    get { return FirstName + " " + LastName; } 
}
```

### 8.2 使用模式匹配 / Use Pattern Matching
利用 C# 的模式匹配功能使代码更简洁。

Leverage C# pattern matching features to make code more concise.

```csharp
// ✓ 好的做法 / Good practice
public string GetStatusMessage(ParcelStatus status) => status switch
{
    ParcelStatus.Pending => "待处理",
    ParcelStatus.Processing => "处理中",
    ParcelStatus.Completed => "已完成",
    _ => "未知状态"
};
```

### 8.3 避免魔法数字和字符串 / Avoid Magic Numbers and Strings
使用命名常量或枚举。

Use named constants or enums.

```csharp
// ✓ 好的做法 / Good practice
private const int MaxRetryCount = 3;
private const string DefaultChuteCode = "DEFAULT";

// ✗ 避免 / Avoid
if (retryCount > 3) { }
if (chuteCode == "DEFAULT") { }
```

### 8.4 使用 LINQ 简化集合操作 / Use LINQ to Simplify Collection Operations
优先使用 LINQ 进行集合查询和转换。

Prefer LINQ for collection queries and transformations.

```csharp
// ✓ 好的做法 / Good practice
var activeRules = rules.Where(r => r.IsEnabled).OrderBy(r => r.Priority);

// ✗ 避免 / Avoid
var activeRules = new List<Rule>();
foreach (var rule in rules)
{
    if (rule.IsEnabled)
    {
        activeRules.Add(rule);
    }
}
activeRules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
```

### 8.5 异步编程最佳实践 / Async Programming Best Practices
- 使用 `async/await` 而不是 `.Result` 或 `.Wait()`
- Use `async/await` instead of `.Result` or `.Wait()`
- 异步方法命名以 `Async` 结尾
- Async method names should end with `Async`
- 传递 `CancellationToken` 支持取消操作
- Pass `CancellationToken` to support cancellation

```csharp
// ✓ 好的做法 / Good practice
public async Task<Result> ProcessAsync(CancellationToken cancellationToken = default)
{
    var data = await _repository.GetDataAsync(cancellationToken);
    return await ProcessDataAsync(data, cancellationToken);
}

// ✗ 避免 / Avoid
public Result Process()
{
    var data = _repository.GetDataAsync().Result;
    return ProcessDataAsync(data).Result;
}
```

## 9. 注释和文档 / Comments and Documentation

### 规则 / Rules:
- 所有 public 类型和成员必须有 XML 文档注释
- All public types and members must have XML documentation comments
- 使用中英文双语注释（中文在前，英文在后）
- Use bilingual comments (Chinese first, English second)
- 注释应该解释"为什么"，而不是"是什么"
- Comments should explain "why", not "what"
- 保持注释与代码同步更新
- Keep comments synchronized with code updates

### 示例 / Example:
```csharp
/// <summary>
/// 分拣规则匹配服务
/// Sorting rule matching service
/// </summary>
public class RuleMatcher
{
    /// <summary>
    /// 执行规则匹配。优先级越小的规则越先匹配。
    /// Perform rule matching. Rules with lower priority values are matched first.
    /// </summary>
    /// <param name="parcel">包裹信息 / Parcel information</param>
    /// <returns>匹配结果 / Match result</returns>
    public MatchResult Match(ParcelInfo parcel)
    {
        // Implementation
    }
}
```

## 10. 性能考虑 / Performance Considerations

- 使用 `Span<T>` 和 `Memory<T>` 处理大量数据
- Use `Span<T>` and `Memory<T>` for handling large amounts of data
- 使用对象池减少 GC 压力
- Use object pools to reduce GC pressure
- 避免不必要的装箱/拆箱
- Avoid unnecessary boxing/unboxing
- 使用 `StringBuilder` 进行大量字符串拼接
- Use `StringBuilder` for extensive string concatenation
- 考虑使用 `ValueTask<T>` 代替 `Task<T>` 用于高频调用
- Consider using `ValueTask<T>` instead of `Task<T>` for high-frequency calls

---

## 总结 / Summary

遵循这些编码规范将帮助我们：
- 编写更安全、更可维护的代码
- 减少运行时错误
- 提高代码性能
- 增强团队协作效率

Following these coding standards will help us:
- Write safer and more maintainable code
- Reduce runtime errors
- Improve code performance
- Enhance team collaboration efficiency

**请在所有代码更改中严格遵守这些规范。**

**Please strictly follow these standards in all code changes.**
