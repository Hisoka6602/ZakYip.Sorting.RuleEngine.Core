#!/bin/bash
#
# Git Pre-commit Hook - 代码质量检查 / Code Quality Check
# 
# 安装方法 / Installation:
#   chmod +x pre-commit-hook.sh
#   cp pre-commit-hook.sh .git/hooks/pre-commit
#
# 或使用符号链接 / Or use symbolic link:
#   ln -sf ../../pre-commit-hook.sh .git/hooks/pre-commit
#

set -e

echo "🔍 运行 Git Pre-commit 代码质量检查 / Running Git Pre-commit Code Quality Checks..."
echo "=========================================="
echo ""

# 颜色定义 / Color definitions
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# 检查 jscpd 是否安装 / Check if jscpd is installed
if ! command -v jscpd &> /dev/null; then
    echo -e "${YELLOW}⚠️  jscpd 未安装，正在安装... / jscpd not installed, installing...${NC}"
    npm install -g jscpd
    if [ $? -ne 0 ]; then
        echo -e "${RED}❌ jscpd 安装失败，请手动安装: npm install -g jscpd${NC}"
        exit 1
    fi
fi

# 运行代码重复检测 / Run code duplication detection
echo "📊 步骤 1/2: 代码重复检测 (jscpd) / Step 1/2: Code Duplication Detection (jscpd)"
echo "----------------------------------------"

JSCPD_REPORT=$(jscpd . --pattern "**/*.cs" \
    --ignore "**/bin/**,**/obj/**,**/Migrations/**,**/Tests/**,**/*.Designer.cs" \
    --reporters console \
    --min-lines 10 \
    --min-tokens 50 2>&1 || true)

# 提取重复率 / Extract duplication rate
DUPLICATION_RATE=$(echo "$JSCPD_REPORT" | grep -oP 'Duplicated lines.*?\(\K[0-9.]+' || echo "0")
THRESHOLD=5

echo ""
echo "重复代码比例 / Duplication Rate: ${DUPLICATION_RATE}%"
echo "阈值 / Threshold: ${THRESHOLD}%"
echo ""

# 使用 bc 进行浮点数比较 / Use bc for floating point comparison
if command -v bc &> /dev/null; then
    if (( $(echo "$DUPLICATION_RATE > $THRESHOLD" | bc -l) )); then
        echo -e "${RED}❌ 代码重复率 ${DUPLICATION_RATE}% 超过阈值 ${THRESHOLD}%${NC}"
        echo -e "${RED}Code duplication rate ${DUPLICATION_RATE}% exceeds threshold ${THRESHOLD}%${NC}"
        echo ""
        echo "📖 请参考 TECHNICAL_DEBT.md 了解如何解决重复代码问题"
        echo "📖 Please refer to TECHNICAL_DEBT.md for guidance on resolving duplicate code"
        echo ""
        echo "💡 提示: 你可以使用 --no-verify 跳过此检查 (不推荐)"
        echo "💡 Tip: You can use --no-verify to skip this check (not recommended)"
        exit 1
    fi
else
    # 如果没有 bc，使用 awk 进行比较 / If bc not available, use awk
    if awk "BEGIN {exit !($DUPLICATION_RATE > $THRESHOLD)}"; then
        echo -e "${RED}❌ 代码重复率 ${DUPLICATION_RATE}% 超过阈值 ${THRESHOLD}%${NC}"
        echo -e "${RED}Code duplication rate ${DUPLICATION_RATE}% exceeds threshold ${THRESHOLD}%${NC}"
        echo ""
        echo "📖 请参考 TECHNICAL_DEBT.md 了解如何解决重复代码问题"
        echo "📖 Please refer to TECHNICAL_DEBT.md for guidance on resolving duplicate code"
        echo ""
        echo "💡 提示: 你可以使用 --no-verify 跳过此检查 (不推荐)"
        echo "💡 Tip: You can use --no-verify to skip this check (not recommended)"
        exit 1
    fi
fi

echo -e "${GREEN}✅ 代码重复率检查通过 / Code duplication check passed${NC}"
echo ""

# 运行影分身语义检测 / Run shadow clone semantic detection
echo "🎭 步骤 2/2: 影分身语义检测 / Step 2/2: Shadow Clone Semantic Detection"
echo "----------------------------------------"

# 检查 .NET 是否可用 / Check if .NET is available
if ! command -v dotnet &> /dev/null; then
    echo -e "${YELLOW}⚠️  .NET SDK 未安装，跳过影分身检测 / .NET SDK not installed, skipping shadow clone detection${NC}"
else
    # 运行影分身检测脚本 / Run shadow clone detection script
    if [ -f "./shadow-clone-check.sh" ]; then
        if ./shadow-clone-check.sh . 2>&1; then
            echo -e "${GREEN}✅ 影分身语义检测通过 / Shadow clone semantic detection passed${NC}"
        else
            SHADOW_EXIT_CODE=$?
            if [ $SHADOW_EXIT_CODE -eq 1 ]; then
                echo ""
                echo -e "${YELLOW}⚠️  发现影分身代码（语义重复）/ Shadow clone code detected (semantic duplicates)${NC}"
                echo ""
                echo "建议在提交前解决，或在 TECHNICAL_DEBT.md 中记录原因"
                echo "Recommend fixing before commit, or documenting the reason in TECHNICAL_DEBT.md"
                echo ""
                echo "💡 提示: 这是警告，不会阻止提交 / Tip: This is a warning and won't block commit"
                echo "💡 如需跳过所有检查，使用: git commit --no-verify"
                # 暂时只警告，不强制失败 / Only warn for now, don't enforce failure
            fi
        fi
    else
        echo -e "${YELLOW}⚠️  未找到 shadow-clone-check.sh，跳过影分身检测${NC}"
        echo -e "${YELLOW}⚠️  shadow-clone-check.sh not found, skipping shadow clone detection${NC}"
    fi
fi

echo ""
echo "=========================================="
echo -e "${GREEN}✅ Pre-commit 检查完成 / Pre-commit checks completed${NC}"
echo ""
echo "📋 请确保你已经:"
echo "   1. 通读 TECHNICAL_DEBT.md"
echo "   2. 完成 7 种影分身检查"
echo "   3. 更新相关文档"
echo ""
echo "📋 Please ensure you have:"
echo "   1. Read through TECHNICAL_DEBT.md"
echo "   2. Completed 7 types of shadow clone checks"
echo "   3. Updated relevant documentation"
echo ""

exit 0
