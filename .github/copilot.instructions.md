# GitHub Copilot 编码规范指南

本文档定义了ZakYip分拣规则引擎系统的代码规范和最佳实践。所有开发者和AI助手在参与项目开发时都必须遵循这些规范。

## 1. 文档维护规范

每次更新代码需要同步更新README.md内容，包括：
- 本次更新的内容说明
- 项目完成度评估
- 项目欠缺内容列表
- 项目优化方向建议
- 如果更新历史过多，仅保留最后3次更新内容

## 2. 测试要求

每次更新代码都需要先测试，确保：
- 编译无错误
- 调试无错误
- 无法调试的内容需要明确说明原因

## 3. 枚举定义规范

定义enum时必须：
- 使用Description特性标记每个枚举值
- 为每个枚举值添加中文注释
- 示例：
  ```csharp
  /// <summary>
  /// 包裹状态枚举
  /// </summary>
  public enum ParcelStatus
  {
      /// <summary>
      /// 已创建
      /// </summary>
      [Description("已创建")]
      Created = 0,
      
      /// <summary>
      /// 处理中
      /// </summary>
      [Description("处理中")]
      Processing = 1
  }
  ```

## 4. Record优先原则

如果record能满足需求，优先使用record而不是class，提高代码的不可变性和简洁性。

## 5. Record Class字段定义规范

在record class中：
- 尽量不把字段写在构造函数参数上
- 不可空的字段必须加上required关键字
- 示例：
  ```csharp
  public record class ParcelInfo
  {
      public required string ParcelId { get; init; }
      public required string Barcode { get; init; }
      public string? Description { get; init; }
  }
  ```

## 6. 事件载荷命名规范

事件载荷必须：
- 使用record struct或record class
- 命名以Event结尾
- 示例：`ParcelCreatedEvent`, `DwsDataReceivedEvent`

## 7. 布尔字段命名规范

布尔字段/属性若表示某个"是否状态"，必须使用以下前缀之一：
- Is（是否）
- Has（拥有）
- Can（能够）
- Should（应该）
- 示例：`IsEnabled`, `HasError`, `CanProcess`, `ShouldRetry`

## 8. 数值类型选择

- 优先使用decimal替代double，以提高精度
- 仅在以下场景使用double：
  - 需要极致性能的计算场景
  - 第三方库接口要求必须使用double
  - 无法使用decimal的技术限制场景

## 9. 架构层次规范（非常重要）

严格划分结构层级边界，尽量做到0入侵：
- Domain层：纯领域逻辑，不依赖基础设施
- Application层：应用服务，协调领域对象
- Infrastructure层：基础设施实现，依赖外部资源
- Service层：API和服务入口
- 各层之间通过接口依赖，避免直接引用具体实现

## 10. DRY原则（不要重复自己）

处理相同内容只在一处实现，多处调用，保证一致性：
- 提取公共方法
- 使用扩展方法
- 创建工具类
- 避免代码重复

## 11. 注释规范

注释中禁止出现第二人称字眼（你、您等），使用客观描述：
- ❌ 错误："你需要传入包裹ID"
- ✅ 正确："调用时需要传入包裹ID"
- ❌ 错误："你可以通过此方法获取数据"
- ✅ 正确："通过此方法可以获取数据"

## 12. 日志语言规范

日志和Console.WriteLine必须使用中文：
```csharp
_logger.LogInformation("包裹 {ParcelId} 处理成功", parcelId);
Console.WriteLine($"开始处理包裹：{parcelId}");
```

## 13. 代码注释语言

所有代码注释必须使用中文，包括：
- XML文档注释
- 行内注释
- 方法说明
- 参数说明

## 14. API测试要求

每个API端点都必须：
- 完成功能测试
- 确保可以正常访问
- 验证返回结果正确性
- 测试异常场景处理

## 15. 异常处理规范

每个可能抛出异常的地方都需要使用安全隔离器（如Polly策略）：
- 数据库操作使用重试策略
- 第三方API调用使用熔断器
- 关键操作使用超时控制
- 组合使用多种策略提高稳定性

## 16. 依赖包管理

保证引用包的一致性：
- 使用Directory.Packages.props中央包管理
- 所有项目使用相同版本的包
- 定期更新依赖包到最新稳定版本
- 避免版本冲突

## 17. 可运行项目文档

每个可运行的项目（API、控制台、exe等）都需要：
- 在项目目录生成README.md文件
- 说明项目用途
- 提供运行方法
- 列出配置说明
- 包含使用示例

## 18. PR创建说明

评估需要单独创建PR的操作，需要在项目的README.md中说明：
- 哪些操作需要单独PR
- PR的创建规范
- 代码审查要求
- 合并条件

## 19. 第三方API集成规范

集成第三方API时必须：
- 先创建单元测试
- 模拟API响应进行测试
- 确定测试可行后再输出正式代码
- 使用Mock对象隔离外部依赖
- 测试各种响应场景（成功、失败、超时等）

## 20. 语法糖使用

尽量使用C#语法糖，提高代码简洁性：
- 使用时需要添加注释说明等价代码
- 示例：
  ```csharp
  // 使用空合并赋值运算符 ??=
  // 等价于：if (value == null) value = defaultValue;
  value ??= defaultValue;
  ```

## 21. 性能优化

追求极致性能，优先使用高性能特性：
- 使用`[MethodImpl(MethodImplOptions.AggressiveInlining)]`标记热路径方法
- 使用Span<T>和Memory<T>减少内存分配
- 使用ValueTask替代Task减少分配
- 使用对象池复用对象
- 使用异步方法避免线程阻塞

## 22. 命名规范

对字段、类型、文件、项目的命名有严格要求，必须符合专业领域术语：
- 使用行业标准术语
- 遵循C#命名约定（PascalCase、camelCase）
- 类名使用名词
- 方法名使用动词
- 接口名以I开头
- 抽象类以Base或Abstract开头

## 23. 代码质量标准

提供的代码库必须保持最新版本实现，确保：
- 代码低耦合、高内聚
- 系统高可用、易维护
- 执行效率最高
- 性能消耗最低
- 尽量使用C# 12.0新特性
- 变量名和字段名符合行业标准
- 当LINQ性能更好时优先使用LINQ

---

**注意**：本规范是强制性的，所有代码提交前都必须经过review确保符合这些规范。违反规范的代码将不被接受。
