# Branch cleanup script for Windows PowerShell
# This script deletes all branches except master
# 此脚本删除除master以外的所有分支

param(
    [switch]$DryRun,
    [switch]$Force
)

$ErrorActionPreference = "Stop"

Write-Host "===================================" -ForegroundColor Yellow
Write-Host "  Branch Cleanup Script" -ForegroundColor Yellow
Write-Host "  分支清理脚本" -ForegroundColor Yellow
Write-Host "===================================" -ForegroundColor Yellow
Write-Host ""

# Check if we're in a git repository
if (-not (Test-Path .git)) {
    Write-Host "Error: Not a git repository" -ForegroundColor Red
    Write-Host "错误：不是git仓库" -ForegroundColor Red
    exit 1
}

# Get current branch
$currentBranch = git rev-parse --abbrev-ref HEAD
Write-Host "Current branch / 当前分支: " -NoNewline
Write-Host $currentBranch -ForegroundColor Green
Write-Host ""

# Dry run mode check
if ($DryRun) {
    Write-Host "Running in DRY-RUN mode (no actual deletions)" -ForegroundColor Yellow
    Write-Host "以模拟模式运行（不会实际删除）" -ForegroundColor Yellow
    Write-Host ""
}

# Fetch latest remote branches
Write-Host "Fetching latest remote branches..." -ForegroundColor Yellow
Write-Host "获取最新的远程分支..." -ForegroundColor Yellow
try {
    git fetch --prune 2>$null
    Write-Host "✓ Fetch successful / 获取成功" -ForegroundColor Green
}
catch {
    Write-Host "⚠ Cannot fetch (using cached remote information) / 无法获取（使用缓存的远程信息）" -ForegroundColor Yellow
}
Write-Host ""

# Get list of remote branches except master
Write-Host "Remote branches to delete / 要删除的远程分支:" -ForegroundColor Yellow
$remoteBranches = git branch -r | Where-Object { 
    $_ -notmatch 'HEAD' -and $_ -notmatch 'origin/master$' 
}

$branchesToDelete = $remoteBranches | ForEach-Object {
    $_.Trim() -replace '^origin/', ''
}

if (-not $branchesToDelete) {
    Write-Host "No branches to delete / 没有需要删除的分支" -ForegroundColor Green
    exit 0
}

$branchesToDelete | ForEach-Object { Write-Host $_ }
Write-Host ""

# Count branches
$branchCount = $branchesToDelete.Count
Write-Host "Total branches to delete / 总共要删除的分支数: $branchCount" -ForegroundColor Yellow
Write-Host ""

# Confirm deletion
if (-not $DryRun -and -not $Force) {
    Write-Host "WARNING: This will delete $branchCount remote branches!" -ForegroundColor Red
    Write-Host "警告：这将删除 $branchCount 个远程分支！" -ForegroundColor Red
    Write-Host "Type 'yes' to confirm / 输入 'yes' 确认: " -ForegroundColor Yellow -NoNewline
    $confirmation = Read-Host
    
    if ($confirmation -ne "yes") {
        Write-Host "Operation cancelled / 操作已取消" -ForegroundColor Yellow
        exit 0
    }
    Write-Host ""
}

# Delete remote branches
Write-Host "Deleting remote branches / 正在删除远程分支..." -ForegroundColor Yellow
foreach ($branch in $branchesToDelete) {
    if ($DryRun) {
        Write-Host "[DRY-RUN] Would delete: origin/$branch" -ForegroundColor Yellow
    }
    else {
        Write-Host "Deleting / 删除: " -NoNewline
        Write-Host $branch -ForegroundColor Red
        try {
            git push origin --delete $branch 2>$null
            Write-Host "✓ Deleted successfully / 删除成功" -ForegroundColor Green
        }
        catch {
            Write-Host "✗ Failed to delete / 删除失败" -ForegroundColor Red
        }
    }
}

Write-Host ""

# Clean up local references
if (-not $DryRun) {
    Write-Host "Cleaning up local references / 清理本地引用..." -ForegroundColor Yellow
    git fetch --prune
    Write-Host "✓ Done / 完成" -ForegroundColor Green
}

Write-Host ""
Write-Host "===================================" -ForegroundColor Green
Write-Host "Branch cleanup completed / 分支清理完成" -ForegroundColor Green
Write-Host "===================================" -ForegroundColor Green
