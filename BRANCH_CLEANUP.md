# 分支清理指南 / Branch Cleanup Guide

本文档说明如何删除除master以外的所有分支。

This document explains how to delete all branches except master.

## 背景 / Background

仓库中存在多个已合并或不再需要的分支：

The repository contains multiple merged or no longer needed branches:

- `copilot/fix-database-connection-check`
- `copilot/fix-log-entries-table-error`
- `copilot/handle-database-dialect-layer`
- `copilot/move-enums-to-domain-enums`
- `copilot/remove-non-master-branches`
- `copilot/update-data-cleanup-logic`
- `copilot/update-precision-and-analysis`

## 方法一：使用GitHub Actions工作流 / Method 1: Use GitHub Actions Workflow

### 概述 / Overview

此方法使用GitHub Actions自动化工作流来删除所有非master分支。这是最简单和最安全的方法。

This method uses a GitHub Actions automated workflow to delete all non-master branches. This is the simplest and safest method.

### 使用步骤 / Usage Steps

1. **转到GitHub上的存储库 / Go to the repository on GitHub**
   
   访问 / Visit: https://github.com/Hisoka6602/ZakYip.Sorting.RuleEngine.Core

2. **单击"Actions"选项卡 / Click the "Actions" tab**

3. **从左侧边栏中选择"删除除主分支之外的所有分支"工作流程 / Select "Delete All Branches Except Master" workflow from the left sidebar**

4. **单击"Run workflow"按钮 / Click the "Run workflow" button**

5. **单击绿色的"Run workflow"按钮进行确认 / Click the green "Run workflow" button to confirm**

### 工作流功能 / Workflow Features

该工作流程将 / The workflow will:

- ✅ 获取所有远程分支 / Fetch all remote branches
- ✅ 识别除master以外的所有分支 / Identify all branches except master
- ✅ 从远程存储库中删除每个分支 / Delete each branch from the remote repository
- ✅ 报告删除期间的任何失败 / Report any failures during deletion
- ✅ 提供详细的执行日志 / Provide detailed execution logs
- ✅ 验证清理结果 / Verify cleanup results

### 优点 / Advantages

- 🔒 **安全** / **Safe**: 在GitHub环境中运行，有完整的审计日志
- 🚀 **简单** / **Simple**: 无需本地环境或权限配置
- 📊 **透明** / **Transparent**: 可以在Actions日志中查看所有操作
- ⏱️ **异步** / **Asynchronous**: 不会阻塞本地工作

## 方法二：使用提供的脚本 / Method 2: Use Provided Scripts

### Linux/macOS

```bash
# 预览要删除的分支（不实际删除）
# Preview branches to be deleted (dry-run)
chmod +x cleanup-branches.sh
./cleanup-branches.sh --dry-run

# 执行删除操作
# Execute deletion
./cleanup-branches.sh
```

### Windows PowerShell

```powershell
# 预览要删除的分支（不实际删除）
# Preview branches to be deleted (dry-run)
.\cleanup-branches.ps1 -DryRun

# 执行删除操作（需要确认）
# Execute deletion (requires confirmation)
.\cleanup-branches.ps1

# 执行删除操作（跳过确认）
# Execute deletion (skip confirmation)
.\cleanup-branches.ps1 -Force
```

## 方法三：使用Git命令 / Method 3: Use Git Commands

### 查看所有远程分支 / List all remote branches

```bash
git branch -r
```

### 删除单个远程分支 / Delete a single remote branch

```bash
git push origin --delete <branch-name>
```

### 批量删除所有非master分支 / Batch delete all non-master branches

**Linux/macOS:**

```bash
# 列出要删除的分支
git branch -r | grep -v 'HEAD' | grep -v -E 'origin/master$' | tr -d ' ' | sed 's/^origin\///' | xargs -I {} echo {}

# 删除所有非master分支
git branch -r | grep -v 'HEAD' | grep -v -E 'origin/master$' | tr -d ' ' | sed 's/^origin\///' | xargs -I {} git push origin --delete {}
```

**Windows PowerShell:**

```powershell
# 列出要删除的分支
git branch -r | Where-Object { $_ -notmatch 'HEAD' -and $_ -notmatch 'origin/master$' } | ForEach-Object { $_.Trim() -replace '^origin/', '' }

# 删除所有非master分支
git branch -r | Where-Object { $_ -notmatch 'HEAD' -and $_ -notmatch 'origin/master$' } | ForEach-Object { $branch = $_.Trim() -replace '^origin/', ''; git push origin --delete $branch }
```

## 方法四：使用GitHub Web界面 / Method 4: Use GitHub Web Interface

1. 访问仓库页面 / Visit repository page:
   https://github.com/Hisoka6602/ZakYip.Sorting.RuleEngine.Core

2. 点击 "branches" / Click "branches":
   https://github.com/Hisoka6602/ZakYip.Sorting.RuleEngine.Core/branches

3. 对于每个要删除的分支，点击垃圾桶图标 / For each branch to delete, click the trash icon

## 注意事项 / Important Notes

⚠️ **删除前确认 / Confirm before deletion:**
- 确保所有分支的代码已合并到master / Ensure all branch code is merged to master
- 备份重要的未合并代码 / Backup important unmerged code
- 删除后无法恢复（除非有备份）/ Cannot be recovered after deletion (unless backed up)

✅ **最佳实践 / Best Practices:**
- 先使用 `--dry-run` 或 `-DryRun` 预览 / Preview with `--dry-run` or `-DryRun` first
- 定期清理已合并的分支 / Regularly clean up merged branches
- 保持分支命名规范 / Maintain branch naming conventions

## 清理后验证 / Verification After Cleanup

```bash
# 查看所有远程分支
# View all remote branches
git fetch --prune
git branch -r

# 应该只显示 master 分支
# Should only show master branch
```

## 故障排除 / Troubleshooting

### 权限错误 / Permission Error

```
remote: Permission to repository denied
```

**解决方法 / Solution:**
确保你有仓库的写权限 / Ensure you have write permission to the repository

### 分支不存在 / Branch Not Found

```
error: unable to delete 'branch-name': remote ref does not exist
```

**解决方法 / Solution:**
先执行 `git fetch --prune` 更新远程分支列表 / Run `git fetch --prune` to update remote branch list

### 默认分支保护 / Default Branch Protection

```
error: Cannot delete branch 'master'
```

**解决方法 / Solution:**
这是正常的，脚本会自动跳过master分支 / This is normal, the script automatically skips the master branch

## 相关资源 / Related Resources

- [Git Documentation - Branch Management](https://git-scm.com/book/en/v2/Git-Branching-Branch-Management)
- [GitHub Documentation - Deleting Branches](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-branches-in-your-repository/deleting-and-restoring-branches-in-a-pull-request)
