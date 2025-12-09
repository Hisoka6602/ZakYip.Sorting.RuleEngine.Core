#!/bin/bash

# 自含部署发布脚本 / Self-Contained Deployment Publish Script
# 用于在 Linux/macOS 上发布应用程序 / For publishing application on Linux/macOS

set -e

# 颜色输出 / Color output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# 脚本目录 / Script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_DIR="${SCRIPT_DIR}/Service/ZakYip.Sorting.RuleEngine.Service"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}ZakYip 分拣规则引擎 - 自含部署发布${NC}"
echo -e "${GREEN}ZakYip Sorting Rule Engine - Self-Contained Publish${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

# 检查 .NET SDK / Check .NET SDK
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}错误: 未找到 dotnet 命令${NC}"
    echo -e "${RED}Error: dotnet command not found${NC}"
    echo -e "${YELLOW}请安装 .NET 8.0 SDK: https://dotnet.microsoft.com/download${NC}"
    echo -e "${YELLOW}Please install .NET 8.0 SDK: https://dotnet.microsoft.com/download${NC}"
    exit 1
fi

echo -e "${GREEN}✓ 检测到 .NET SDK 版本:${NC} $(dotnet --version)"
echo ""

# 选择发布配置 / Select publish profile
echo -e "${YELLOW}请选择发布配置 / Please select publish profile:${NC}"
echo "1) Linux x64 (多文件) / Linux x64 (Multiple files)"
echo "2) Linux x64 (单文件) / Linux x64 (Single file)"
echo "3) Windows x64 (多文件) / Windows x64 (Multiple files)"
echo "4) Windows x64 (单文件) / Windows x64 (Single file)"
echo ""
read -p "请输入选项 (1-4) / Enter option (1-4): " choice

case $choice in
    1)
        PROFILE="SelfContained-Linux-x64"
        RUNTIME="linux-x64"
        ;;
    2)
        PROFILE="SelfContained-SingleFile-Linux-x64"
        RUNTIME="linux-x64"
        ;;
    3)
        PROFILE="SelfContained-Win-x64"
        RUNTIME="win-x64"
        ;;
    4)
        PROFILE="SelfContained-SingleFile-Win-x64"
        RUNTIME="win-x64"
        ;;
    *)
        echo -e "${RED}无效的选项 / Invalid option${NC}"
        exit 1
        ;;
esac

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}开始发布 / Starting publish${NC}"
echo -e "${GREEN}配置文件 / Profile: ${PROFILE}${NC}"
echo -e "${GREEN}运行时 / Runtime: ${RUNTIME}${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

# 切换到项目目录 / Change to project directory
cd "${PROJECT_DIR}"

# 清理之前的发布 / Clean previous publish
echo -e "${YELLOW}清理之前的发布输出... / Cleaning previous publish output...${NC}"
rm -rf "bin/Release/net8.0/${RUNTIME}/publish"

# 执行发布 / Execute publish
echo -e "${YELLOW}开始发布... / Starting publish...${NC}"
dotnet publish -c Release -p:PublishProfile="${PROFILE}" -p:RuntimeIdentifier="${RUNTIME}"

if [ $? -eq 0 ]; then
    echo ""
    echo -e "${GREEN}========================================${NC}"
    echo -e "${GREEN}✓ 发布成功 / Publish successful${NC}"
    echo -e "${GREEN}========================================${NC}"
    echo ""
    echo -e "${GREEN}发布输出目录 / Publish output directory:${NC}"
    echo -e "${YELLOW}${PROJECT_DIR}/bin/Release/net8.0/${RUNTIME}/publish/${NC}"
    echo ""
    
    # 显示发布文件大小 / Show publish file size
    PUBLISH_DIR="${PROJECT_DIR}/bin/Release/net8.0/${RUNTIME}/publish"
    if [ -d "${PUBLISH_DIR}" ]; then
        echo -e "${GREEN}发布包大小 / Publish package size:${NC}"
        du -sh "${PUBLISH_DIR}"
        echo ""
        
        echo -e "${GREEN}主要文件列表 / Main files list:${NC}"
        ls -lh "${PUBLISH_DIR}" | grep -E "ZakYip|\.exe$|\.dll$|appsettings|nlog" | head -10
        echo ""
    fi
    
    echo -e "${YELLOW}提示 / Tips:${NC}"
    echo "1. 发布包已包含 .NET 运行时，无需在目标机器上安装 .NET"
    echo "   The publish package includes .NET runtime, no need to install .NET on target machine"
    echo ""
    echo "2. 复制整个 publish 目录到目标机器"
    echo "   Copy the entire publish directory to target machine"
    echo ""
    echo "3. Linux 上需要设置可执行权限: chmod +x ZakYip.Sorting.RuleEngine.Service"
    echo "   On Linux, set executable permission: chmod +x ZakYip.Sorting.RuleEngine.Service"
    echo ""
    echo "4. 运行应用程序: ./ZakYip.Sorting.RuleEngine.Service"
    echo "   Run application: ./ZakYip.Sorting.RuleEngine.Service"
    echo ""
else
    echo ""
    echo -e "${RED}========================================${NC}"
    echo -e "${RED}✗ 发布失败 / Publish failed${NC}"
    echo -e "${RED}========================================${NC}"
    exit 1
fi
