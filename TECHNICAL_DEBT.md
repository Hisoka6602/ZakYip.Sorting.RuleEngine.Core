# 技术债务文档 / Technical Debt Documentation

本文档记录项目中已识别的技术债务。每次开启 PR 前必须通读此文档，确保不会引入新的技术债务，并在可能的情况下解决现有债务。

This document records identified technical debt in the project. Before opening any PR, this document must be read thoroughly to ensure no new technical debt is introduced and existing debt is resolved when possible.

---

## ⚠️ PR 提交前检查清单 / PR Submission Checklist

**提交 PR 前，请确认以下事项 / Before submitting a PR, please confirm the following:**

- [ ] 已通读本技术债务文档 / Have read this technical debt document
- [ ] 新代码未引入重复代码（影分身代码） / New code does not introduce duplicate code (shadow clone code)
- [ ] 运行 `jscpd` 检查重复代码比例未超过 5% / Run `jscpd` to check duplicate code ratio does not exceed 5%
- [ ] 如果解决了技术债务，已更新本文档 / If technical debt was resolved, this document has been updated
- [ ] 如果引入了新的技术债务，已在本文档中记录 / If new technical debt was introduced, it has been documented here

---

## 📊 当前技术债务概览 / Current Technical Debt Overview

| 类别 Category | 数量 Count | 严重程度 Severity | 状态 Status |
|--------------|-----------|-------------------|-------------|
| 重复代码 Duplicate Code | 65 处 | 🟢 低 Low | ✅ 已超越目标 |
| 代码重复率 Duplication Rate | 3.40% | 🟢 低 Low (✅ 低于 CI 阈值 5%，超越 SonarQube 目标 3%) | 已超越目标 |

> **注意 / Note:** CI 流水线阈值为 5%，SonarQube 目标为 3%。当前重复率已超越 SonarQube 目标！
> CI pipeline threshold is 5%, SonarQube target is 3%. Current duplication rate exceeds SonarQube target!

> **进展 / Progress:** 从 6.02% (93 clones) → 4.88% (79 clones) → 3.87% (69 clones) → **3.40% (65 clones)**，消除 601 行重复代码。
> Reduced from 6.02% (93 clones) → 4.88% (79 clones) → 3.87% (69 clones) → **3.40% (65 clones)**, eliminated 601 duplicate lines.

---

## 🔄 重复代码债务 / Duplicate Code Debt (影分身代码)

### 检测方法 / Detection Method

使用 `jscpd` 工具检测重复代码：
Use `jscpd` tool to detect duplicate code:

```bash
# 安装 / Install
npm install -g jscpd

# 运行检测 / Run detection
jscpd . --pattern "**/*.cs" --ignore "**/bin/**,**/obj/**,**/Migrations/**,**/Tests/**" --min-lines 10 --min-tokens 50
```

### 重复代码清单 / Duplicate Code Inventory

以下是当前项目中识别的主要重复代码区域（按严重程度排序）：

The following are the major duplicate code areas identified in the project (sorted by severity):

#### ✅ 已解决 / Resolved

| ID | 文件 Files | 原重复行数 Lines | 解决方案 Solution | 解决日期 Date |
|----|-----------|-----------------|-------------------|--------------|
| TD-DUP-001 | `PostCollectionApiClient.cs` ↔ `PostProcessingCenterApiClient.cs` | 249 行 | ✅ 已抽取 `BasePostalApiClient` 基类 / Extracted `BasePostalApiClient` base class | 2025-12-06 |
| TD-DUP-002 | `MySqlLogDbContext.cs` ↔ `SqliteLogDbContext.cs` | 157 行 | ✅ 已抽取 `BaseLogDbContext` 基类 / Extracted `BaseLogDbContext` base class | 2025-12-07 |
| TD-DUP-003 | `WdtErpFlagshipApiClient.cs` ↔ `WdtWmsApiClient.cs` | 151 行 | ✅ 已抽取 `BaseErpApiClient` 基类 / Extracted `BaseErpApiClient` base class | 2025-12-11 |
| TD-DUP-004 | `JushuitanErpApiClient.cs` ↔ `WdtWmsApiClient.cs` | 126 行 | ✅ 已抽取 `BaseErpApiClient` 基类 / Extracted `BaseErpApiClient` base class | 2025-12-11 |
| TD-DUP-005 | `ResilientLogRepository.cs` (内部重复) | 120 行 | ✅ 已抽取 `SyncBatchWithTransactionAsync` 辅助方法 / Extracted `SyncBatchWithTransactionAsync` helper method | 2025-12-11 |
| TD-DUP-006 | `VolumeMatcher.cs` ↔ `WeightMatcher.cs` | 118 行 | ✅ 已抽取 `BaseExpressionEvaluator` 共享逻辑 / Extracted `BaseExpressionEvaluator` shared logic | 2025-12-06 |
| TD-DUP-007 | `MySqlMonitoringAlertRepository.cs` ↔ `SqliteMonitoringAlertRepository.cs` | 107 行 | ✅ 已抽取 `BaseMonitoringAlertRepository` 基类 / Extracted `BaseMonitoringAlertRepository` base class | 2025-12-07 |
| TD-DUP-012 | `MySqlLogRepository.cs` ↔ `SqliteLogRepository.cs` | 61 行 | ✅ 已抽取 `BaseLogRepositoryImpl` 基类 / Extracted `BaseLogRepositoryImpl` base class | 2025-12-07 |
| TD-DUP-013 | `ApiCommunicationLog.cs` ↔ `WcsApiResponse.cs` | 57 行 | ✅ 已抽取 `BaseApiCommunication` 基类 / Extracted `BaseApiCommunication` base class | 2025-12-11 |
| TD-DUP-014 | `MonitoringAlertDto.cs` ↔ `MonitoringAlert.cs` | 56 行 | ✅ 已抽取 `BaseMonitoringAlert` 基类 / Extracted `BaseMonitoringAlert` base class | 2025-12-11 |
| TD-DUP-019 | `Program.cs` (内部重复) | 38 行 | ✅ 已抽取 `HttpClientConfigurationHelper` 文件作用域类 / Extracted `HttpClientConfigurationHelper` file-scoped class | 2025-12-11 |

#### 🔴 高优先级 / High Priority (>100 lines)

**全部已解决！All resolved!**

#### 🟡 中优先级 / Medium Priority (50-100 lines)

| ID | 文件 Files | 重复行数 Lines | 描述 Description |
|----|-----------|---------------|------------------|
| TD-DUP-008 | `WcsApiClient.cs` (内部重复) | 95 行 | WCS API客户端内部重复代码 / Internal duplicate in WCS API client |
| TD-DUP-009 | `WcsApiClient.cs` ↔ `WdtWmsApiClient.cs` | 93 行 | API客户端间重复代码 / Duplicate code between API clients |
| TD-DUP-010 | `WdtWmsApiClient.cs` (内部重复) | 80 行 | API客户端内部重复代码 / Internal duplicate in API client |
| TD-DUP-011 | `ApiClientTestController.cs` (内部重复) | 78 行 | 测试控制器重复代码 / Duplicate code in test controller |
| TD-DUP-013 | `ApiCommunicationLog.cs` ↔ `WcsApiResponse.cs` | 57 行 | 实体类重复属性定义 / Duplicate property definitions in entities |
| TD-DUP-014 | `MonitoringAlertDto.cs` ↔ `MonitoringAlert.cs` | 56 行 | DTO与实体类重复 / Duplicate between DTO and entity |
| TD-DUP-015 | `LogController.cs` (内部重复) | 55 行 | 日志控制器重复代码 / Duplicate code in log controller |

### 🎯 接口定义重复 / Interface Definition Duplicates

#### ✅ 已解决 / Resolved

| 相似接口 Similar Interfaces | 描述 Description | 解决方案 Solution | 解决日期 Date |
|---------------------------|------------------|-------------------|--------------|
| `IWcsAdapterManager` ↔ `ISorterAdapterManager` ↔ `IDwsAdapterManager` | 三个适配器管理器接口有相同的连接管理方法 / Three adapter manager interfaces have identical connection management methods | ✅ 已抽取 `IAdapterManager<TConfig>` 泛型基接口 / Extracted `IAdapterManager<TConfig>` generic base interface | 2025-12-11 |
| `IDwsConfigRepository` ↔ `IWcsApiConfigRepository` | 两个配置仓储接口有相同的CRUD操作 / Two config repository interfaces have identical CRUD operations | ✅ 已抽取 `IConfigRepository<TConfig>` 泛型基接口 / Extracted `IConfigRepository<TConfig>` generic base interface | 2025-12-11 |

---

### 🟢 低优先级 / Low Priority (<50 lines)

| ID | 文件 Files | 重复行数 Lines | 描述 Description |
|----|-----------|---------------|------------------|
| TD-DUP-016 | `DataAnalysisService.cs` (内部重复) | 47 行 | 数据分析服务内部重复 / Internal duplicate in data analysis service |
| TD-DUP-017 | `ResiliencePolicyFactory.cs` (内部重复) | 31 行 | 弹性策略工厂重复代码 / Duplicate in resilience policy factory |
| TD-DUP-018 | `RuleCreatedEvent.cs` ↔ `RuleUpdatedEvent.cs` | 28 行 | 事件类重复属性 / Duplicate properties in event classes |
| TD-DUP-019 | `Program.cs` (内部重复) | 38 行 | 启动配置重复代码 / Duplicate startup configuration |
| TD-DUP-020 | `SignalRClientService.cs` ↔ `TcpClientService.cs` | 13 行 | 通信服务重复代码 / Duplicate communication service code |
| TD-DUP-021 | `Chute.cs` ↔ `SortingRule.cs` | 16 行 | 实体类重复方法 / Duplicate methods in entity classes |
| TD-DUP-022 | `ChuteCreatedEvent.cs` ↔ `ChuteUpdatedEvent.cs` | 23 行 | 事件类重复属性 / Duplicate properties in event classes |

---

## 📋 重构建议 / Refactoring Suggestions

### 1. API 客户端重构 / API Client Refactoring

**问题描述 / Problem Description:**
多个 API 客户端 (`PostCollectionApiClient`, `PostProcessingCenterApiClient`, `WdtErpFlagshipApiClient`, `WdtWmsApiClient`, `JushuitanErpApiClient`, `WcsApiClient`) 包含大量重复代码。

**建议方案 / Suggested Solution:**
- 创建 `BaseApiClient` 抽象基类
- 提取通用 HTTP 请求方法
- 使用模板方法模式处理不同的序列化/反序列化逻辑

### 2. 数据库上下文重构 / Database Context Refactoring

**问题描述 / Problem Description:**
`MySqlLogDbContext` 和 `SqliteLogDbContext` 包含大量重复的实体配置代码。

**建议方案 / Suggested Solution:**
- 创建 `BaseLogDbContext` 共享基类
- 将通用的实体配置移至基类
- 只在子类中实现数据库特定的配置

### 3. 仓储层重构 / Repository Layer Refactoring

**问题描述 / Problem Description:**
`MySqlLogRepository`, `SqliteLogRepository`, `MySqlMonitoringAlertRepository`, `SqliteMonitoringAlertRepository` 等存在重复代码。

**建议方案 / Suggested Solution:**
- 创建泛型仓储基类
- 使用策略模式处理数据库差异
- 考虑使用 `ResilientLogRepository` 作为唯一入口点

### 4. 匹配器重构 / Matcher Refactoring

**问题描述 / Problem Description:**
`VolumeMatcher` 和 `WeightMatcher` 包含重复的范围匹配逻辑。

**建议方案 / Suggested Solution:**
- 创建 `RangeMatcher<T>` 泛型基类
- 提取通用的范围比较逻辑
- 只在子类中定义特定的值提取逻辑

### 5. DTO 与实体类重构 / DTO and Entity Refactoring

**问题描述 / Problem Description:**
`MonitoringAlertDto` 与 `MonitoringAlert` 几乎完全相同。

**建议方案 / Suggested Solution:**
- 评估是否真正需要分离 DTO 和实体
- 如需分离，使用 AutoMapper 或手动映射
- 避免复制粘贴属性定义

---

## 🛡️ 预防措施 / Prevention Measures

### CI/CD 集成 / CI/CD Integration

项目已配置以下检查来防止新的技术债务：

The project has configured the following checks to prevent new technical debt:

1. **代码重复检测 / Code Duplication Detection**
   - 使用 `jscpd` 在 CI 中检测重复代码
   - 阈值：最大 5% 重复率
   - 超过阈值将导致 CI 失败

2. **SonarQube 分析 / SonarQube Analysis**
   - 已配置在 `sonar-project.properties`
   - 目标：重复率 < 3%

3. **PR 模板检查 / PR Template Check**
   - PR 模板包含技术债务确认项
   - 必须确认已通读本文档

---

## 📝 债务解决记录 / Debt Resolution Log

记录技术债务的解决情况：

Record of technical debt resolution:

| 日期 Date | 债务 ID | 描述 Description | 解决者 Resolved By | PR 编号 PR Number |
|-----------|---------|------------------|-------------------|-------------------|
| 2025-12-06 | TD-DUP-001 | 抽取 BasePostalApiClient 基类消除 PostCollectionApiClient 与 PostProcessingCenterApiClient 重复 / Extract BasePostalApiClient to eliminate PostCollection/PostProcessingCenter duplication | GitHub Copilot | Previous PR |
| 2025-12-06 | TD-DUP-006 | 抽取 BaseExpressionEvaluator 消除 VolumeMatcher 与 WeightMatcher 重复 / Extract BaseExpressionEvaluator to eliminate VolumeMatcher/WeightMatcher duplication | GitHub Copilot | Previous PR |
| 2025-12-07 | TD-DUP-002 | 抽取 BaseLogDbContext 基类消除 MySqlLogDbContext 与 SqliteLogDbContext 重复（157行）/ Extract BaseLogDbContext to eliminate MySql/Sqlite DbContext duplication (157 lines) | GitHub Copilot | Current PR |
| 2025-12-07 | TD-DUP-007 | 抽取 BaseMonitoringAlertRepository 基类消除 MySql 与 Sqlite MonitoringAlertRepository 重复（107行）/ Extract BaseMonitoringAlertRepository to eliminate MySql/Sqlite repository duplication (107 lines) | GitHub Copilot | Current PR |
| 2025-12-07 | TD-DUP-012 | 抽取 BaseLogRepositoryImpl 基类消除 MySqlLogRepository 与 SqliteLogRepository 重复（61行）/ Extract BaseLogRepositoryImpl to eliminate MySql/Sqlite log repository duplication (61 lines) | GitHub Copilot | Previous PR |
| 2025-12-11 | TD-DUP-003 | 抽取 BaseErpApiClient 基类消除 WdtErpFlagshipApiClient 与 WdtWmsApiClient 重复（151行）/ Extract BaseErpApiClient to eliminate WdtErpFlagship/WdtWms duplication (151 lines) | GitHub Copilot | Current PR |
| 2025-12-11 | TD-DUP-004 | 抽取 BaseErpApiClient 基类消除 JushuitanErpApiClient 与 WdtWmsApiClient 重复（126行）/ Extract BaseErpApiClient to eliminate Jushuituan/WdtWms duplication (126 lines) | GitHub Copilot | Current PR |
| 2025-12-11 | TD-DUP-005 | 抽取 SyncBatchWithTransactionAsync 辅助方法消除 ResilientLogRepository 内部重复（120行）/ Extract SyncBatchWithTransactionAsync helper to eliminate ResilientLogRepository internal duplication (120 lines) | GitHub Copilot | Current PR |
| 2025-12-11 | TD-DUP-013 | 抽取 BaseApiCommunication 基类消除 ApiCommunicationLog 与 WcsApiResponse 重复（57行）/ Extract BaseApiCommunication base class to eliminate ApiCommunicationLog/WcsApiResponse duplication (57 lines) | GitHub Copilot | Current PR |
| 2025-12-11 | TD-DUP-014 | 抽取 BaseMonitoringAlert 基类消除 MonitoringAlert 与 MonitoringAlertDto 重复（56行）/ Extract BaseMonitoringAlert base class to eliminate MonitoringAlert/MonitoringAlertDto duplication (56 lines) | GitHub Copilot | Current PR |
| 2025-12-11 | TD-DUP-019 | 抽取 HttpClientConfigurationHelper 文件作用域类消除 Program.cs 内部重复（38行）/ Extract HttpClientConfigurationHelper file-scoped class to eliminate Program.cs internal duplication (38 lines) | GitHub Copilot | Current PR |
| 2025-12-11 | 接口重复 | 抽取 IAdapterManager<TConfig> 和 IConfigRepository<TConfig> 泛型接口消除功能相似但命名不同的接口定义 / Extract IAdapterManager<TConfig> and IConfigRepository<TConfig> generic interfaces to eliminate functionally similar but differently named interface definitions | GitHub Copilot | Current PR |

---

## 🔧 如何使用本文档 / How to Use This Document

### 作为开发者 / As a Developer

1. **开发新功能前 / Before developing new features:**
   - 通读本文档，了解现有技术债务
   - 检查你的改动是否会影响债务区域
   - 如果可能，尝试在改动中解决相关债务

2. **提交 PR 前 / Before submitting PR:**
   - 运行重复代码检测
   - 确认未引入新的重复代码
   - 如果解决了债务，更新本文档

3. **引入新债务时 / When introducing new debt:**
   - 必须在本文档中记录
   - 说明债务原因和计划解决时间
   - 获得团队确认

### 作为代码审查者 / As a Code Reviewer

1. 检查 PR 是否增加了代码重复
2. 确认提交者已阅读本文档
3. 如发现新债务，要求更新本文档

---

## 📅 定期审查 / Regular Review

本文档应每季度审查一次，评估：
This document should be reviewed quarterly to assess:

- 技术债务解决进度 / Technical debt resolution progress
- 新增债务情况 / Newly added debt
- 债务优先级调整 / Debt priority adjustments

**下次审查日期 / Next Review Date:** 2026-03-01

---

## 📞 联系方式 / Contact

如有关于技术债务的问题，请联系项目负责人。
For questions about technical debt, please contact the project lead.

---

*最后更新 / Last Updated: 2025-12-11*
*更新者 / Updated By: GitHub Copilot Agent*
