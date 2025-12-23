# 数据库表缺失问题解决方案 / Database Table Missing Issue Solution

## 问题症状 / Problem Symptoms

运行时出现以下错误:
Runtime error occurs:

```
MySqlConnector.MySqlException (0x80004005): Table 'zakyip_sorting_ruleengine_db.parcel_infos' doesn't exist
```

**错误出现位置 / Error Locations:**
1. `DwsDataReceivedEventHandler` - 处理 DWS 数据时
2. `ParcelCacheService.GetOrLoadAsync` - 加载包裹缓存时
3. `DownstreamSorterEventSubscriptionService.OnParcelDetected` - 包裹检测事件处理时
4. `DownstreamSorterEventSubscriptionService.OnSortingCompleted` - 分拣完成事件处理时

---

## 根本原因 / Root Cause

虽然 EF Core 迁移文件已经存在（`20251220025800_AddParcelInfoAndLifecycle.cs`），但迁移尚未应用到实际的 MySQL 数据库中。

While the EF Core migration file exists (`20251220025800_AddParcelInfoAndLifecycle.cs`), the migration has not been applied to the actual MySQL database.

**可能的原因 / Possible Reasons:**
1. 首次部署，数据库是全新的
2. 应用程序尚未启动过（迁移在启动时自动应用）
3. MySQL 服务器连接失败，导致迁移未能执行
4. 数据库配置错误（连接字符串不正确）

---

## 快速解决方案 / Quick Solution

### 方案 A: 重启应用程序（最简单）/ Restart Application (Easiest)

**如果您刚部署应用，只需启动一次应用程序:**

If you just deployed the app, simply start the application once:

```bash
cd Service/ZakYip.Sorting.RuleEngine.Service
dotnet run
```

**预期输出 / Expected Output:**
```
[INFO] 尝试应用MySQL数据库迁移...
[INFO] MySQL数据库迁移成功
```

✅ **迁移会自动创建所有表，包括 `parcel_infos`**

### 方案 B: 使用迁移脚本 / Use Migration Script

```bash
./apply-migrations.sh
```

脚本会:
The script will:
- 自动安装 EF Core 工具（如需要）
- 显示所有可用的迁移
- 应用所有待处理的迁移到 MySQL 和 SQLite

---

## 详细解决步骤 / Detailed Solution Steps

### 步骤 1: 检查 MySQL 连接 / Step 1: Check MySQL Connection

```bash
mysql -h 127.0.0.1 -P 3306 -u root -p zakyip_sorting_ruleengine_db
```

**如果连接失败:**
- 确保 MySQL 服务器正在运行
- 检查 `appsettings.json` 中的连接字符串
- 验证数据库用户权限

### 步骤 2: 验证配置文件 / Step 2: Verify Configuration

**文件位置:** `Service/ZakYip.Sorting.RuleEngine.Service/appsettings.json`

```json
{
  "AppSettings": {
    "MySql": {
      "ConnectionString": "Server=127.0.0.1;Port=3306;Database=zakyip_sorting_ruleengine_db;User=root;Password=YOUR_PASSWORD;",
      "Enabled": true,  // ✅ 必须是 true
      "ServerVersion": "8.0.33"
    }
  }
}
```

### 步骤 3: 手动应用迁移 / Step 3: Manually Apply Migrations

#### 方法 3.1: 使用 EF Core CLI

```bash
# 1. 安装 EF Core 工具
dotnet tool install --global dotnet-ef

# 2. 应用迁移
cd /path/to/ZakYip.Sorting.RuleEngine.Core

dotnet ef database update \
  --project Infrastructure/ZakYip.Sorting.RuleEngine.Infrastructure/ZakYip.Sorting.RuleEngine.Infrastructure.csproj \
  --startup-project Service/ZakYip.Sorting.RuleEngine.Service/ZakYip.Sorting.RuleEngine.Service.csproj \
  --context MySqlLogDbContext
```

#### 方法 3.2: 直接执行 SQL（最后手段）

如果上述方法都失败，可以手动执行 SQL 脚本:

**文件:** `Infrastructure/ZakYip.Sorting.RuleEngine.Infrastructure/Persistence/MySql/Migrations/20251220025800_AddParcelInfoAndLifecycle.cs`

提取 SQL 命令并在 MySQL 中执行:

```sql
USE zakyip_sorting_ruleengine_db;

-- 创建 parcel_infos 表
CREATE TABLE IF NOT EXISTS `parcel_infos` (
    `ParcelId` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `CartNumber` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `Barcode` varchar(100) CHARACTER SET utf8mb4 NULL,
    `Length` decimal(18,3) NULL,
    `Width` decimal(18,3) NULL,
    `Height` decimal(18,3) NULL,
    `Volume` decimal(18,3) NULL,
    `Weight` decimal(18,3) NULL,
    `TargetChute` varchar(50) CHARACTER SET utf8mb4 NULL,
    `ActualChute` varchar(50) CHARACTER SET utf8mb4 NULL,
    `DecisionReason` varchar(200) CHARACTER SET utf8mb4 NULL,
    `MatchedRuleId` varchar(100) CHARACTER SET utf8mb4 NULL,
    `PositionBias` int NOT NULL,
    `ChuteNumber` varchar(50) CHARACTER SET utf8mb4 NULL,
    `BagId` varchar(100) CHARACTER SET utf8mb4 NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NULL,
    `CompletedAt` datetime(6) NULL,
    `Status` int NOT NULL,
    `LifecycleStage` int NOT NULL,
    CONSTRAINT `PK_parcel_infos` PRIMARY KEY (`ParcelId`)
) CHARACTER SET=utf8mb4;

-- 创建索引
CREATE INDEX `IX_parcel_infos_ParcelId` ON `parcel_infos` (`ParcelId`);
CREATE INDEX `IX_parcel_infos_Status_CreatedAt` ON `parcel_infos` (`Status`, `CreatedAt` DESC);
CREATE INDEX `IX_parcel_infos_TargetChute_CreatedAt` ON `parcel_infos` (`TargetChute`, `CreatedAt` DESC);
CREATE INDEX `IX_parcel_infos_CompletedAt_Desc` ON `parcel_infos` (`CompletedAt` DESC);
CREATE INDEX `IX_parcel_infos_BagId` ON `parcel_infos` (`BagId`);
CREATE INDEX `IX_parcel_infos_LifecycleStage_CreatedAt` ON `parcel_infos` (`LifecycleStage`, `CreatedAt` DESC);
CREATE INDEX `IX_parcel_infos_CartNumber` ON `parcel_infos` (`CartNumber`);

-- 创建 parcel_lifecycle_nodes 表
CREATE TABLE IF NOT EXISTS `parcel_lifecycle_nodes` (
    `NodeId` bigint NOT NULL AUTO_INCREMENT,
    `ParcelId` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Stage` int NOT NULL,
    `EventTime` datetime(6) NOT NULL,
    `Description` varchar(500) CHARACTER SET utf8mb4 NULL,
    `AdditionalDataJson` longtext CHARACTER SET utf8mb4 NULL,
    `CreatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_parcel_lifecycle_nodes` PRIMARY KEY (`NodeId`)
) CHARACTER SET=utf8mb4;

-- 创建索引
CREATE INDEX `IX_parcel_lifecycle_nodes_ParcelId` ON `parcel_lifecycle_nodes` (`ParcelId`);
CREATE INDEX `IX_parcel_lifecycle_nodes_Stage_EventTime` ON `parcel_lifecycle_nodes` (`Stage`, `EventTime` DESC);
CREATE INDEX `IX_parcel_lifecycle_nodes_EventTime_Desc` ON `parcel_lifecycle_nodes` (`EventTime` DESC);

-- 记录迁移历史
INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251220025800_AddParcelInfoAndLifecycle', '8.0.0');
```

### 步骤 4: 验证表已创建 / Step 4: Verify Tables Created

```sql
USE zakyip_sorting_ruleengine_db;

-- 显示所有表
SHOW TABLES;

-- 应该看到:
-- parcel_infos ✅
-- parcel_lifecycle_nodes ✅

-- 查看表结构
DESC parcel_infos;
DESC parcel_lifecycle_nodes;

-- 检查迁移历史
SELECT * FROM __EFMigrationsHistory ORDER BY MigrationId;
-- 应该看到 20251220025800_AddParcelInfoAndLifecycle ✅
```

### 步骤 5: 重启应用程序 / Step 5: Restart Application

```bash
cd Service/ZakYip.Sorting.RuleEngine.Service
dotnet run
```

**验证日志中没有错误:**
```
[INFO] MySQL数据库迁移成功
[INFO] 📦 [步骤1-包裹检测] ParcelId=... ✅
```

---

## 降级方案：使用 SQLite / Fallback: Use SQLite

如果 MySQL 持续有问题，可以临时使用 SQLite:

If MySQL continues to have issues, you can temporarily use SQLite:

### 修改 appsettings.json

```json
{
  "AppSettings": {
    "MySql": {
      "Enabled": false  // ❌ 禁用 MySQL
    },
    "Sqlite": {
      "ConnectionString": "Data Source=./data/logs.db"
    }
  }
}
```

### 应用 SQLite 迁移

```bash
dotnet ef database update \
  --project Infrastructure/ZakYip.Sorting.RuleEngine.Infrastructure/ZakYip.Sorting.RuleEngine.Infrastructure.csproj \
  --startup-project Service/ZakYip.Sorting.RuleEngine.Service/ZakYip.Sorting.RuleEngine.Service.csproj \
  --context SqliteLogDbContext
```

### 验证 SQLite 数据库

```bash
sqlite3 Service/ZakYip.Sorting.RuleEngine.Service/data/logs.db

.tables
-- 应该看到 parcel_infos ✅

.schema parcel_infos
-- 查看表结构

SELECT * FROM __EFMigrationsHistory ORDER BY MigrationId;
-- 应该看到 20251220025800_AddParcelInfoAndLifecycle ✅
```

---

## 预防措施 / Prevention Measures

### 1. 自动化部署脚本 / Automated Deployment Script

创建部署脚本确保迁移总是被应用:

```bash
#!/bin/bash
# deploy.sh

echo "🚀 部署应用程序 / Deploying Application"

# 1. 停止应用
echo "⏹️  停止应用 / Stopping application..."
# systemctl stop zakyip-sorting-engine

# 2. 更新代码
echo "📥 更新代码 / Updating code..."
git pull

# 3. 应用迁移
echo "🔄 应用数据库迁移 / Applying database migrations..."
./apply-migrations.sh

# 4. 编译应用
echo "🔨 编译应用 / Building application..."
dotnet build -c Release

# 5. 启动应用
echo "▶️  启动应用 / Starting application..."
# systemctl start zakyip-sorting-engine

echo "✅ 部署完成 / Deployment completed"
```

### 2. 健康检查端点 / Health Check Endpoint

在应用中添加健康检查，验证数据库表存在:

```csharp
app.MapGet("/health/database", async (MySqlLogDbContext context) =>
{
    try
    {
        // 检查 parcel_infos 表是否存在
        await context.ParcelInfos.AnyAsync();
        return Results.Ok(new { status = "healthy", database = "mysql", tables = "verified" });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, title: "Database table missing");
    }
});
```

### 3. 监控和告警 / Monitoring and Alerts

添加监控确保能及时发现数据库问题:

```csharp
// Program.cs - 启动时验证
var canAccessParcelInfos = await VerifyDatabaseTablesAsync(services);
if (!canAccessParcelInfos)
{
    logger.Error("❌ 关键表 parcel_infos 不存在，应用无法正常运行");
    throw new InvalidOperationException("Database schema verification failed");
}
```

---

## 相关文档 / Related Documentation

- **完整迁移指南:** [DATABASE_MIGRATION_GUIDE.md](./DATABASE_MIGRATION_GUIDE.md)
- **技术债务文档:** [TECHNICAL_DEBT.md](./TECHNICAL_DEBT.md)
- **迁移脚本:** [apply-migrations.sh](./apply-migrations.sh)

---

## 常见问题 / FAQ

### Q: 为什么自动迁移没有工作？/ Why didn't auto-migration work?

**A:** 可能的原因:
1. MySQL 连接失败（检查 `appsettings.json`）
2. 数据库用户权限不足（需要 CREATE TABLE 权限）
3. 应用程序从未完全启动过
4. 迁移执行时出现异常被捕获

### Q: 可以删除旧的迁移历史吗？/ Can I delete old migration history?

**A:** ⚠️ **不建议** / **Not recommended**

迁移历史 (`__EFMigrationsHistory` 表) 用于跟踪哪些迁移已应用。删除可能导致:
- 重复应用迁移
- 数据不一致
- 表结构冲突

### Q: 生产环境应该自动迁移吗？/ Should production use auto-migration?

**A:** 视情况而定 / It depends

**优点:**
- ✅ 简化部署流程
- ✅ 减少人工错误

**缺点:**
- ❌ 无法回滚
- ❌ 大型迁移可能导致停机

**建议:** 生产环境使用手动迁移 + 备份 + 测试环境验证

---

**最后更新 / Last Updated**: 2025-12-23  
**问题编号 / Issue Number**: N/A  
**严重程度 / Severity**: 🔴 高 High (阻止应用运行)  
**解决状态 / Resolution Status**: ✅ 已解决 (提供多种解决方案)
