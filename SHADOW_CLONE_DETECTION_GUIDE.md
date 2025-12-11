# 影分身检测使用指南 / Shadow Clone Detection Usage Guide

## 快速开始 / Quick Start

### 开发者使用 / Developer Usage

在提交 PR 之前，运行以下检查：

Before submitting a PR, run the following checks:

```bash
# 1. 传统代码重复检测 / Traditional code duplication detection
jscpd . --pattern "**/*.cs" --ignore "**/bin/**,**/obj/**,**/Migrations/**,**/Tests/**"

# 2. 影分身语义检测 / Shadow clone semantic detection
./shadow-clone-check.sh .
```

### CI/CD 自动检测 / CI/CD Automated Detection

所有 PR 会自动运行两个检测：

All PRs will automatically run two detections:

1. **jscpd 代码重复检测** - 行级重复检测，阈值 5%
2. **Shadow Clone 语义检测** - 7 种类型的语义重复检测，阈值 80%

## 7 种检查类型详解 / 7 Types of Checks Explained

### 1. 枚举检查 / Enum Check

**检查内容 / What to Check:**
- 是否新增枚举？/ New enums added?
- 是否与现有枚举成员相似？/ Similar to existing enum members?
- 是否可以合并或抽象？/ Can be merged or abstracted?

**示例场景 / Example Scenarios:**

❌ **不好 / Bad:**
```csharp
public enum OrderStatus { Pending, Processing, Shipped, Delivered, Cancelled }
public enum ShipmentStatus { Pending, Processing, Shipped, Delivered, Cancelled }
```

✅ **好 / Good:**
```csharp
// 合并为通用状态枚举 / Merge into common status enum
public enum ProcessStatus { Pending, Processing, Shipped, Delivered, Cancelled }
```

### 2. 接口检查 / Interface Check

**检查内容 / What to Check:**
- 是否新增接口？/ New interfaces added?
- 是否与现有接口方法签名重叠？/ Method signatures overlap with existing interfaces?
- 是否可以使用泛型接口？/ Can use generic interfaces?

**示例场景 / Example Scenarios:**

❌ **不好 / Bad:**
```csharp
public interface IUserRepository {
    Task<User> GetByIdAsync(int id);
    Task<List<User>> GetAllAsync();
    Task SaveAsync(User entity);
}

public interface IProductRepository {
    Task<Product> GetByIdAsync(int id);
    Task<List<Product>> GetAllAsync();
    Task SaveAsync(Product entity);
}
```

✅ **好 / Good:**
```csharp
// 使用泛型基接口 / Use generic base interface
public interface IRepository<T> where T : class {
    Task<T> GetByIdAsync(int id);
    Task<List<T>> GetAllAsync();
    Task SaveAsync(T entity);
}

public interface IUserRepository : IRepository<User> { }
public interface IProductRepository : IRepository<Product> { }
```

### 3. DTO 检查 / DTO Check

**检查内容 / What to Check:**
- 是否新增 DTO？/ New DTOs added?
- 是否与现有 DTO 字段结构相同？/ Field structures identical to existing DTOs?
- 是否可以复用或继承？/ Can be reused or inherited?

**示例场景 / Example Scenarios:**

❌ **不好 / Bad:**
```csharp
public class UserDto {
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CustomerDto {
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

✅ **好 / Good:**
```csharp
// 使用基类或直接复用 / Use base class or reuse directly
public class PersonDto {
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserDto : PersonDto { }
public class CustomerDto : PersonDto { }
```

### 4. Options/配置类检查 / Options/Config Class Check

**检查内容 / What to Check:**
- 是否新增配置类？/ New config classes added?
- 是否在多个命名空间重复？/ Duplicated across multiple namespaces?
- 是否可以统一到共享配置？/ Can be unified into shared configuration?

**示例场景 / Example Scenarios:**

❌ **不好 / Bad:**
```csharp
namespace Service.Configuration {
    public class CircuitBreakerSettings {
        public int MaxRetries { get; set; } = 3;
        public int TimeoutSeconds { get; set; } = 30;
    }
}

namespace Infrastructure.Configuration {
    public class CircuitBreakerSettings {
        public int MaxRetries { get; set; } = 3;
        public int TimeoutSeconds { get; set; } = 30;
    }
}
```

✅ **好 / Good:**
```csharp
// 统一到共享配置命名空间 / Unify to shared configuration namespace
namespace Shared.Configuration {
    public class CircuitBreakerSettings {
        public int MaxRetries { get; set; } = 3;
        public int TimeoutSeconds { get; set; } = 30;
    }
}
```

### 5. 扩展方法检查 / Extension Method Check

**检查内容 / What to Check:**
- 是否新增扩展方法？/ New extension methods added?
- 是否与现有扩展方法签名相同？/ Signatures identical to existing extension methods?
- 是否可以合并到同一扩展类？/ Can be merged into the same extension class?

**示例场景 / Example Scenarios:**

❌ **不好 / Bad:**
```csharp
public static class StringExtensions1 {
    public static bool IsNullOrEmpty(this string str) 
        => string.IsNullOrEmpty(str);
}

public static class StringExtensions2 {
    public static bool IsNullOrEmpty(this string str) 
        => string.IsNullOrEmpty(str);
}
```

✅ **好 / Good:**
```csharp
// 合并到一个扩展类 / Merge into one extension class
public static class StringExtensions {
    public static bool IsNullOrEmpty(this string str) 
        => string.IsNullOrEmpty(str);
}
```

### 6. 静态类检查 / Static Class Check

**检查内容 / What to Check:**
- 是否新增静态工具类？/ New static utility classes added?
- 是否与现有工具类功能重复？/ Functionality duplicates existing utility classes?
- 是否可以合并或重构？/ Can be merged or refactored?

**示例场景 / Example Scenarios:**

❌ **不好 / Bad:**
```csharp
public static class DateHelper {
    public static DateTime GetStartOfDay(DateTime date) => date.Date;
    public static DateTime GetEndOfDay(DateTime date) => date.Date.AddDays(1).AddTicks(-1);
}

public static class TimeHelper {
    public static DateTime GetStartOfDay(DateTime date) => date.Date;
    public static DateTime GetEndOfDay(DateTime date) => date.Date.AddDays(1).AddTicks(-1);
}
```

✅ **好 / Good:**
```csharp
// 合并为一个工具类 / Merge into one utility class
public static class DateTimeHelper {
    public static DateTime GetStartOfDay(DateTime date) => date.Date;
    public static DateTime GetEndOfDay(DateTime date) => date.Date.AddDays(1).AddTicks(-1);
}
```

### 7. 常量检查 / Constant Check

**检查内容 / What to Check:**
- 是否定义常量？/ Constants defined?
- 是否与现有常量值相同？/ Values identical to existing constants?
- 是否应该引用共享常量？/ Should reference shared constants?

**示例场景 / Example Scenarios:**

❌ **不好 / Bad:**
```csharp
public class ServiceA {
    public const int MaxRetries = 3;
    public const int TimeoutMs = 5000;
}

public class ServiceB {
    public const int MaxRetries = 3;      // 重复 / Duplicate
    public const int TimeoutMs = 5000;    // 重复 / Duplicate
}
```

✅ **好 / Good:**
```csharp
// 定义共享常量 / Define shared constants
public static class ConfigurationDefaults {
    public const int MaxRetries = 3;
    public const int TimeoutMs = 5000;
}

public class ServiceA {
    private const int MaxRetries = ConfigurationDefaults.MaxRetries;
    private const int TimeoutMs = ConfigurationDefaults.TimeoutMs;
}

public class ServiceB {
    private const int MaxRetries = ConfigurationDefaults.MaxRetries;
    private const int TimeoutMs = ConfigurationDefaults.TimeoutMs;
}
```

## PR 检查流程 / PR Check Process

### 步骤 1: 开发前检查 / Step 1: Pre-development Check

```bash
# 查看现有的技术债务 / Review existing technical debt
cat TECHNICAL_DEBT.md

# 运行检测工具了解当前状态 / Run detection tools to understand current state
./shadow-clone-check.sh .
```

### 步骤 2: 开发过程中 / Step 2: During Development

- 遵循编码规范 / Follow coding standards
- 优先复用现有代码 / Prioritize reusing existing code
- 使用泛型和继承减少重复 / Use generics and inheritance to reduce duplication

### 步骤 3: 提交前检查 / Step 3: Pre-submission Check

```bash
# 1. 运行影分身检测 / Run shadow clone detection
./shadow-clone-check.sh .

# 2. 运行代码重复检测 / Run code duplication detection
jscpd .

# 3. 检查是否需要更新技术债务文档 / Check if technical debt document needs updating
# 如果修复了债务，更新 TECHNICAL_DEBT.md
# If fixed debt, update TECHNICAL_DEBT.md
```

### 步骤 4: 填写 PR 模板 / Step 4: Fill PR Template

在 PR 描述中勾选所有检查项：

Check all items in the PR description:

- ✅ 基础检查 / Basic checks
- ✅ 7 种影分身语义检查 / 7 types of shadow clone semantic checks

### 步骤 5: 响应 CI 反馈 / Step 5: Respond to CI Feedback

如果 CI 检测到问题：

If CI detects issues:

1. 查看 CI 输出了解具体问题 / Review CI output for specific issues
2. 下载检测报告查看详情 / Download detection report for details
3. 修复问题后重新提交 / Fix issues and resubmit

## 常见问题 / FAQ

### Q1: 为什么需要两种检测工具？

**A**: 
- **jscpd**: 检测行级代码重复，发现完全相同的代码块
- **Shadow Clone Detector**: 检测语义重复，发现结构相似但不完全相同的代码

### Q2: 相似度阈值如何设置？

**A**: 
- 默认 80% 适用于大多数场景
- 可根据项目需要调整：70% (宽松) - 90% (严格)

### Q3: 发现影分身后如何处理？

**A**:
1. 评估是否可以合并或抽象
2. 如果可以，优先重构消除重复
3. 如果暂时无法修复，在 TECHNICAL_DEBT.md 中记录

### Q4: 某些重复是否可以接受？

**A**:
是的，某些情况下重复是合理的：
- 不同领域的业务模型
- 特定场景的性能优化
- 第三方接口适配

但必须在 TECHNICAL_DEBT.md 中说明原因。

### Q5: 工具会误报吗？

**A**:
可能存在误报，主要场景：
- 泛型类型参数不同
- 命名空间不同但确实需要分离
- 简单的数值常量

遇到误报时，可以在 PR 中说明。

## 最佳实践总结 / Best Practices Summary

1. **预防优于治疗 / Prevention Over Cure**
   - 编码前查看现有代码
   - 优先复用和继承
   - 定期运行检测工具

2. **及时记录 / Timely Documentation**
   - 发现债务立即记录
   - 说明保留原因
   - 规划修复时间

3. **持续改进 / Continuous Improvement**
   - 定期审查技术债务
   - 优先处理高影响项
   - 分享重构经验

4. **团队协作 / Team Collaboration**
   - Code Review 时关注重复
   - 讨论最佳抽象方案
   - 统一编码风格

---

**需要帮助？/ Need Help?**
- 查看 [TECHNICAL_DEBT.md](../TECHNICAL_DEBT.md)
- 查看 [Tools/ShadowCloneDetector/README.md](../Tools/ShadowCloneDetector/README.md)
- 联系项目维护者 / Contact project maintainers

**最后更新 / Last Updated**: 2025-12-11
