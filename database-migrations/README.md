# 数据库迁移说明 / Database Migration Instructions

## 问题描述 / Problem Description

用户报告了3个API端点的问题：

1. **GET /api/Log/dws-communication** - 返回错误：`Unknown column 'd.ImagesJson' in 'field list'`
2. **GET /api/Log/sorter-communication** - 没有返回数据（实际有数据）
3. **GET /api/Log/api-communication** - 返回了本系统API日志，应该只返回WCS API请求日志

## 解决方案 / Solutions

### 1. 数据库列缺失修复 / Fix Missing Database Columns

已创建数据库迁移脚本：
- `001_add_missing_communication_log_columns.sql` - MySQL版本
- `001_add_missing_communication_log_columns_sqlite.sql` - SQLite版本

### 2. API逻辑修复 / API Logic Fixes

✅ **已修复 / Fixed**: `GET /api/Log/api-communication` 
- 从查询 `ApiRequestLog`（入站请求）改为查询 `ApiCommunicationLog`（出站到WCS的请求）
- 筛选参数从 `requestPath` 改为 `parcelId`

## 应用迁移脚本 / Apply Migration Scripts

### MySQL

```bash
# 方式1：使用迁移工具（推荐）
cd database-migrations/tools
./apply-migration.sh ../001_add_missing_communication_log_columns.sql

# 方式2：直接执行SQL
mysql -u your_username -p your_database < 001_add_missing_communication_log_columns.sql
```

### SQLite

```bash
# 直接执行SQL
sqlite3 your_database.db < 001_add_missing_communication_log_columns_sqlite.sql
```

## 验证迁移成功 / Verify Migration Success

### MySQL

```sql
-- 查看表结构
DESCRIBE DwsCommunicationLog;
DESCRIBE SorterCommunicationLog;

-- 查看索引
SHOW INDEX FROM DwsCommunicationLog;
SHOW INDEX FROM SorterCommunicationLog;

-- 测试查询
SELECT Id, Barcode, ImagesJson, CommunicationTime 
FROM DwsCommunicationLog 
ORDER BY CommunicationTime DESC 
LIMIT 10;
```

### SQLite

```sql
-- 查看表结构
PRAGMA table_info(DwsCommunicationLog);
PRAGMA table_info(SorterCommunicationLog);

-- 查看索引
PRAGMA index_list(DwsCommunicationLog);

-- 测试查询
SELECT Id, Barcode, ImagesJson, CommunicationTime 
FROM DwsCommunicationLog 
ORDER BY CommunicationTime DESC 
LIMIT 10;
```

## API测试 / API Testing

迁移完成后，测试以下API端点：

### 1. DWS通信日志
```http
GET /api/Log/dws-communication?page=1&pageSize=10
```

**期望结果 / Expected Result**: 
- ✅ 返回DWS通信日志列表
- ✅ 无 `ImagesJson` 列错误

### 2. 分拣机通信日志
```http
GET /api/Log/sorter-communication?page=1&pageSize=10
```

**期望结果 / Expected Result**:
- ✅ 返回分拣机通信日志列表
- ✅ 包含实际数据

### 3. WCS API通信日志（仅外部请求）
```http
GET /api/Log/api-communication?page=1&pageSize=10
```

**期望结果 / Expected Result**:
- ✅ 只返回对WCS的API请求日志（出站）
- ✅ 不包含本系统的API请求日志（入站）

## 注意事项 / Important Notes

1. **备份数据库** - 在执行迁移前，请先备份数据库
2. **测试环境** - 建议先在测试环境执行迁移
3. **幂等性** - 迁移脚本设计为幂等的，可以安全地多次执行
4. **索引** - 迁移脚本会自动创建索引以提升查询性能

## 问题排查 / Troubleshooting

### 问题1: 迁移脚本执行失败

**原因**: 列已存在或权限不足

**解决方案**:
```sql
-- 检查列是否已存在
SELECT COLUMN_NAME 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'DwsCommunicationLog' 
AND COLUMN_NAME = 'ImagesJson';
```

### 问题2: API仍然返回错误

**原因**: 应用程序缓存或未重启

**解决方案**:
1. 重启应用程序
2. 清除Entity Framework缓存
3. 验证数据库连接字符串正确

### 问题3: 分拣机日志为空

**原因**: 日志记录逻辑可能未正确配置

**解决方案**:
1. 检查 `SorterCommunicationLogRepository` 是否正确注册到DI
2. 检查日志记录代码是否被正确调用
3. 查看应用程序日志是否有错误信息

---

**创建日期 / Created Date**: 2025-12-23
**版本 / Version**: 1.0
**状态 / Status**: ✅ 已完成 / Completed
