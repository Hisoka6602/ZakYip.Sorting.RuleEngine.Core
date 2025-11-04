# 事件驱动架构文档 (Event-Driven Architecture Documentation)

## 概述 (Overview)

本项目已完全集成MediatR，实现了完整的事件驱动架构。所有事件载荷均使用`record struct`或`record class`，确保值语义和不可变性。

The project has fully integrated MediatR, implementing a complete event-driven architecture. All event payloads use `record struct` or `record class` to ensure value semantics and immutability.

## 事件类型规范 (Event Type Specifications)

### 使用 record struct 的新事件 (New Events using record struct)

为新增的业务操作事件使用`record struct`，适合小型、轻量级的事件载荷：

1. **RuleCreatedEvent** - 规则创建事件
2. **RuleUpdatedEvent** - 规则更新事件
3. **RuleDeletedEvent** - 规则删除事件
4. **ChuteCreatedEvent** - 格口创建事件
5. **ChuteUpdatedEvent** - 格口更新事件
6. **ChuteDeletedEvent** - 格口删除事件
7. **DataArchivedEvent** - 数据归档事件
8. **DataCleanedEvent** - 数据清理事件
9. **ConfigurationCacheInvalidatedEvent** - 配置缓存失效事件
10. **ThirdPartyApiCalledEvent** - 第三方API调用事件

### 使用 record class 的核心事件 (Core Events using record class)

核心业务流程事件使用`record class`，适合包含复杂对象引用的事件：

1. **ParcelCreatedEvent** - 包裹创建事件
2. **DwsDataReceivedEvent** - DWS数据接收事件
3. **RuleMatchCompletedEvent** - 规则匹配完成事件

## 事件发布位置 (Event Publishing Locations)

### 1. API 控制器 (API Controllers)

#### RuleController
- **POST /api/rule** → 发布 `RuleCreatedEvent`
- **PUT /api/rule/{ruleId}** → 发布 `RuleUpdatedEvent`
- **DELETE /api/rule/{ruleId}** → 发布 `RuleDeletedEvent`

#### ChuteController
- **POST /api/chute** → 发布 `ChuteCreatedEvent` + `ConfigurationCacheInvalidatedEvent`
- **PUT /api/chute/{id}** → 发布 `ChuteUpdatedEvent` + `ConfigurationCacheInvalidatedEvent`
- **DELETE /api/chute/{id}** → 发布 `ChuteDeletedEvent` + `ConfigurationCacheInvalidatedEvent`

### 2. 后台服务 (Background Services)

#### DataArchiveService
- 数据归档完成后 → 发布 `DataArchivedEvent`

#### DataCleanupService
- 数据清理完成后 → 发布 `DataCleanedEvent`

### 3. 应用服务 (Application Services)

#### ParcelOrchestrationService
- 包裹创建 → 发布 `ParcelCreatedEvent`
- DWS数据接收 → 发布 `DwsDataReceivedEvent`
- 规则匹配完成 → 发布 `RuleMatchCompletedEvent`

#### DwsDataReceivedEventHandler
- 第三方API调用 → 发布 `ThirdPartyApiCalledEvent`

## 事件处理器 (Event Handlers)

每个事件都有对应的处理器，实现 `INotificationHandler<TEvent>` 接口：

### 规则相关处理器
- **RuleCreatedEventHandler** - 记录规则创建日志
- **RuleUpdatedEventHandler** - 记录规则更新日志
- **RuleDeletedEventHandler** - 记录规则删除日志

### 格口相关处理器
- **ChuteCreatedEventHandler** - 记录格口创建日志
- **ChuteUpdatedEventHandler** - 记录格口更新日志
- **ChuteDeletedEventHandler** - 记录格口删除日志

### 数据管理处理器
- **DataArchivedEventHandler** - 记录数据归档日志
- **DataCleanedEventHandler** - 记录数据清理日志

### 配置管理处理器
- **ConfigurationCacheInvalidatedEventHandler** - 记录缓存失效日志

### API调用处理器
- **ThirdPartyApiCalledEventHandler** - 记录第三方API调用日志（成功和失败）

### 核心业务处理器
- **ParcelCreatedEventHandler** - 处理包裹创建
- **DwsDataReceivedEventHandler** - 处理DWS数据接收并调用第三方API
- **RuleMatchCompletedEventHandler** - 处理规则匹配完成

## 事件流程示例 (Event Flow Examples)

### 示例1：创建规则 (Creating a Rule)

```
1. POST /api/rule
   ↓
2. RuleController.AddRule()
   ↓
3. _ruleRepository.AddAsync(rule)
   ↓
4. _publisher.Publish(RuleCreatedEvent)
   ↓
5. RuleCreatedEventHandler.Handle()
   ↓
6. _logRepository.LogInfoAsync("规则已创建")
```

### 示例2：包裹分拣流程 (Parcel Sorting Flow)

```
1. 分拣程序调用 CreateParcelAsync()
   ↓
2. ParcelOrchestrationService 发布 ParcelCreatedEvent
   ↓
3. ParcelCreatedEventHandler 记录日志
   ↓
4. DWS设备发送数据
   ↓
5. ParcelOrchestrationService 发布 DwsDataReceivedEvent
   ↓
6. DwsDataReceivedEventHandler 调用第三方API
   ↓
7. 发布 ThirdPartyApiCalledEvent
   ↓
8. ThirdPartyApiCalledEventHandler 记录API调用日志
   ↓
9. 规则匹配完成
   ↓
10. 发布 RuleMatchCompletedEvent
   ↓
11. RuleMatchCompletedEventHandler 记录匹配结果
```

### 示例3：数据清理流程 (Data Cleanup Flow)

```
1. DataCleanupService 检测到空闲时间
   ↓
2. 执行数据清理操作
   ↓
3. 发布 DataCleanedEvent
   ↓
4. DataCleanedEventHandler 记录清理日志
   ↓
5. 日志包含：清理的记录数、表名、截止日期、耗时
```

## 事件设计原则 (Event Design Principles)

### 1. 不可变性 (Immutability)
所有事件使用 `record` 类型，属性使用 `init` 关键字，确保事件创建后不可修改。

### 2. 值语义 (Value Semantics)
- 小型事件使用 `record struct`，减少堆分配
- 包含引用类型的复杂事件使用 `record class`

### 3. 必需属性 (Required Properties)
关键属性使用 `required` 关键字，确保事件创建时提供必要信息。

### 4. 时间戳 (Timestamps)
每个事件都包含时间戳字段（如 `CreatedAt`, `UpdatedAt`, `DeletedAt` 等），便于追踪和审计。

### 5. 单一职责 (Single Responsibility)
每个事件代表一个具体的业务操作，事件处理器专注于该操作的副作用处理。

## 事件命名规范 (Event Naming Conventions)

- 事件名称使用过去时态：`Created`, `Updated`, `Deleted`, `Archived`, `Cleaned`, `Called`
- 事件类名格式：`{Entity}{Action}Event`
- 事件处理器格式：`{Event}Handler`

## 扩展事件 (Extending Events)

### 添加新事件的步骤：

1. **定义事件** - 在 `Domain/Events` 文件夹创建事件类
```csharp
public record struct MyNewEvent : INotification
{
    public required string SomeId { get; init; }
    public DateTime CreatedAt { get; init; }
}
```

2. **创建处理器** - 在 `Application/EventHandlers` 文件夹创建处理器
```csharp
public class MyNewEventHandler : INotificationHandler<MyNewEvent>
{
    public async Task Handle(MyNewEvent notification, CancellationToken cancellationToken)
    {
        // 处理事件
    }
}
```

3. **发布事件** - 在适当的位置发布事件
```csharp
await _publisher.Publish(new MyNewEvent
{
    SomeId = id,
    CreatedAt = DateTime.Now
}, cancellationToken);
```

4. **MediatR自动注册** - 处理器会被自动注册到依赖注入容器

## 性能考虑 (Performance Considerations)

### 1. 异步处理
所有事件处理器都是异步的，不会阻塞主流程。

### 2. 轻量级事件
使用 `record struct` 减少内存分配，提高性能。

### 3. 批量操作
对于可能产生大量事件的操作（如数据归档），只在完成后发布一次汇总事件。

### 4. 可选处理器
事件处理器可以选择性实现，不需要的可以不创建。

## 总结 (Summary)

本项目的事件驱动架构：
- ✅ 完全集成MediatR
- ✅ 13个事件全面覆盖业务操作
- ✅ 所有事件使用 `record struct` 或 `record class`
- ✅ 事件处理器完整实现
- ✅ 支持异步处理和解耦
- ✅ 便于扩展和维护

通过事件驱动架构，系统实现了：
- 业务操作的审计追踪
- 组件间的松耦合
- 可扩展的副作用处理
- 清晰的职责分离
