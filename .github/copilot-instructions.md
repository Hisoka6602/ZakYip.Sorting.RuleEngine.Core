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

## 11. 技术债务管理 / Technical Debt Management

### 规则 / Rules:
- 每次开启 PR 前必须通读 [TECHNICAL_DEBT.md](../TECHNICAL_DEBT.md) 文档
- Before opening any PR, read through the [TECHNICAL_DEBT.md](../TECHNICAL_DEBT.md) document
- 新代码不得引入重复代码（影分身代码）
- New code must not introduce duplicate code (shadow clone code)
- 运行 `jscpd` 检查代码重复率，确保不超过 5%
- Run `jscpd` to check code duplication rate, ensure it does not exceed 5%
- 如果引入新的技术债务，必须在 TECHNICAL_DEBT.md 中记录
- If new technical debt is introduced, it must be documented in TECHNICAL_DEBT.md

### 重复代码检测 / Duplicate Code Detection:
```bash
# 安装 jscpd / Install jscpd
npm install -g jscpd

# 运行检测 / Run detection
jscpd .
```

### 示例 / Example:
```csharp
// ✗ 避免：复制粘贴代码 / Avoid: Copy-paste code
public class WeightMatcher
{
    public bool Match(decimal weight, decimal min, decimal max) 
    {
        if (min > 0 && weight < min) return false;
        if (max > 0 && weight > max) return false;
        return true;
    }
}

public class VolumeMatcher
{
    public bool Match(decimal volume, decimal min, decimal max) 
    {
        if (min > 0 && volume < min) return false;  // 重复代码
        if (max > 0 && volume > max) return false;  // 重复代码
        return true;
    }
}

// ✓ 好的做法：提取通用逻辑 / Good: Extract common logic
public static class RangeMatcher
{
    public static bool IsInRange(decimal value, decimal min, decimal max)
    {
        if (min > 0 && value < min) return false;
        if (max > 0 && value > max) return false;
        return true;
    }
}

public class WeightMatcher
{
    public bool Match(decimal weight, decimal min, decimal max) 
        => RangeMatcher.IsInRange(weight, min, max);
}

public class VolumeMatcher
{
    public bool Match(decimal volume, decimal min, decimal max) 
        => RangeMatcher.IsInRange(volume, min, max);
}
```

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

**⚠️ 重要：每次提交 PR 前，请确保已通读 <a>TECHNICAL_DEBT.md</a>**

**⚠️ IMPORTANT: Before each PR submission, make sure you have read <a>TECHNICAL_DEBT.md</a>**

---

## 12. PR 完整性约束 / PR Integrity Constraints

### 规则 / Rules

**小型 PR（< 24小时工作量）强制完整性 / Small PR (< 24 hours) Mandatory Integrity**:
- ❌ **禁止** 提交半完成状态（如：只删除接口但不修复引用） / Prohibit half-completed state
- ❌ **禁止** 留下编译错误或测试失败 / Prohibit compilation errors or test failures
- ❌ **禁止** 使用"后续PR修复"作为理由 / Prohibit using "fix in next PR" as excuse
- ❌ **禁止** 代码中出现"TODO: 后续PR"等标记 / Prohibit "TODO: next PR" markers
- ✅ **必须** 保证代码可编译、测试通过、功能完整 / Must ensure code compiles, tests pass, features complete

**大型 PR（≥ 24小时工作量）分阶段处理 / Large PR (≥ 24 hours) Phased Approach**:
- ✅ 允许分多个 PR 逐步完成 / Allow multiple PRs to complete gradually
- ✅ 每个阶段 PR 必须独立可编译、测试通过 / Each phase PR must compile and pass tests independently
- ✅ 未完成部分必须登记到技术债务文档 / Incomplete parts must be documented in <a>TECHNICAL_DEBT.md</a>
- ✅ 技术债条目必须包含 / Tech debt entries must include:
  - 已完成和未完成的工作清单 / Completed and incomplete work checklist
  - 详细的下一步指引（文件清单、修改建议、注意事项）/ Detailed next steps guide
  - 预估工作量和风险等级 / Estimated effort and risk level

---

## 13. 影分身零容忍策略（Shadow Clone Zero Tolerance） / Shadow Clone Code Zero Tolerance

> **⚠️ 危险警告 / Danger Warning**: 影分身代码是最危险的技术债务类型 / Shadow clone code is the most dangerous type of technical debt

### 什么是"影分身"？/ What is "Shadow Clone"?

**影分身** 是指功能相同或高度相似的重复代码，表现形式包括：

**Shadow Clone** refers to duplicate code with identical or highly similar functionality, manifesting as:

1. **重复接口 / Duplicate Interfaces** - 同一职责出现第二个接口 / Second interface for same responsibility
2. **纯转发类型 / Pure Forwarding Types** - 只做方法转发、不增加任何实质逻辑的 Facade/Adapter/Wrapper/Proxy
3. **重复 DTO/Model** - 多处存在字段结构完全一致的数据传输对象 / DTOs with identical structure in multiple places
4. **重复 Options/Settings** - 多处定义相同的配置类 / Configuration classes defined in multiple places
5. **重复工具方法 / Duplicate Utility Methods** - 在不同类中重复实现相同逻辑的辅助方法
6. **重复常量 / Duplicate Constants** - 在多个类中定义语义相同的常量（魔法数字）

### 零容忍策略 / Zero Tolerance Policy

**规则 / Rules**:

1. **新增影分身 = PR 不合规 / New Shadow Clone = PR Non-Compliant**
   - 一旦发现新增的影分身类型，即视为当前 PR 不合规
   - PR 必须在当前分支中删除该影分身类型或合并到既有实现中
   - **不能** "先留下以后再清理" / Cannot "leave it for later cleanup"

2. **历史影分身必须优先清理 / Historical Shadow Clones Must Be Cleaned First**
   - 若在当前 PR 涉及对应模块或调用链，必须优先尝试清理
   - 如短期内无法彻底清理，必须登记技术债并规划清理 PR

3. **禁止行为 / Prohibited Actions**
   - ❌ 新增任何形式的"影分身"类型，并期望后续再清理
   - ❌ 保留一套 Legacy 实现与一套新实现并存
   - ❌ 在 PR 描述中以"与本次改动无关"为理由保留新增影分身

### 纯转发 Facade/Adapter 判定标准 / Pure Forwarding Judgment Criteria

**判定为影分身的条件 / Conditions for Shadow Clone**:
- 类型以 `*Facade` / `*Adapter` / `*Wrapper` / `*Proxy` 结尾
- 只持有 1~2 个服务接口字段
- 方法体只做直接调用另一个服务的方法，没有任何附加逻辑

**合法的附加逻辑包括 / Legal Additional Logic Includes**:
- 类型转换/协议映射逻辑（如 LINQ Select、new 对象初始化器）
- 事件订阅/转发机制（如 `+=` 事件绑定）
- 状态跟踪（如 `_lastKnownState` 字段）
- 批量操作聚合（如 `foreach` + `await`）
- 验证或重试逻辑

### 示例 / Examples

```csharp
// ❌ 错误：纯转发适配器（影分身）/ Wrong: Pure forwarding adapter (shadow clone)
public class LoggerAdapter : ICustomLogger
{
    private readonly ILogger _logger;

    public void LogInformation(string message, params object[] args)
    {
        _logger.LogInformation(message, args);  // ❌ 一行转发，无附加值 / One-line forwarding, no added value
    }
}

// ✅ 正确：直接使用 ILogger，删除无意义包装 / Correct: Use ILogger directly, remove meaningless wrapper
public class OrderService
{
    private readonly ILogger _logger;  // ✅ 直接依赖 ILogger / Direct dependency on ILogger

    public OrderService(ILogger logger)
    {
        _logger = logger;
    }
}

// ✅ 正确：有附加值的适配器（类型转换 + 事件订阅）/ Correct: Adapter with added value
public class SensorEventProviderAdapter : ISensorEventProvider
{
    private readonly IHardwareSensorService _hardwareService;

    public SensorEventProviderAdapter(IHardwareSensorService service)
    {
        _hardwareService = service;
        _hardwareService.SensorTriggered += OnHardwareSensorTriggered;  // ✅ 事件订阅 / Event subscription
    }

    private void OnHardwareSensorTriggered(object? sender, HardwareSensorEventArgs e)
    {
        // ✅ 类型转换和协议映射 / Type conversion and protocol mapping
        var domainEvent = new SensorDetectedArgs
        {
            SensorId = e.DeviceId,
            DetectedAt = e.Timestamp,
            SignalStrength = e.RawValue
        };
        SensorDetected?.Invoke(this, domainEvent);
    }

    public event EventHandler<SensorDetectedArgs>? SensorDetected;
}
```

---

## 14. 冗余代码零容忍策略 / Dead Code Zero Tolerance

> **⚠️ 危险警告 / Danger Warning**: 冗余代码（Dead Code）是项目的隐形负担 / Dead code is a hidden burden

### 什么是"冗余代码"？/ What is "Dead Code"?

**冗余代码** 是指已经定义但从未被实际使用的代码：

**Dead Code** refers to code that has been defined but is never actually used:

1. **未在 DI 注册的服务 / Services Not Registered in DI** - 定义了接口和实现，但从未在依赖注入容器中注册
2. **已注册但从未被注入的服务 / Registered but Never Injected Services** - 在 DI 容器中注册，但没有任何地方通过构造函数注入使用
3. **已注入但从未调用的服务 / Injected but Never Called Services** - 通过构造函数注入，但从未调用其任何方法或属性
4. **未使用的方法和属性 / Unused Methods and Properties** - 在类中定义，但在整个解决方案中从未被调用
5. **未使用的类型 / Unused Types** - 定义的类、接口、枚举等，从未被引用

### 零容忍策略 / Zero Tolerance Policy

**规则 / Rules**:

1. **新增冗余代码 = PR 不合规 / New Dead Code = PR Non-Compliant**
   - 所有新增的类型、方法、属性必须有实际使用场景
   - 在提交 PR 前必须检查代码是否被实际使用

2. **禁止行为 / Prohibited Actions**
   - ❌ "先实现，以后可能会用到"的提前设计 / "Implement first, might use later" premature design
   - ❌ 保留"可能有用"的代码 / Keep "potentially useful" code
   - ❌ 注释掉代码而不删除（使用版本控制系统）/ Comment out code instead of deleting (use VCS)

### 检测方法 / Detection Methods

**使用 IDE 的"查找所有引用"功能 / Use IDE's "Find All References" feature**:
- Visual Studio: Right-click → Find All References
- Rider: Right-click → Find Usages
- VS Code: Right-click → Find All References

---

## 15. Id 类型统一规范 / Id Type Unification Standard

> **规则 / Rule**: 统一 Id 类型可以避免类型不一致导致的转换错误和混淆 / Unified Id types avoid conversion errors and confusion

### Id 必须使用 long 类型 / Id Must Use long Type

**规则 / Rules**:

1. 除数据库自增主键或外部系统强制使用特定类型的 Key 以外，所有内部定义的 Id 均必须使用 `long` 类型
2. 禁止在同一语义下混用 `int` 与 `long`

**允许的例外 / Allowed Exceptions**:
- 数据库表中已有历史字段为 `int` 且暂时无法迁移时
- 外部系统接口明确要求使用 `int`、`Guid` 或其他类型时

**示例 / Example**:

```csharp
// ✅ 正确：统一使用 long 类型 / Correct: Unified use of long type
public class User
{
    public long UserId { get; set; }
    public string UserName { get; set; }
}

public record UserDto(
    long UserId,
    string UserName,
    string Email
);

// ❌ 错误：混用 int 和 long / Wrong: Mixing int and long
public class User
{
    public int UserId { get; set; }  // ❌ 使用 int / Using int
}

public record UserDto(
    long UserId,  // ❌ 这里使用 long，与领域模型不一致 / Using long here, inconsistent with domain model
    string UserName
);
```

---

## 16. 时间处理规范 / Time Handling Standards

### 统一使用时间抽象接口 / Use Time Abstraction Interface

**规则 / Rules**: 
- 所有时间获取必须通过抽象接口（如 `ISystemClock`）
- **严格禁止** 直接使用 `DateTime.Now` 或 `DateTime.UtcNow`

**原因 / Reasons**:
- 便于单元测试（可以 Mock 时间）/ Easy to unit test (can mock time)
- 统一时区管理 / Unified timezone management
- 避免时区转换错误 / Avoid timezone conversion errors
- 支持时间旅行测试场景 / Support time-travel testing scenarios

**LocalNow vs UtcNow 选择指南 / LocalNow vs UtcNow Selection Guide**:
- **推荐使用 `LocalNow`** / **Recommended: LocalNow**: 大多数业务场景（日志、记录、显示、业务逻辑）
- **仅在特定场景使用 `UtcNow`** / **Use UtcNow only in specific scenarios**:
  - 与外部系统通信时，协议明确要求 UTC 时间
  - 跨时区的分布式系统需要统一时间基准
  - 存储到数据库时需要 UTC（但显示时转换为本地时间）

### 示例 / Example

```csharp
// ✅ 正确 / Correct
public class OrderService
{
    private readonly ISystemClock _clock;
    
    public OrderService(ISystemClock clock)
    {
        _clock = clock;
    }
    
    public Order CreateOrder(string customerId)
    {
        return new Order
        {
            OrderId = GenerateId(),
            CreatedAt = _clock.LocalNow,  // ✅ 使用抽象接口 / Use abstract interface
            CustomerId = customerId
        };
    }
}

// ❌ 错误 / Wrong
public Order CreateOrder(string customerId)
{
    return new Order
    {
        OrderId = GenerateId(),
        CreatedAt = DateTime.Now  // ❌ 禁止直接使用 / Prohibited direct use
    };
}
```

---

## 17. 并发安全规范 / Concurrency Safety Standards

### 跨线程共享集合必须使用线程安全容器 / Thread-Safe Containers Required

**规则 / Rules**: 任何跨线程共享的集合必须使用线程安全容器或明确的锁封装

**线程安全容器 / Thread-Safe Containers**:
- `ConcurrentDictionary<TKey, TValue>`
- `ConcurrentQueue<T>`
- `ConcurrentBag<T>`
- `ConcurrentStack<T>`
- `ImmutableList<T>` / `ImmutableDictionary<TKey, TValue>`

### 示例 / Example

```csharp
// ✅ 正确：使用线程安全容器 / Correct: Use thread-safe container
public class SessionTracker
{
    private readonly ConcurrentDictionary<string, SessionState> _sessions = new();
    
    public void UpdateSession(string sessionId, SessionState state)
    {
        _sessions.AddOrUpdate(sessionId, state, (_, __) => state);
    }
}

// ✅ 正确：使用明确的锁 / Correct: Use explicit lock
public class SessionTracker
{
    private readonly Dictionary<string, SessionState> _sessions = new();
    private readonly object _lock = new();
    
    public void UpdateSession(string sessionId, SessionState state)
    {
        lock (_lock)
        {
            _sessions[sessionId] = state;
        }
    }
}

// ❌ 错误：非线程安全 / Wrong: Not thread-safe
public class SessionTracker
{
    private readonly Dictionary<string, SessionState> _sessions = new();
    
    public void UpdateSession(string sessionId, SessionState state)
    {
        _sessions[sessionId] = state;  // ❌ 多线程不安全 / Multi-threaded unsafe
    }
}
```

---

## 18. API 设计规范增强 / Enhanced API Design Standards

### Swagger 文档注释规范（强制要求）/ Swagger Documentation Annotations (Mandatory)

**规则 / Rules**: 所有 API 端点必须具有完整的 Swagger 注释

**强制要求 / Mandatory Requirements**:
1. **Controller 类注释** / **Controller Class Annotations**:
   - 每个 Controller 类必须有完整的 `/// <summary>` 注释
   - 可选：使用 `/// <remarks>` 提供详细说明

2. **Action 方法注释** / **Action Method Annotations**:
   - 每个 Action 方法必须使用 `[SwaggerOperation]` 特性
   - 必须包含：`Summary`、`Description`、`OperationId`、`Tags`
   - 每个 Action 方法必须使用 `[SwaggerResponse]` 特性标注**所有可能的响应码**
   - 包括成功响应（200、201、204）和错误响应（400、401、403、404、500）

3. **DTO 属性注释** / **DTO Property Annotations**:
   - 请求/响应 DTO 的所有属性必须有 `/// <summary>` 注释
   - 复杂字段应使用 `/// <remarks>` 提供详细说明
   - 使用 `/// <example>` 提供示例值

**禁止行为 / Prohibited Actions**:
- ❌ 新增没有任何注释的 API 端点
- ❌ 使用占位描述（如 "TODO"、"Test"）
- ❌ 只标注成功响应，不标注错误响应
- ❌ DTO 属性没有 `<summary>` 注释

### 示例 / Example

```csharp
/// <summary>
/// 用户管理 API / User Management API
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    /// <summary>
    /// 获取用户信息 / Get user information
    /// </summary>
    /// <param name="id">用户 ID / User ID</param>
    /// <returns>用户详细信息 / User details</returns>
    /// <response code="200">成功返回用户信息 / Successfully returns user information</response>
    /// <response code="404">用户不存在 / User not found</response>
    /// <response code="500">服务器内部错误 / Internal server error</response>
    [HttpGet("{id}")]
    [SwaggerOperation(
        Summary = "获取用户信息 / Get user information",
        Description = "根据用户 ID 获取用户详细信息 / Get user details by user ID",
        OperationId = "GetUser",
        Tags = new[] { "用户管理 / User Management" }
    )]
    [SwaggerResponse(200, "成功返回用户信息", typeof(ApiResponse<UserDto>))]
    [SwaggerResponse(404, "用户不存在", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUser([FromRoute] long id)
    {
        // Implementation
    }
}

/// <summary>
/// 用户数据传输对象 / User DTO
/// </summary>
public record UserDto
{
    /// <summary>
    /// 用户唯一标识 / User unique identifier
    /// </summary>
    /// <example>1001</example>
    public long UserId { get; init; }
    
    /// <summary>
    /// 用户名 / Username
    /// </summary>
    /// <example>john_doe</example>
    public string UserName { get; init; } = string.Empty;
}
```

---

## 19. 代码审查清单 / Code Review Checklist

在提交代码前，请检查 / Before submitting code, please check:

### PR 完整性 / PR Integrity
- [ ] PR 可独立编译、测试通过 / PR can compile independently and tests pass
- [ ] 未留下"TODO: 后续PR"标记 / No "TODO: next PR" markers
- [ ] 大型 PR 的未完成部分已登记技术债 / Incomplete parts of large PRs documented as tech debt

### 影分身检查（最重要）/ Shadow Clone Check (Most Important)
- [ ] 未创建纯转发 Facade/Adapter/Wrapper/Proxy / No pure forwarding facades/adapters/wrappers/proxies
- [ ] 未重复定义相同的工具方法 / No duplicate utility methods
- [ ] 未重复定义相同结构的 DTO/Model / No duplicate DTOs/Models with same structure
- [ ] 未重复定义相同的 Options/Settings / No duplicate Options/Settings
- [ ] 未在多处定义相同的常量 / No duplicate constants
- [ ] 已清理历史影分身（如果涉及相关模块）/ Cleaned historical shadow clones (if related modules involved)

### 冗余代码检查 / Dead Code Check
- [ ] 未定义从未在 DI 中注册的服务 / No services defined but never registered in DI
- [ ] 未注册从未被注入使用的服务 / No registered services never injected
- [ ] 未注入从未调用的服务 / No injected services never called
- [ ] 未定义从未使用的方法和属性 / No unused methods and properties
- [ ] 未定义从未使用的类型 / No unused types

### Id 类型规范 / Id Type Standards
- [ ] 所有内部 Id 统一使用 `long` 类型 / All internal Ids use long type
- [ ] 未混用 `int` 和 `long` 作为同一语义的 Id / No mixing int and long for same semantic Id

### 时间处理 / Time Handling
- [ ] 所有时间通过抽象接口（如 `ISystemClock`）获取 / All time obtained through abstract interface
- [ ] 未直接使用 `DateTime.Now` / `DateTime.UtcNow` / No direct use of DateTime.Now/DateTime.UtcNow

### 并发安全 / Concurrency Safety
- [ ] 跨线程集合使用线程安全容器或锁 / Thread-shared collections use thread-safe containers or locks

### API 设计 / API Design
- [ ] **所有 API 端点有完整的 Swagger 注释** / **All API endpoints have complete Swagger annotations**
- [ ] Controller 类有 `<summary>` 注释 / Controller classes have `<summary>` annotations
- [ ] Action 方法有 `[SwaggerOperation]` 特性 / Action methods have `[SwaggerOperation]` attribute
- [ ] Action 方法标注了所有可能的响应码 / Action methods annotate all possible response codes
- [ ] DTO 属性有 `<summary>` 注释 / DTO properties have `<summary>` annotations

### 通用规范 / General Standards
- [ ] 类型使用（record / readonly struct / file / required + init）/ Type usage
- [ ] 可空引用类型 / Nullable reference types
- [ ] 异常处理 / Exception handling
- [ ] 方法设计（单一职责、< 50 行）/ Method design
- [ ] 命名约定 / Naming conventions
- [ ] 分层架构 / Layered architecture
- [ ] 测试覆盖 / Test coverage
- [ ] 代码清理 / Code cleanup
