# 数据库迁移指南 / Database Migration Guide

## 问题描述 / Problem Description

如果您遇到以下错误:

```
MySqlConnector.MySqlException (0x80004005): Table 'zakyip_sorting_ruleengine_db.parcel_infos' doesn't exist
```

这表示数据库迁移尚未应用到您的 MySQL 数据库。

If you encounter the following error:
```
MySqlConnector.MySqlException (0x80004005): Table 'zakyip_sorting_ruleengine_db.parcel_infos' doesn't exist
```

This indicates that database migrations have not been applied to your MySQL database.

---

## 自动迁移 / Automatic Migration

应用程序在启动时会**自动应用迁移**。确保:

The application **automatically applies migrations** on startup. Ensure that:

1. ✅ MySQL 服务器正在运行 / MySQL server is running
2. ✅ `appsettings.json` 中的连接字符串正确 / Connection string in `appsettings.json` is correct
3. ✅ 数据库用户有创建表的权限 / Database user has permission to create tables

**配置文件位置 / Configuration File Location:**
```
Service/ZakYip.Sorting.RuleEngine.Service/appsettings.json
```

**连接字符串配置 / Connection String Configuration:**
```json
{
  "AppSettings": {
    "MySql": {
      "ConnectionString": "Server=127.0.0.1;Port=3306;Database=zakyip_sorting_ruleengine_db;User=root;Password=YOUR_PASSWORD;AllowLoadLocalInfile=true;Pooling=true;",
      "Enabled": true
    }
  }
}
```

### 启动应用程序触发自动迁移 / Start Application to Trigger Auto Migration

```bash
cd Service/ZakYip.Sorting.RuleEngine.Service
dotnet run
```

应用程序会在启动时打印迁移日志:
The application will print migration logs on startup:

```
[INFO] 正在应用MySQL数据库迁移...
[INFO] MySQL数据库迁移成功
```

---

## 手动迁移 / Manual Migration

如果自动迁移失败,可以使用提供的脚本手动应用迁移:

If automatic migration fails, use the provided script to manually apply migrations:

### 方法 1: 使用脚本 / Method 1: Use Script

```bash
# Linux / macOS
./apply-migrations.sh

# Windows (PowerShell)
# 需要先安装 dotnet-ef 工具 / Need to install dotnet-ef tool first
dotnet tool install --global dotnet-ef
```

### 方法 2: 使用 EF Core CLI / Method 2: Use EF Core CLI

#### 步骤 1: 安装 EF Core 工具 / Step 1: Install EF Core Tools

```bash
dotnet tool install --global dotnet-ef
# 或更新 / Or update
dotnet tool update --global dotnet-ef
```

#### 步骤 2: 应用 MySQL 迁移 / Step 2: Apply MySQL Migrations

```bash
cd /path/to/ZakYip.Sorting.RuleEngine.Core

dotnet ef database update \
  --project Infrastructure/ZakYip.Sorting.RuleEngine.Infrastructure/ZakYip.Sorting.RuleEngine.Infrastructure.csproj \
  --startup-project Service/ZakYip.Sorting.RuleEngine.Service/ZakYip.Sorting.RuleEngine.Service.csproj \
  --context MySqlLogDbContext
```

#### 步骤 3: 应用 SQLite 迁移 (备用) / Step 3: Apply SQLite Migrations (Fallback)

```bash
dotnet ef database update \
  --project Infrastructure/ZakYip.Sorting.RuleEngine.Infrastructure/ZakYip.Sorting.RuleEngine.Infrastructure.csproj \
  --startup-project Service/ZakYip.Sorting.RuleEngine.Service/ZakYip.Sorting.RuleEngine.Service.csproj \
  --context SqliteLogDbContext
```

---

## 验证迁移 / Verify Migrations

### 查看已应用的迁移 / List Applied Migrations

```bash
# MySQL
dotnet ef migrations list \
  --project Infrastructure/ZakYip.Sorting.RuleEngine.Infrastructure/ZakYip.Sorting.RuleEngine.Infrastructure.csproj \
  --startup-project Service/ZakYip.Sorting.RuleEngine.Service/ZakYip.Sorting.RuleEngine.Service.csproj \
  --context MySqlLogDbContext

# SQLite
dotnet ef migrations list \
  --project Infrastructure/ZakYip.Sorting.RuleEngine.Infrastructure/ZakYip.Sorting.RuleEngine.Infrastructure.csproj \
  --startup-project Service/ZakYip.Sorting.RuleEngine.Service/ZakYip.Sorting.RuleEngine.Service.csproj \
  --context SqliteLogDbContext
```

### 检查数据库表 / Check Database Tables

**MySQL:**
```sql
USE zakyip_sorting_ruleengine_db;

-- 显示所有表 / Show all tables
SHOW TABLES;

-- 检查 parcel_infos 表结构 / Check parcel_infos table structure
DESC parcel_infos;

-- 检查迁移历史 / Check migration history
SELECT * FROM __EFMigrationsHistory ORDER BY MigrationId;
```

**SQLite:**
```bash
sqlite3 Service/ZakYip.Sorting.RuleEngine.Service/data/logs.db

-- 显示所有表 / Show all tables
.tables

-- 检查 parcel_infos 表结构 / Check parcel_infos table structure
.schema parcel_infos

-- 检查迁移历史 / Check migration history
SELECT * FROM __EFMigrationsHistory ORDER BY MigrationId;
```

---

## 迁移文件清单 / Migration Files Checklist

确保以下迁移文件存在:
Ensure the following migration files exist:

### MySQL 迁移 / MySQL Migrations
- ✅ `20251025042050_InitialCreate.cs`
- ✅ `20251108021853_AddMonitoringAlertsTable.cs`
- ✅ `20251108061415_FixMonitoringAlertIdType.cs`
- ✅ `20251109090727_AddCommunicationTypeToLogTables.cs`
- ✅ `20251112190500_AddPerformanceIndexes.cs`
- ✅ `20251220025800_AddParcelInfoAndLifecycle.cs` ⭐ **关键迁移 / Key Migration**
- ✅ `20251223040000_AddImagesJsonToDwsLogs.cs`

### SQLite 迁移 / SQLite Migrations
- ✅ `20251025042107_InitialCreate.cs`
- ✅ `20251108021911_AddMonitoringAlertsTable.cs`
- ✅ `20251108061446_FixMonitoringAlertIdType.cs`
- ✅ `20251109090756_AddCommunicationTypeToLogTables.cs`
- ✅ `20251112190500_AddPerformanceIndexes.cs`
- ✅ `20251220025800_AddParcelInfoAndLifecycle.cs` ⭐ **关键迁移 / Key Migration**

---

## 常见问题 / FAQ

### Q1: 迁移失败,提示"连接超时" / Migration fails with "connection timeout"

**解决方案 / Solution:**
1. 检查 MySQL 服务器是否正在运行
2. 检查防火墙设置
3. 验证连接字符串中的主机、端口、用户名和密码

```bash
# 测试 MySQL 连接 / Test MySQL connection
mysql -h 127.0.0.1 -P 3306 -u root -p
```

### Q2: 提示"权限不足" / "Permission denied" error

**解决方案 / Solution:**

确保数据库用户有以下权限:
Ensure database user has the following permissions:

```sql
GRANT ALL PRIVILEGES ON zakyip_sorting_ruleengine_db.* TO 'root'@'%';
FLUSH PRIVILEGES;
```

### Q3: SQLite 文件路径问题 / SQLite file path issue

**解决方案 / Solution:**

确保 `data` 目录存在:
Ensure `data` directory exists:

```bash
mkdir -p Service/ZakYip.Sorting.RuleEngine.Service/data
```

### Q4: 自动迁移被禁用 / Auto migration is disabled

**检查 Program.cs / Check Program.cs:**

确认以下代码存在:
Confirm the following code exists:

```csharp
// 自动应用数据库迁移 / Auto-apply database migrations
mysqlContext.Database.Migrate();
```

---

## 降级方案 / Fallback Strategy

如果 MySQL 不可用,系统会**自动降级到 SQLite**:

If MySQL is unavailable, the system will **automatically fallback to SQLite**:

1. ✅ 应用程序检测到 MySQL 连接失败
2. ✅ 自动切换到 SQLite
3. ✅ 日志记录在本地 SQLite 文件 (./data/logs.db)

**配置 SQLite 作为主数据库 / Configure SQLite as Primary Database:**

在 `appsettings.json` 中设置:
Set in `appsettings.json`:

```json
{
  "AppSettings": {
    "MySql": {
      "Enabled": false  // ❌ 禁用 MySQL / Disable MySQL
    },
    "Sqlite": {
      "ConnectionString": "Data Source=./data/logs.db"
    }
  }
}
```

---

## 生产环境建议 / Production Recommendations

### 1. 备份数据库 / Backup Database

在应用迁移前,备份现有数据:
Backup existing data before applying migrations:

```bash
# MySQL
mysqldump -u root -p zakyip_sorting_ruleengine_db > backup_$(date +%Y%m%d_%H%M%S).sql

# SQLite
cp Service/ZakYip.Sorting.RuleEngine.Service/data/logs.db logs_backup_$(date +%Y%m%d_%H%M%S).db
```

### 2. 测试迁移 / Test Migrations

在测试环境先验证迁移:
Verify migrations in test environment first:

```bash
# 使用测试数据库 / Use test database
dotnet ef database update --context MySqlLogDbContext -- --environment Testing
```

### 3. 监控迁移日志 / Monitor Migration Logs

检查应用程序启动日志:
Check application startup logs:

```bash
tail -f logs/app_$(date +%Y%m%d).log | grep -i migration
```

---

## 相关文件 / Related Files

- **迁移文件 / Migration Files**: `Infrastructure/ZakYip.Sorting.RuleEngine.Infrastructure/Persistence/*/Migrations/`
- **数据库上下文 / Database Context**: `Infrastructure/ZakYip.Sorting.RuleEngine.Infrastructure/Persistence/BaseLogDbContext.cs`
- **启动配置 / Startup Configuration**: `Service/ZakYip.Sorting.RuleEngine.Service/Program.cs` (行 1137)
- **连接字符串 / Connection String**: `Service/ZakYip.Sorting.RuleEngine.Service/appsettings.json`

---

## 联系支持 / Contact Support

如果问题持续存在,请提供以下信息:

If the issue persists, please provide the following information:

1. 完整的错误日志 / Complete error logs
2. MySQL 版本 / MySQL version: `SELECT VERSION();`
3. .NET SDK 版本 / .NET SDK version: `dotnet --version`
4. 操作系统 / Operating system
5. `appsettings.json` 配置 (隐藏密码 / hide password)

---

**最后更新 / Last Updated**: 2025-12-23  
**文档版本 / Document Version**: 1.0
