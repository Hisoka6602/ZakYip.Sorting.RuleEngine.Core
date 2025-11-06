#!/bin/bash

# Branch cleanup script
# This script deletes all branches except master
# 此脚本删除除master以外的所有分支

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}===================================${NC}"
echo -e "${YELLOW}  Branch Cleanup Script${NC}"
echo -e "${YELLOW}  分支清理脚本${NC}"
echo -e "${YELLOW}===================================${NC}"
echo ""

# Check if we're in a git repository
if [ ! -d .git ]; then
    echo -e "${RED}Error: Not a git repository${NC}"
    echo -e "${RED}错误：不是git仓库${NC}"
    exit 1
fi

# Get current branch
CURRENT_BRANCH=$(git rev-parse --abbrev-ref HEAD)
echo -e "Current branch / 当前分支: ${GREEN}$CURRENT_BRANCH${NC}"
echo ""

# Dry run mode check
DRY_RUN=false
if [ "$1" == "--dry-run" ] || [ "$1" == "-n" ]; then
    DRY_RUN=true
    echo -e "${YELLOW}Running in DRY-RUN mode (no actual deletions)${NC}"
    echo -e "${YELLOW}以模拟模式运行（不会实际删除）${NC}"
    echo ""
fi

# Fetch latest remote branches
echo -e "${YELLOW}Fetching latest remote branches...${NC}"
echo -e "${YELLOW}获取最新的远程分支...${NC}"
if git fetch --prune 2>/dev/null; then
    echo -e "${GREEN}✓ Fetch successful / 获取成功${NC}"
else
    echo -e "${YELLOW}⚠ Cannot fetch (using cached remote information) / 无法获取（使用缓存的远程信息）${NC}"
fi
echo ""

# Get list of remote branches except master
echo -e "${YELLOW}Remote branches to delete / 要删除的远程分支:${NC}"
BRANCHES_TO_DELETE=$(git branch -r | grep -v 'HEAD' | grep -v -E 'origin/master$' | tr -d ' ' | sed 's/^origin\///')

if [ -z "$BRANCHES_TO_DELETE" ]; then
    echo -e "${GREEN}No branches to delete / 没有需要删除的分支${NC}"
    exit 0
fi

echo "$BRANCHES_TO_DELETE"
echo ""

# Count branches
BRANCH_COUNT=$(echo "$BRANCHES_TO_DELETE" | wc -l)
echo -e "${YELLOW}Total branches to delete / 总共要删除的分支数: $BRANCH_COUNT${NC}"
echo ""

# Confirm deletion
if [ "$DRY_RUN" = false ]; then
    echo -e "${RED}WARNING: This will delete $BRANCH_COUNT remote branches!${NC}"
    echo -e "${RED}警告：这将删除 $BRANCH_COUNT 个远程分支！${NC}"
    echo -e "${YELLOW}Type 'yes' to confirm / 输入 'yes' 确认:${NC} "
    read -r CONFIRMATION
    
    if [ "$CONFIRMATION" != "yes" ]; then
        echo -e "${YELLOW}Operation cancelled / 操作已取消${NC}"
        exit 0
    fi
    echo ""
fi

# Delete remote branches
echo -e "${YELLOW}Deleting remote branches / 正在删除远程分支...${NC}"
echo "$BRANCHES_TO_DELETE" | while read -r BRANCH; do
    if [ -n "$BRANCH" ]; then
        if [ "$DRY_RUN" = true ]; then
            echo -e "${YELLOW}[DRY-RUN]${NC} Would delete: origin/$BRANCH"
        else
            echo -e "Deleting / 删除: ${RED}$BRANCH${NC}"
            if git push origin --delete "$BRANCH" 2>/dev/null; then
                echo -e "${GREEN}✓ Deleted successfully / 删除成功${NC}"
            else
                echo -e "${RED}✗ Failed to delete / 删除失败${NC}"
            fi
        fi
    fi
done

echo ""

# Clean up local references
if [ "$DRY_RUN" = false ]; then
    echo -e "${YELLOW}Cleaning up local references / 清理本地引用...${NC}"
    git fetch --prune
    echo -e "${GREEN}✓ Done / 完成${NC}"
fi

echo ""
echo -e "${GREEN}===================================${NC}"
echo -e "${GREEN}Branch cleanup completed / 分支清理完成${NC}"
echo -e "${GREEN}===================================${NC}"
