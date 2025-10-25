#!/bin/bash

# 数据库迁移应用工具
# Database Migration Application Tool

set -e

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# 显示使用说明
show_usage() {
    echo "使用方法: ./apply-migration.sh [OPTIONS]"
    echo ""
    echo "选项:"
    echo "  -d, --database TYPE    数据库类型 (mysql|sqlite)"
    echo "  -h, --host HOST        MySQL主机 (默认: localhost)"
    echo "  -P, --port PORT        MySQL端口 (默认: 3306)"
    echo "  -u, --user USER        MySQL用户名"
    echo "  -p, --password PASS    MySQL密码"
    echo "  -n, --dbname NAME      数据库名称"
    echo "  -f, --file FILE        SQLite数据库文件路径"
    echo "  -s, --script SCRIPT    要应用的迁移脚本"
    echo "  -v, --version VERSION  目标版本号"
    echo "  --help                 显示此帮助信息"
    echo ""
    echo "示例:"
    echo "  # MySQL迁移"
    echo "  ./apply-migration.sh -d mysql -h localhost -u root -p password -n mydb -s V1.1.0__Add_indexes.sql"
    echo ""
    echo "  # SQLite迁移"
    echo "  ./apply-migration.sh -d sqlite -f /path/to/db.sqlite -s V1.1.0__Add_indexes.sql"
}

# 默认值
DB_TYPE=""
MYSQL_HOST="localhost"
MYSQL_PORT="3306"
MYSQL_USER=""
MYSQL_PASSWORD=""
DB_NAME=""
SQLITE_FILE=""
SCRIPT=""
VERSION=""

# 解析命令行参数
while [[ $# -gt 0 ]]; do
    case $1 in
        -d|--database)
            DB_TYPE="$2"
            shift 2
            ;;
        -h|--host)
            MYSQL_HOST="$2"
            shift 2
            ;;
        -P|--port)
            MYSQL_PORT="$2"
            shift 2
            ;;
        -u|--user)
            MYSQL_USER="$2"
            shift 2
            ;;
        -p|--password)
            MYSQL_PASSWORD="$2"
            shift 2
            ;;
        -n|--dbname)
            DB_NAME="$2"
            shift 2
            ;;
        -f|--file)
            SQLITE_FILE="$2"
            shift 2
            ;;
        -s|--script)
            SCRIPT="$2"
            shift 2
            ;;
        -v|--version)
            VERSION="$2"
            shift 2
            ;;
        --help)
            show_usage
            exit 0
            ;;
        *)
            echo -e "${RED}错误: 未知选项 $1${NC}"
            show_usage
            exit 1
            ;;
    esac
done

# 验证必需参数
if [ -z "$DB_TYPE" ]; then
    echo -e "${RED}错误: 必须指定数据库类型 (-d|--database)${NC}"
    show_usage
    exit 1
fi

if [ -z "$SCRIPT" ]; then
    echo -e "${RED}错误: 必须指定迁移脚本 (-s|--script)${NC}"
    show_usage
    exit 1
fi

# 检查脚本文件是否存在
if [ ! -f "$SCRIPT" ]; then
    echo -e "${RED}错误: 迁移脚本文件不存在: $SCRIPT${NC}"
    exit 1
fi

# 应用MySQL迁移
apply_mysql_migration() {
    echo -e "${YELLOW}正在应用MySQL迁移...${NC}"
    
    if [ -z "$MYSQL_USER" ] || [ -z "$DB_NAME" ]; then
        echo -e "${RED}错误: MySQL迁移需要用户名和数据库名${NC}"
        exit 1
    fi
    
    # 备份数据库
    BACKUP_FILE="backup_${DB_NAME}_$(date +%Y%m%d_%H%M%S).sql"
    echo -e "${YELLOW}备份数据库到: $BACKUP_FILE${NC}"
    
    if [ -n "$MYSQL_PASSWORD" ]; then
        mysqldump -h "$MYSQL_HOST" -P "$MYSQL_PORT" -u "$MYSQL_USER" -p"$MYSQL_PASSWORD" "$DB_NAME" > "$BACKUP_FILE"
    else
        mysqldump -h "$MYSQL_HOST" -P "$MYSQL_PORT" -u "$MYSQL_USER" "$DB_NAME" > "$BACKUP_FILE"
    fi
    
    # 应用迁移
    echo -e "${YELLOW}应用迁移脚本: $SCRIPT${NC}"
    
    if [ -n "$MYSQL_PASSWORD" ]; then
        mysql -h "$MYSQL_HOST" -P "$MYSQL_PORT" -u "$MYSQL_USER" -p"$MYSQL_PASSWORD" "$DB_NAME" < "$SCRIPT"
    else
        mysql -h "$MYSQL_HOST" -P "$MYSQL_PORT" -u "$MYSQL_USER" "$DB_NAME" < "$SCRIPT"
    fi
    
    echo -e "${GREEN}✓ MySQL迁移成功应用${NC}"
    echo -e "${GREEN}✓ 备份文件: $BACKUP_FILE${NC}"
}

# 应用SQLite迁移
apply_sqlite_migration() {
    echo -e "${YELLOW}正在应用SQLite迁移...${NC}"
    
    if [ -z "$SQLITE_FILE" ]; then
        echo -e "${RED}错误: SQLite迁移需要数据库文件路径${NC}"
        exit 1
    fi
    
    if [ ! -f "$SQLITE_FILE" ]; then
        echo -e "${RED}错误: SQLite数据库文件不存在: $SQLITE_FILE${NC}"
        exit 1
    fi
    
    # 备份数据库
    BACKUP_FILE="${SQLITE_FILE}.backup_$(date +%Y%m%d_%H%M%S)"
    echo -e "${YELLOW}备份数据库到: $BACKUP_FILE${NC}"
    cp "$SQLITE_FILE" "$BACKUP_FILE"
    
    # 应用迁移
    echo -e "${YELLOW}应用迁移脚本: $SCRIPT${NC}"
    sqlite3 "$SQLITE_FILE" < "$SCRIPT"
    
    echo -e "${GREEN}✓ SQLite迁移成功应用${NC}"
    echo -e "${GREEN}✓ 备份文件: $BACKUP_FILE${NC}"
}

# 执行迁移
case $DB_TYPE in
    mysql)
        apply_mysql_migration
        ;;
    sqlite)
        apply_sqlite_migration
        ;;
    *)
        echo -e "${RED}错误: 不支持的数据库类型: $DB_TYPE${NC}"
        echo -e "支持的类型: mysql, sqlite"
        exit 1
        ;;
esac

echo -e "${GREEN}✓ 迁移完成${NC}"
