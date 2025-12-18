# WcsApiResponse 重构技术债务 / WcsApiResponse Refactoring Technical Debt

## 背景 / Background

`WcsApiResponse` 类已经被重构为新的结构，以更好地组织API请求和响应信息。这个重构涉及：
1. 字段重命名和重组
2. `ParcelId` 从 `string` 改为 `long`（提供 `ParcelIdString` 向后兼容属性）
3. 添加新字段（`RequestStatus`, `Method` 等，都有默认值）
4. 移除旧字段（`Success`, `Code`, `Message`, `Data`, `ErrorMessage` 等）
5. `ElapsedMilliseconds` 保持为 `long` 类型（避免溢出）

The `WcsApiResponse` class has been refactored to a new structure for better organization of API request and response information. This refactoring involves:
1. Field renaming and reorganization
2. `ParcelId` changed from `string` to `long` (with `ParcelIdString` backward compatibility property)
3. Added new fields (`RequestStatus`, `Method`, etc., all with default values)
4. Removed old fields (`Success`, `Code`, `Message`, `Data`, `ErrorMessage`, etc.)
5. `ElapsedMilliseconds` kept as `long` type (to avoid overflow)

## 向后兼容性改进 / Backward Compatibility Improvements

为减少迁移难度，已实施以下改进：
- 所有新增字段都提供了默认值，不再是 `required`
- 提供 `ParcelIdString` 废弃属性用于字符串到 long 的自动转换
- `ElapsedMilliseconds` 保持 `long` 类型以避免潜在的溢出问题

To ease migration, the following improvements have been made:
- All new fields have default values and are no longer `required`
- Deprecated `ParcelIdString` property provided for automatic string-to-long conversion
- `ElapsedMilliseconds` kept as `long` to avoid potential overflow issues

## 新旧字段映射 / Old-New Field Mapping

| 旧字段 / Old Field | 新字段 / New Field | 说明 / Notes |
|-------------------|-------------------|--------------|
| `ParcelId` (string) | `ParcelId` (long) | 类型改变，提供 ParcelIdString 向后兼容 / Type changed, ParcelIdString provided for backward compatibility |
| `Success` (bool) | `RequestStatus` (enum) | true → Success, false → Failure |
| `Code` (string) | `FormattedMessage` | 合并到消息中 / Merged into message |
| `Message` (string) | `FormattedMessage` | 重命名 / Renamed |
| `Data` (string) | `ResponseBody` | 重命名 / Renamed |
| `ErrorMessage` (string) | `Exception` | 重命名 / Renamed |
| `RequestHeaders` (string) | `Headers` | 重命名 / Renamed |
| `DurationMs` (long) | `ElapsedMilliseconds` (long) | 重命名（保持 long 以避免溢出） / Renamed (keep long to avoid overflow) |
| `FormattedCurl` (string) | `CurlData` | 重命名 / Renamed |
| (无 / none) | `Method` (string) | 新增字段（有默认值） / New field (with default value) |
| (无 / none) | `QueryParams` (string) | 新增字段 / New field |
| (继承 BaseApiCommunication) | (直接定义) | 不再继承基类 / No longer inherits base class |

## 受影响的文件清单 / Affected Files List

### 1. API 客户端 / API Clients (46处使用 / 46 usages)

#### Infrastructure/ApiClients/WcsApiClient.cs
- [ ] `CreateSuccessResponse()` 方法 - 需要更新所有字段映射
- [ ] `CreateErrorResponse()` 方法 - 需要更新所有字段映射  
- [ ] `CreateExceptionResponse()` 方法 - 需要更新所有字段映射
- [ ] 添加 `Method` 参数到所有方法

#### Infrastructure/ApiClients/JushuitanErp/JushuitanErpApiClient.cs
- [ ] `RequestChuteAsync()` - 所有 `new WcsApiResponse` 实例化
- [ ] 更新字段：`ParcelId` (string → long), 添加 `Method`, 添加 `RequestStatus`

#### Infrastructure/ApiClients/WdtWms/WdtWmsApiClient.cs
- [ ] `RequestChuteAsync()` - 所有 `new WcsApiResponse` 实例化
- [ ] 更新字段映射

#### Infrastructure/ApiClients/WdtErpFlagship/WdtErpFlagshipApiClient.cs
- [ ] `RequestChuteAsync()` - 所有 `new WcsApiResponse` 实例化
- [ ] 更新字段映射

#### Infrastructure/ApiClients/PostCollection/PostCollectionApiClient.cs
- [ ] `ScanParcelAsync()` - 6处 `new WcsApiResponse`
- [ ] `RequestChuteAsync()` - 4处 `new WcsApiResponse`
- [ ] `UploadImageAsync()` - 1处 `new WcsApiResponse`
- [ ] `NotifyChuteLandingAsync()` - 3处 `new WcsApiResponse`

#### Infrastructure/ApiClients/PostProcessingCenter/PostProcessingCenterApiClient.cs
- [ ] `ScanParcelAsync()` - 6处 `new WcsApiResponse`
- [ ] `RequestChuteAsync()` - 4处 `new WcsApiResponse`
- [ ] `UploadImageAsync()` - 1处 `new WcsApiResponse`
- [ ] `NotifyChuteLandingAsync()` - 3处 `new WcsApiResponse`

### 2. Application 层 / Application Layer

#### Application/EventHandlers/WcsApiCalledEventHandler.cs
- [ ] 第83行：`ParcelId` 类型转换 (string → long)
- [ ] 第86行：`RequestHeaders` → `Headers`
- [ ] 第88行：`DurationMs` → `ElapsedMilliseconds`
- [ ] 第91行：`ResponseStatusCode` - 保持不变
- [ ] 第92行：`ResponseHeaders` - 保持不变
- [ ] 第93行：`FormattedCurl` → `CurlData`
- [ ] 第94行：`Success` → `RequestStatus` 判断
- [ ] 第95行：`ErrorMessage` → `Exception`

#### Application/EventHandlers/DwsDataReceivedEventHandler.cs
- [ ] 第59行：`Success` → `RequestStatus`, `Message` → `FormattedMessage`
- [ ] 第66行：`Success` → `RequestStatus`
- [ ] 第67行：`ResponseStatusCode` - 保持不变
- [ ] 第68行：`DurationMs` → `ElapsedMilliseconds`
- [ ] 第70行：`Message` → `FormattedMessage`

#### Application/Services/RuleEngineService.cs
- [ ] 第223行：`Data` → `ResponseBody`
- [ ] 第225行：`Data` → `ResponseBody`

### 3. Domain 实体 / Domain Entities

#### Domain/Entities/BaseApiCommunication.cs
- [ ] 评估是否仍需要此基类（`WcsApiResponse` 已不再继承）
- [ ] 如果 `ApiCommunicationLog` 仍在使用，保持不变

### 4. 测试文件 / Test Files

需要搜索并更新所有测试中对 `WcsApiResponse` 的使用。

## 修复策略 / Fix Strategy

### 阶段1：字段重命名（简单替换）/ Phase 1: Field Renaming (Simple Replacement)
预计工作量：2-3小时 / Estimated effort: 2-3 hours

1. `RequestHeaders` → `Headers`
2. `DurationMs` → `ElapsedMilliseconds` (注意类型：long → int)
3. `FormattedCurl` → `CurlData`
4. `Data` → `ResponseBody`
5. `Message` → `FormattedMessage`
6. `ErrorMessage` → `Exception`

### 阶段2：ParcelId 类型转换 / Phase 2: ParcelId Type Conversion
预计工作量：1-2小时 / Estimated effort: 1-2 hours

所有传递 `string` 类型 ParcelId 的地方需要：
- 如果是字面量（如 `"test123"`），需要改为数字或先解析
- 如果是变量，需要使用 `long.Parse()` 或 `long.TryParse()` 转换
- 确保所有 ParcelId 都是有效的数字

### 阶段3：Success → RequestStatus / Phase 3: Success → RequestStatus
预计工作量：1-2小时 / Estimated effort: 1-2 hours

1. `Success = true` → `RequestStatus = ApiRequestStatus.Success`
2. `Success = false` → `RequestStatus = ApiRequestStatus.Failure`
3. `if (response.Success)` → `if (response.RequestStatus == ApiRequestStatus.Success)`
4. `if (!response.Success)` → `if (response.RequestStatus != ApiRequestStatus.Success)`

### 阶段4：添加 Method 字段 / Phase 4: Add Method Field
预计工作量：2-3小时 / Estimated effort: 2-3 hours

所有创建 `WcsApiResponse` 的地方需要添加 `Method` 字段：
- `RequestChuteAsync` → `Method = "RequestChuteAsync"`
- `UploadImageAsync` → `Method = "UploadImageAsync"`
- `NotifyChuteLandingAsync` → `Method = "NotifyChuteLandingAsync"`
- `ScanParcelAsync` → `Method = "ScanParcelAsync"`

### 阶段5：验证和测试 / Phase 5: Validation and Testing
预计工作量：2-3小时 / Estimated effort: 2-3 hours

1. 编译整个解决方案
2. 运行所有单元测试
3. 运行集成测试
4. 手动验证 API 端点

## 总预计工作量 / Total Estimated Effort
**8-13 小时 / 8-13 hours**

## 优先级 / Priority
🔴 高 / High - 此重构阻塞了正常编译

## 建议的 PR 标题 / Suggested PR Title
`Refactor WcsApiResponse to new structure and update all usages`

## 检查清单 / Checklist
- [ ] 更新所有 API 客户端中的 `WcsApiResponse` 创建代码
- [ ] 更新所有 EventHandler 中的字段访问
- [ ] 更新 Application 层的字段访问
- [ ] 运行并修复所有编译错误
- [ ] 更新相关单元测试
- [ ] 运行集成测试
- [ ] 更新 API 文档（如果有）
- [ ] Code Review
- [ ] 合并到主分支

## 备注 / Notes

- 此重构破坏性较大，建议在单独的 PR 中完成
- 建议使用 IDE 的重构工具批量替换字段名
- `ParcelId` 类型转换需要特别小心，确保没有数据丢失
- 考虑添加迁移脚本或向后兼容层（如果生产环境已有数据）
