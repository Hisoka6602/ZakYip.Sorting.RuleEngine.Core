# 数据库迁移工具 (Database Migration Tools)

本目录包含数据库迁移脚本和工具，用于管理数据库架构的版本控制和升级。

## 目录结构

```
database-migrations/
├── README.md                 # 本文件
├── scripts/                  # SQL迁移脚本目录
│   ├── mysql/               # MySQL迁移脚本
│   │   └── V*.sql          # 版本化的迁移脚本
│   └── sqlite/              # SQLite迁移脚本
│       └── V*.sql          # 版本化的迁移脚本
└── tools/                    # 迁移工具脚本
    ├── apply-migration.sh   # 应用迁移脚本
    ├── rollback-migration.sh # 回滚迁移脚本
    └── check-version.sh     # 检查数据库版本
```

## 迁移脚本命名规范

迁移脚本按照以下格式命名：

```
V<版本号>__<描述>.sql
```

示例:
- `V1.0.0__Initial_schema.sql`
- `V1.1.0__Add_chute_statistics_table.sql`
- `V1.2.0__Add_performance_indexes.sql`

## 使用Entity Framework Core迁移

### 为MySQL创建迁移

```bash
# 添加新迁移
cd ZakYip.Sorting.RuleEngine.Infrastructure
dotnet ef migrations add <MigrationName> --context MySqlLogDbContext --output-dir Persistence/MySql/Migrations

# 应用迁移
dotnet ef database update --context MySqlLogDbContext

# 生成SQL脚本（用于生产环境）
dotnet ef migrations script --context MySqlLogDbContext --output ../database-migrations/scripts/mysql/V<version>__<description>.sql
```

### 为SQLite创建迁移

```bash
# 添加新迁移
cd ZakYip.Sorting.RuleEngine.Infrastructure
dotnet ef migrations add <MigrationName> --context SqliteLogDbContext --output-dir Persistence/Sqlite/Migrations

# 应用迁移
dotnet ef database update --context SqliteLogDbContext

# 生成SQL脚本
dotnet ef migrations script --context SqliteLogDbContext --output ../database-migrations/scripts/sqlite/V<version>__<description>.sql
```

## 生产环境迁移流程

### 步骤 1: 生成迁移脚本

在开发环境中使用EF Core生成SQL迁移脚本：

```bash
# MySQL
dotnet ef migrations script --context MySqlLogDbContext --idempotent --output migration.sql

# SQLite  
dotnet ef migrations script --context SqliteLogDbContext --idempotent --output migration.sql
```

`--idempotent` 选项确保脚本可以安全地多次运行。

### 步骤 2: 审查脚本

在应用之前，务必审查生成的SQL脚本：
- 检查是否有数据丢失风险
- 验证索引和约束
- 确认数据类型更改
- 评估性能影响

### 步骤 3: 备份数据库

在应用迁移之前，务必备份生产数据库：

```bash
# MySQL备份
mysqldump -u root -p database_name > backup_$(date +%Y%m%d_%H%M%S).sql

# SQLite备份
cp production.db backup_$(date +%Y%m%d_%H%M%S).db
```

### 步骤 4: 应用迁移

```bash
# MySQL
mysql -u root -p database_name < migration.sql

# SQLite
sqlite3 production.db < migration.sql
```

### 步骤 5: 验证迁移

- 检查数据库架构是否正确更新
- 运行应用程序冒烟测试
- 验证关键功能正常工作

## 回滚策略

如果迁移失败或出现问题：

1. **立即停止应用程序**
2. **从备份恢复数据库**
3. **调查问题**
4. **修复迁移脚本**
5. **在测试环境重新验证**

## 迁移最佳实践

1. **小步迭代**: 每次迁移只做少量更改
2. **可逆性**: 尽可能设计可回滚的迁移
3. **测试**: 在与生产环境相似的环境中测试迁移
4. **文档化**: 记录每次迁移的目的和影响
5. **版本控制**: 所有迁移脚本都应纳入版本控制
6. **数据迁移**: 架构迁移和数据迁移分开进行
7. **幂等性**: 确保迁移脚本可以安全地多次运行

## 数据库版本跟踪

EF Core自动在数据库中创建`__EFMigrationsHistory`表来跟踪已应用的迁移。

查看已应用的迁移：

```sql
-- MySQL/SQLite
SELECT * FROM __EFMigrationsHistory ORDER BY MigrationId;
```

## 故障排查

### 问题: 迁移无法应用

**解决方案:**
1. 检查数据库连接字符串
2. 验证数据库用户权限
3. 查看错误日志
4. 检查是否有架构锁定

### 问题: 迁移历史不一致

**解决方案:**
1. 检查`__EFMigrationsHistory`表
2. 手动调整迁移历史记录
3. 如需要，删除并重新创建数据库（仅开发环境）

## 自动化迁移（生产环境不推荐）

虽然应用程序在`Program.cs`中配置了自动迁移：

```csharp
mysqlContext.Database.Migrate();
sqliteContext.Database.Migrate();
```

但在生产环境中，推荐使用手动迁移流程以获得更好的控制。

## 零停机迁移

对于需要零停机的迁移：

1. **使用蓝绿部署**
2. **分阶段迁移** - 先添加新列，再填充数据，最后删除旧列
3. **向后兼容** - 确保新旧代码都能工作
4. **在线架构变更** - 使用支持在线DDL的数据库功能

## 参考资源

- [EF Core Migrations Documentation](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [MySQL Migration Guide](https://dev.mysql.com/doc/refman/8.0/en/migration.html)
- [SQLite Migration Patterns](https://www.sqlite.org/lang_altertable.html)
