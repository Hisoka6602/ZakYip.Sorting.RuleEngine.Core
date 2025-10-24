# Version 1.2.0 更新说明 / Release Notes

**发布日期 / Release Date**: 2025-10-24  
**版本 / Version**: 1.2.0

---

## 重大变更 / Breaking Changes

### 1. 项目重命名 / Project Rename
- **之前 / Before**: `ZakYip.Sorting.RuleEngine.Core`
- **现在 / Now**: `ZakYip.Sorting.RuleEngine`

**影响 / Impact**:
- 解决方案文件名已更改
- 核心项目文件夹和项目文件已重命名
- 命名空间已更新
- 所有文档已更新

**迁移指南 / Migration Guide**:
如果您有引用此项目的代码，需要更新引用：
- 更新 `.sln` 文件引用
- 更新项目引用路径
- 重新构建解决方案

---

## 新功能 / New Features

### 1. 基于空闲的数据清理策略 / Idle-Based Data Cleanup Strategy

**之前的行为 / Previous Behavior**:
- 使用定时任务，每天凌晨 2 点执行清理
- 无法根据系统活动状态调整

**新的行为 / New Behavior**:
- 当系统空闲（默认 30 分钟无包裹创建）时触发清理
- 清理可被新包裹创建打断
- 防止频繁清理（最小间隔 1 小时）

**新增配置项 / New Configuration**:
```json
"Sharding": {
  "IdleMinutesBeforeCleanup": 30,        // 触发清理的空闲分钟数
  "IdleCheckIntervalSeconds": 60         // 检查空闲状态的间隔秒数
}
```

**实现细节 / Implementation Details**:
- 新增 `IParcelActivityTracker` 接口
- 新增 `ParcelActivityTracker` 实现类
- 修改 `DataCleanupService` 使用空闲检测
- 在 `ParcelOrchestrationService` 中集成活动跟踪

**优势 / Benefits**:
- ✅ 更智能的清理时机
- ✅ 避免在高峰期清理影响性能
- ✅ 根据实际业务情况调整
- ✅ 可配置的空闲阈值

---

### 2. 分片表自动创建和管理 / Automatic Sharding Table Management

**功能描述 / Description**:
新增后台服务自动创建和管理分片表，确保数据到达前表已存在。

**实现细节 / Implementation Details**:
- 新增 `ShardingTableManagementService` 后台服务
- 支持三种分片策略：
  - **Daily（日度）**: 提前创建 7 天的表
  - **Weekly（周度）**: 提前创建 4 周的表
  - **Monthly（月度）**: 提前创建 3 个月的表
- 每小时检查并创建缺失的表
- 自动创建索引以优化性能

**表命名规则 / Table Naming Convention**:
- 日度: `parcel_log_entries_20241024`
- 周度: `parcel_log_entries_2024W43`
- 月度: `parcel_log_entries_202410`

**索引自动创建 / Automatic Index Creation**:
- 主键: `Id`
- 降序索引: `CreatedAt DESC`
- 单列索引: `ParcelId`
- 复合索引: `(ParcelId, CreatedAt)`

**优势 / Benefits**:
- ✅ 自动管理，无需手动干预
- ✅ 提前创建表，避免运行时创建影响性能
- ✅ 支持多种分片策略
- ✅ 自动优化索引

---

## 文件变更 / File Changes

### 新增文件 / New Files
1. `ZakYip.Sorting.RuleEngine.Domain/Interfaces/IParcelActivityTracker.cs`
2. `ZakYip.Sorting.RuleEngine.Infrastructure/Services/ParcelActivityTracker.cs`
3. `ZakYip.Sorting.RuleEngine.Infrastructure/BackgroundServices/ShardingTableManagementService.cs`
4. `CHANGES_V1.2.0.md` (本文件 / This file)

### 修改文件 / Modified Files
1. `ZakYip.Sorting.RuleEngine.Infrastructure/BackgroundServices/DataCleanupService.cs`
   - 添加空闲检测逻辑
   - 注入 `IParcelActivityTracker`
   - 移除定时触发逻辑

2. `ZakYip.Sorting.RuleEngine.Infrastructure/Sharding/ShardingSettings.cs`
   - 添加 `IdleMinutesBeforeCleanup` 配置
   - 添加 `IdleCheckIntervalSeconds` 配置
   - 标记 `CleanupSchedule` 为过期

3. `ZakYip.Sorting.RuleEngine.Application/Services/ParcelOrchestrationService.cs`
   - 注入 `IParcelActivityTracker`（可选）
   - 在创建包裹时记录活动

4. `ZakYip.Sorting.RuleEngine.Service/Program.cs`
   - 注册 `IParcelActivityTracker` 服务
   - 注册 `ShardingTableManagementService` 后台服务

5. `ZakYip.Sorting.RuleEngine.Service/appsettings.json`
   - 添加空闲清理配置项

### 重命名文件 / Renamed Files
1. `ZakYip.Sorting.RuleEngine.Core.sln` → `ZakYip.Sorting.RuleEngine.sln`
2. `ZakYip.Sorting.RuleEngine.Core/` → `ZakYip.Sorting.RuleEngine/`
3. `ZakYip.Sorting.RuleEngine.Core.csproj` → `ZakYip.Sorting.RuleEngine.csproj`

### 文档更新 / Documentation Updates
- `README.md` - 更新项目名称和新功能说明
- `IMPLEMENTATION_COMPLETE.md` - 更新项目名称
- `IMPLEMENTATION_SUMMARY.md` - 添加版本 1.2.0 更新摘要
- `API_QUICK_REFERENCE.md` - 更新项目名称
- `DEPLOYMENT.md` - 更新项目名称
- `SUMMARY.md` - 更新项目名称
- `USAGE.md` - 更新项目名称

---

## 配置变更 / Configuration Changes

### 新增配置项 / New Configuration Options

```json
{
  "AppSettings": {
    "Sharding": {
      "Enabled": true,
      "Strategy": "Monthly",
      "RetentionDays": 90,
      "ColdDataThresholdDays": 30,
      "IdleMinutesBeforeCleanup": 30,      // ⬅️ 新增
      "IdleCheckIntervalSeconds": 60,      // ⬅️ 新增
      "CleanupSchedule": "0 0 2 * * ?",    // ⚠️ 已废弃
      "ArchiveSchedule": "0 0 3 * * ?"
    }
  }
}
```

**配置说明 / Configuration Description**:

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `IdleMinutesBeforeCleanup` | int | 30 | 系统空闲多少分钟后触发清理 |
| `IdleCheckIntervalSeconds` | int | 60 | 检查空闲状态的间隔（秒）|

---

## 兼容性 / Compatibility

### 向后兼容性 / Backward Compatibility
- ✅ API 端点保持不变
- ✅ 数据库架构保持不变
- ✅ 配置文件向后兼容（新增配置项有默认值）
- ⚠️ 项目名称变更需要更新引用

### 依赖版本 / Dependencies
无依赖版本变更 / No dependency version changes

---

## 测试结果 / Test Results

### 单元测试 / Unit Tests
- **总计 / Total**: 14
- **通过 / Passed**: 14 ✅
- **失败 / Failed**: 0
- **跳过 / Skipped**: 0

### 构建状态 / Build Status
- **状态 / Status**: ✅ 成功 / Success
- **警告 / Warnings**: 0
- **错误 / Errors**: 0

---

## 性能影响 / Performance Impact

### 空闲检测开销 / Idle Detection Overhead
- **内存影响 / Memory**: 极小（单个 DateTime 和锁对象）
- **CPU 影响 / CPU**: 极小（每 60 秒检查一次）
- **性能影响 / Performance**: 可忽略

### 分片表管理开销 / Sharding Table Management Overhead
- **内存影响 / Memory**: 极小
- **CPU 影响 / CPU**: 极小（每小时检查一次）
- **数据库影响 / Database**: 仅在表不存在时创建

---

## 升级指南 / Upgrade Guide

### 1. 备份数据 / Backup Data
```bash
# 备份配置数据库
cp data/config.db data/config.db.backup

# 备份日志数据库（如果使用 MySQL）
mysqldump -u root -p sorting_logs > backup_before_upgrade.sql
```

### 2. 更新代码 / Update Code
```bash
git pull origin main
```

### 3. 更新配置 / Update Configuration
在 `appsettings.json` 中添加新配置项（可选，有默认值）：
```json
"Sharding": {
  "IdleMinutesBeforeCleanup": 30,
  "IdleCheckIntervalSeconds": 60
}
```

### 4. 重新构建 / Rebuild
```bash
dotnet build ZakYip.Sorting.RuleEngine.sln
```

### 5. 运行测试 / Run Tests
```bash
dotnet test ZakYip.Sorting.RuleEngine.sln
```

### 6. 重启服务 / Restart Service
```bash
# 如果作为 Windows 服务运行
sc stop "ZakYipSortingEngine"
sc start "ZakYipSortingEngine"
```

---

## 已知问题 / Known Issues

无 / None

---

## 未来计划 / Future Plans

1. 支持更多分片策略（如按小时）
2. 提供分片表清理和归档的 UI 界面
3. 添加分片表健康检查和报告
4. 支持动态调整空闲阈值

---

## 贡献者 / Contributors

- Hisoka6602

---

## 相关链接 / Related Links

- [项目主页 / Project Home](https://github.com/Hisoka6602/ZakYip.Sorting.RuleEngine.Core)
- [问题反馈 / Issue Tracker](https://github.com/Hisoka6602/ZakYip.Sorting.RuleEngine.Core/issues)
- [文档 / Documentation](./README.md)

---

**感谢使用 ZakYip 分拣规则引擎！ / Thank you for using ZakYip Sorting Rule Engine!**
