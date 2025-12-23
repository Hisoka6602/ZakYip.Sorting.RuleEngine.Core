#!/bin/bash
# 数据库迁移应用脚本 / Database Migration Application Script
# 用途: 手动应用 EF Core 迁移到 MySQL 和 SQLite 数据库
# Usage: Manually apply EF Core migrations to MySQL and SQLite databases

set -e

echo "======================================"
echo "数据库迁移应用工具 / Database Migration Tool"
echo "======================================"
echo ""

# 切换到项目根目录
cd "$(dirname "$0")"

# 检查 .NET SDK
if ! command -v dotnet &> /dev/null; then
    echo "❌ 错误: 未找到 .NET SDK"
    echo "❌ Error: .NET SDK not found"
    exit 1
fi

echo "✅ .NET SDK 版本 / .NET SDK Version:"
dotnet --version
echo ""

# 检查 EF Core 工具
echo "📦 检查 EF Core 工具 / Checking EF Core Tools..."
if ! dotnet ef --version &> /dev/null; then
    echo "⚠️  未安装 EF Core 工具,正在安装... / EF Core tools not installed, installing..."
    dotnet tool install --global dotnet-ef
    echo "✅ EF Core 工具安装完成 / EF Core tools installed"
else
    echo "✅ EF Core 工具已安装 / EF Core tools installed"
    dotnet ef --version
fi
echo ""

# 设置项目路径
INFRASTRUCTURE_PROJECT="Infrastructure/ZakYip.Sorting.RuleEngine.Infrastructure/ZakYip.Sorting.RuleEngine.Infrastructure.csproj"
SERVICE_PROJECT="Service/ZakYip.Sorting.RuleEngine.Service/ZakYip.Sorting.RuleEngine.Service.csproj"

# 检查项目文件是否存在
if [ ! -f "$INFRASTRUCTURE_PROJECT" ]; then
    echo "❌ 错误: 未找到 Infrastructure 项目文件"
    echo "❌ Error: Infrastructure project file not found: $INFRASTRUCTURE_PROJECT"
    exit 1
fi

if [ ! -f "$SERVICE_PROJECT" ]; then
    echo "❌ 错误: 未找到 Service 项目文件"
    echo "❌ Error: Service project file not found: $SERVICE_PROJECT"
    exit 1
fi

# 显示可用的迁移
echo "📋 查询可用的迁移 / Listing available migrations..."
echo ""
echo "--- MySQL 迁移 / MySQL Migrations ---"
dotnet ef migrations list --project "$INFRASTRUCTURE_PROJECT" --startup-project "$SERVICE_PROJECT" --context MySqlLogDbContext || true
echo ""
echo "--- SQLite 迁移 / SQLite Migrations ---"
dotnet ef migrations list --project "$INFRASTRUCTURE_PROJECT" --startup-project "$SERVICE_PROJECT" --context SqliteLogDbContext || true
echo ""

# 询问用户是否继续
read -p "是否应用迁移到数据库? / Apply migrations to database? (y/n): " -n 1 -r
echo ""
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "❌ 已取消 / Cancelled"
    exit 0
fi

# 应用 MySQL 迁移
echo ""
echo "🔄 应用 MySQL 迁移 / Applying MySQL migrations..."
if dotnet ef database update --project "$INFRASTRUCTURE_PROJECT" --startup-project "$SERVICE_PROJECT" --context MySqlLogDbContext; then
    echo "✅ MySQL 迁移应用成功 / MySQL migrations applied successfully"
else
    echo "⚠️  MySQL 迁移失败,可能是连接问题 / MySQL migration failed, possibly connection issue"
    echo "   系统将在运行时自动降级到 SQLite"
    echo "   System will automatically fallback to SQLite at runtime"
fi

# 应用 SQLite 迁移
echo ""
echo "🔄 应用 SQLite 迁移 / Applying SQLite migrations..."
if dotnet ef database update --project "$INFRASTRUCTURE_PROJECT" --startup-project "$SERVICE_PROJECT" --context SqliteLogDbContext; then
    echo "✅ SQLite 迁移应用成功 / SQLite migrations applied successfully"
else
    echo "❌ SQLite 迁移失败 / SQLite migration failed"
    exit 1
fi

echo ""
echo "======================================"
echo "✅ 迁移应用完成 / Migration application completed"
echo "======================================"
echo ""
echo "提示 / Tips:"
echo "1. MySQL 数据库确保服务器正在运行且连接字符串正确"
echo "   Ensure MySQL server is running and connection string is correct"
echo ""
echo "2. SQLite 数据库文件将自动创建在:"
echo "   SQLite database file will be auto-created at:"
echo "   ./Service/ZakYip.Sorting.RuleEngine.Service/data/logs.db"
echo ""
echo "3. 如果遇到问题,检查 appsettings.json 中的连接字符串配置"
echo "   If issues occur, check connection string in appsettings.json"
echo ""
