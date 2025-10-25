#!/bin/bash

# 数据库版本检查工具
# Database Version Check Tool

set -e

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# 显示使用说明
show_usage() {
    echo "使用方法: ./check-version.sh [OPTIONS]"
    echo ""
    echo "选项:"
    echo "  -d, --database TYPE    数据库类型 (mysql|sqlite)"
    echo "  -h, --host HOST        MySQL主机 (默认: localhost)"
    echo "  -P, --port PORT        MySQL端口 (默认: 3306)"
    echo "  -u, --user USER        MySQL用户名"
    echo "  -p, --password PASS    MySQL密码"
    echo "  -n, --dbname NAME      数据库名称"
    echo "  -f, --file FILE        SQLite数据库文件路径"
    echo "  --help                 显示此帮助信息"
}

# 默认值
DB_TYPE=""
MYSQL_HOST="localhost"
MYSQL_PORT="3306"
MYSQL_USER=""
MYSQL_PASSWORD=""
DB_NAME=""
SQLITE_FILE=""

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

# 检查MySQL版本
check_mysql_version() {
    if [ -z "$MYSQL_USER" ] || [ -z "$DB_NAME" ]; then
        echo -e "${RED}错误: MySQL需要用户名和数据库名${NC}"
        exit 1
    fi
    
    echo -e "${BLUE}=== MySQL数据库迁移历史 ===${NC}"
    echo ""
    
    QUERY="SELECT MigrationId, ProductVersion FROM __EFMigrationsHistory ORDER BY MigrationId;"
    
    if [ -n "$MYSQL_PASSWORD" ]; then
        mysql -h "$MYSQL_HOST" -P "$MYSQL_PORT" -u "$MYSQL_USER" -p"$MYSQL_PASSWORD" "$DB_NAME" -e "$QUERY" 2>/dev/null
    else
        mysql -h "$MYSQL_HOST" -P "$MYSQL_PORT" -u "$MYSQL_USER" "$DB_NAME" -e "$QUERY" 2>/dev/null
    fi
    
    if [ $? -eq 0 ]; then
        echo ""
        echo -e "${GREEN}✓ 成功查询迁移历史${NC}"
        
        # 获取最新迁移
        if [ -n "$MYSQL_PASSWORD" ]; then
            LATEST=$(mysql -h "$MYSQL_HOST" -P "$MYSQL_PORT" -u "$MYSQL_USER" -p"$MYSQL_PASSWORD" "$DB_NAME" -N -e "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC LIMIT 1;" 2>/dev/null)
        else
            LATEST=$(mysql -h "$MYSQL_HOST" -P "$MYSQL_PORT" -u "$MYSQL_USER" "$DB_NAME" -N -e "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC LIMIT 1;" 2>/dev/null)
        fi
        
        echo -e "${YELLOW}当前版本: $LATEST${NC}"
    else
        echo -e "${RED}✗ 无法查询迁移历史，可能是数据库未初始化${NC}"
    fi
}

# 检查SQLite版本
check_sqlite_version() {
    if [ -z "$SQLITE_FILE" ]; then
        echo -e "${RED}错误: SQLite需要数据库文件路径${NC}"
        exit 1
    fi
    
    if [ ! -f "$SQLITE_FILE" ]; then
        echo -e "${RED}错误: SQLite数据库文件不存在: $SQLITE_FILE${NC}"
        exit 1
    fi
    
    echo -e "${BLUE}=== SQLite数据库迁移历史 ===${NC}"
    echo ""
    
    sqlite3 "$SQLITE_FILE" "SELECT MigrationId, ProductVersion FROM __EFMigrationsHistory ORDER BY MigrationId;" -header -column 2>/dev/null
    
    if [ $? -eq 0 ]; then
        echo ""
        echo -e "${GREEN}✓ 成功查询迁移历史${NC}"
        
        # 获取最新迁移
        LATEST=$(sqlite3 "$SQLITE_FILE" "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC LIMIT 1;" 2>/dev/null)
        echo -e "${YELLOW}当前版本: $LATEST${NC}"
    else
        echo -e "${RED}✗ 无法查询迁移历史，可能是数据库未初始化${NC}"
    fi
}

# 执行检查
case $DB_TYPE in
    mysql)
        check_mysql_version
        ;;
    sqlite)
        check_sqlite_version
        ;;
    *)
        echo -e "${RED}错误: 不支持的数据库类型: $DB_TYPE${NC}"
        echo -e "支持的类型: mysql, sqlite"
        exit 1
        ;;
esac
