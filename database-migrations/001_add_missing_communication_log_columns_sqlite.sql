-- =====================================================
-- SQLite 数据库迁移脚本：添加缺失的通信日志列
-- SQLite Database Migration Script: Add Missing Communication Log Columns
-- 版本 / Version: 001
-- 日期 / Date: 2025-12-23
-- 描述 / Description: 
--   修复通信日志表缺失的列，确保所有表结构与实体定义一致
--   Fix missing columns in communication log tables to ensure table structure matches entity definitions
-- =====================================================

-- 注意 / Note:
-- SQLite 不支持 ADD COLUMN IF NOT EXISTS，需要先检查列是否存在
-- SQLite does not support ADD COLUMN IF NOT EXISTS, need to check if column exists first

-- =====================================================
-- SQLite 数据库迁移 / SQLite Database Migration
-- =====================================================

-- 1. 为 DwsCommunicationLog 表添加 ImagesJson 列（如果不存在）
-- Add ImagesJson column to DwsCommunicationLog table (if not exists)

-- 检查列是否存在的方法：
-- Method to check if column exists:
-- SELECT COUNT(*) FROM pragma_table_info('DwsCommunicationLog') WHERE name='ImagesJson';
-- 如果返回0，则执行以下语句：
-- If returns 0, execute the following statement:

BEGIN TRANSACTION;

-- 尝试添加 ImagesJson 列（如果失败则说明列已存在）
-- Try to add ImagesJson column (if fails, column already exists)
ALTER TABLE DwsCommunicationLog ADD COLUMN ImagesJson TEXT NULL;

COMMIT;

-- 2. 创建索引以提升查询性能（如果不存在）
-- Create indexes to improve query performance (if not exists)

CREATE INDEX IF NOT EXISTS idx_dws_barcode ON DwsCommunicationLog(Barcode);
CREATE INDEX IF NOT EXISTS idx_dws_communication_time ON DwsCommunicationLog(CommunicationTime);
CREATE INDEX IF NOT EXISTS idx_sorter_parcel_id ON SorterCommunicationLog(ExtractedParcelId);
CREATE INDEX IF NOT EXISTS idx_sorter_communication_time ON SorterCommunicationLog(CommunicationTime);

-- =====================================================
-- 验证说明 / Verification Instructions
-- =====================================================
-- 运行此脚本后，执行以下查询验证：
-- After running this script, verify with the following queries:
--
-- PRAGMA table_info(DwsCommunicationLog);
-- PRAGMA table_info(SorterCommunicationLog);
-- PRAGMA index_list(DwsCommunicationLog);
-- PRAGMA index_list(SorterCommunicationLog);
-- =====================================================
