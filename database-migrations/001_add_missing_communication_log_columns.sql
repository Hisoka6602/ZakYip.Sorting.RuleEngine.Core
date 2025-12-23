-- =====================================================
-- 数据库迁移脚本：添加缺失的通信日志列
-- Database Migration Script: Add Missing Communication Log Columns
-- 版本 / Version: 001
-- 日期 / Date: 2025-12-23
-- 描述 / Description: 
--   修复通信日志表缺失的列，确保所有表结构与实体定义一致
--   Fix missing columns in communication log tables to ensure table structure matches entity definitions
-- =====================================================

-- =====================================================
-- MySQL 数据库迁移 / MySQL Database Migration
-- =====================================================

-- 1. 检查并添加 DwsCommunicationLog 表的 ImagesJson 列
-- Check and add ImagesJson column to DwsCommunicationLog table
ALTER TABLE `DwsCommunicationLog` 
ADD COLUMN IF NOT EXISTS `ImagesJson` TEXT NULL COMMENT '图片信息（JSON格式）';

-- 2. 确保 SorterCommunicationLog 表结构完整
-- Ensure SorterCommunicationLog table structure is complete
ALTER TABLE `SorterCommunicationLog`
ADD COLUMN IF NOT EXISTS `CommunicationType` INT NOT NULL DEFAULT 1 COMMENT '通信类型（TCP/SignalR/HTTP/MQTT）' AFTER `Id`,
ADD COLUMN IF NOT EXISTS `SorterAddress` VARCHAR(255) NOT NULL DEFAULT '' COMMENT '分拣机地址' AFTER `CommunicationType`,
ADD COLUMN IF NOT EXISTS `OriginalContent` TEXT NOT NULL COMMENT '原始内容' AFTER `SorterAddress`,
ADD COLUMN IF NOT EXISTS `FormattedContent` TEXT NULL COMMENT '格式化内容' AFTER `OriginalContent`,
ADD COLUMN IF NOT EXISTS `ExtractedParcelId` VARCHAR(100) NULL COMMENT '提取的包裹ID' AFTER `FormattedContent`,
ADD COLUMN IF NOT EXISTS `ExtractedCartNumber` VARCHAR(50) NULL COMMENT '提取的小车号' AFTER `ExtractedParcelId`,
ADD COLUMN IF NOT EXISTS `CommunicationTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) COMMENT '通信时间' AFTER `ExtractedCartNumber`,
ADD COLUMN IF NOT EXISTS `IsSuccess` TINYINT(1) NOT NULL DEFAULT 1 COMMENT '是否成功' AFTER `CommunicationTime`,
ADD COLUMN IF NOT EXISTS `ErrorMessage` TEXT NULL COMMENT '错误信息' AFTER `IsSuccess`;

-- 3. 添加索引以提升查询性能
-- Add indexes to improve query performance
CREATE INDEX IF NOT EXISTS `idx_dws_barcode` ON `DwsCommunicationLog`(`Barcode`);
CREATE INDEX IF NOT EXISTS `idx_dws_communication_time` ON `DwsCommunicationLog`(`CommunicationTime`);
CREATE INDEX IF NOT EXISTS `idx_sorter_parcel_id` ON `SorterCommunicationLog`(`ExtractedParcelId`);
CREATE INDEX IF NOT EXISTS `idx_sorter_communication_time` ON `SorterCommunicationLog`(`CommunicationTime`);

-- =====================================================
-- 验证说明 / Verification Instructions
-- =====================================================
-- 运行此脚本后，执行以下查询验证：
-- After running this script, verify with the following queries:
--
-- DESCRIBE `DwsCommunicationLog`;
-- DESCRIBE `SorterCommunicationLog`;
-- SHOW INDEX FROM `DwsCommunicationLog`;
-- SHOW INDEX FROM `SorterCommunicationLog`;
-- =====================================================
