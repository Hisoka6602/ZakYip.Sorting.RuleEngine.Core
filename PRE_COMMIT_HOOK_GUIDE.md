# Git Pre-commit Hook 安装指南 / Git Pre-commit Hook Installation Guide

## 📖 简介 / Introduction

本项目提供了一个 Git pre-commit hook，用于在提交代码前自动运行代码质量检查，包括：

This project provides a Git pre-commit hook to automatically run code quality checks before committing code, including:

1. **代码重复检测 (jscpd)** - 确保代码重复率 < 5%
2. **影分身语义检测** - 检测 7 种类型的语义重复

## 🚀 快速安装 / Quick Installation

### 方法 1: 符号链接 (推荐) / Method 1: Symbolic Link (Recommended)

```bash
# 从项目根目录运行 / Run from project root
ln -sf ../../pre-commit-hook.sh .git/hooks/pre-commit
chmod +x .git/hooks/pre-commit
```

**优点 / Advantages:**
- ✅ 脚本更新时自动同步 / Automatically syncs when script is updated
- ✅ 版本控制友好 / Version control friendly

### 方法 2: 直接复制 / Method 2: Direct Copy

```bash
# 从项目根目录运行 / Run from project root
cp pre-commit-hook.sh .git/hooks/pre-commit
chmod +x .git/hooks/pre-commit
```

**注意 / Note:** 使用此方法时，脚本更新后需要重新复制。
When using this method, you need to copy again when the script is updated.

## 🔧 前置要求 / Prerequisites

### 必需 / Required
- **Node.js** - 用于运行 jscpd
- **npm** - 用于安装 jscpd

### 可选 / Optional
- **.NET SDK 8.0+** - 用于运行影分身语义检测
- **bc** - 用于浮点数比较 (如果系统没有，会降级到 awk)

## 📋 Hook 执行流程 / Hook Execution Flow

```
┌─────────────────────────────────┐
│  git commit                     │
└─────────────┬───────────────────┘
              │
              ▼
┌─────────────────────────────────┐
│  Pre-commit Hook 触发            │
└─────────────┬───────────────────┘
              │
              ▼
┌─────────────────────────────────┐
│  步骤 1: 检查 jscpd 是否安装     │
│  (未安装则自动安装)              │
└─────────────┬───────────────────┘
              │
              ▼
┌─────────────────────────────────┐
│  步骤 2: 运行代码重复检测        │
│  (jscpd)                        │
└─────────────┬───────────────────┘
              │
        ┌─────┴─────┐
        │ 重复率 > 5%? │
        └─────┬─────┘
          ❌ Yes    │ ✅ No
        ┌──────┴─────┐
        │ 提交失败   │   ┌────────────────────┐
        │ 显示错误   │   │ 步骤 3: 运行影分身  │
        └────────────┘   │ 语义检测             │
                        └──────────┬───────────┘
                                  │
                        ┌─────────┴──────┐
                        │ 发现影分身?      │
                        └─────┬────┬────┘
                          ⚠️ Yes  │ ✅ No
                        ┌──────┘  │
                        │ 警告显示 │  ┌────────────┐
                        │ 不阻止提交│  │ 提交成功   │
                        └─────────┘  └────────────┘
```

## 🛑 如何跳过检查 / How to Skip Checks

在某些紧急情况下，你可以跳过 pre-commit 检查：

In emergency situations, you can skip pre-commit checks:

```bash
git commit --no-verify -m "emergency fix"
# 或 / or
git commit -n -m "emergency fix"
```

**⚠️ 警告 / Warning:** 不推荐跳过检查，除非：
- 紧急修复生产问题 / Emergency production fix
- 已经过手动验证 / Already manually verified
- 计划在后续 PR 中修复 / Planning to fix in follow-up PR

## 🔍 检查详情 / Check Details

### 1. 代码重复检测 / Code Duplication Detection

**工具 / Tool:** jscpd

**检查内容 / What it checks:**
- C# 源代码文件 (*.cs)
- 排除 bin/, obj/, Migrations/, Tests/, *.Designer.cs
- 最小行数: 10
- 最小 tokens: 50

**阈值 / Threshold:** 5%

**失败时的行为 / Behavior on failure:**
- ❌ 阻止提交 / Blocks commit
- 显示详细错误信息 / Shows detailed error message
- 提供解决方案链接 / Provides solution link

### 2. 影分身语义检测 / Shadow Clone Semantic Detection

**工具 / Tool:** ShadowCloneDetector (自研)

**检查类型 / Check types:**
1. 枚举重复 / Enum duplicates
2. 接口重复 / Interface duplicates
3. DTO 重复 / DTO duplicates
4. Options/配置类重复 / Options/Config class duplicates
5. 扩展方法重复 / Extension method duplicates
6. 静态类重复 / Static class duplicates
7. 常量重复 / Constant duplicates

**相似度阈值 / Similarity threshold:** 80%

**失败时的行为 / Behavior on failure:**
- ⚠️ 显示警告，但不阻止提交 / Shows warning but doesn't block commit
- 建议在 TECHNICAL_DEBT.md 中记录 / Suggests documenting in TECHNICAL_DEBT.md

## 🧪 测试 Hook / Testing the Hook

```bash
# 1. 安装 hook / Install hook
ln -sf ../../pre-commit-hook.sh .git/hooks/pre-commit
chmod +x .git/hooks/pre-commit

# 2. 创建测试提交 / Create test commit
echo "// test" >> test-file.cs
git add test-file.cs
git commit -m "test pre-commit hook"

# 3. 观察输出 / Observe output
# 应该看到代码质量检查运行 / Should see code quality checks running

# 4. 清理测试文件 / Cleanup test file
git reset HEAD~1
rm test-file.cs
```

## 📊 检查报告示例 / Check Report Example

### 成功示例 / Success Example

```
🔍 运行 Git Pre-commit 代码质量检查 / Running Git Pre-commit Code Quality Checks...
==========================================

📊 步骤 1/2: 代码重复检测 (jscpd) / Step 1/2: Code Duplication Detection (jscpd)
----------------------------------------

重复代码比例 / Duplication Rate: 3.17%
阈值 / Threshold: 5%

✅ 代码重复率检查通过 / Code duplication check passed

🎭 步骤 2/2: 影分身语义检测 / Step 2/2: Shadow Clone Semantic Detection
----------------------------------------

✅ 影分身语义检测通过 / Shadow clone semantic detection passed

==========================================
✅ Pre-commit 检查完成 / Pre-commit checks completed
```

### 失败示例 / Failure Example

```
🔍 运行 Git Pre-commit 代码质量检查 / Running Git Pre-commit Code Quality Checks...
==========================================

📊 步骤 1/2: 代码重复检测 (jscpd) / Step 1/2: Code Duplication Detection (jscpd)
----------------------------------------

重复代码比例 / Duplication Rate: 6.50%
阈值 / Threshold: 5%

❌ 代码重复率 6.50% 超过阈值 5%
Code duplication rate 6.50% exceeds threshold 5%

📖 请参考 TECHNICAL_DEBT.md 了解如何解决重复代码问题
📖 Please refer to TECHNICAL_DEBT.md for guidance on resolving duplicate code

💡 提示: 你可以使用 --no-verify 跳过此检查 (不推荐)
💡 Tip: You can use --no-verify to skip this check (not recommended)
```

## 🔧 故障排查 / Troubleshooting

### 问题 1: jscpd 安装失败 / Problem 1: jscpd installation failed

**症状 / Symptoms:**
```
❌ jscpd 安装失败，请手动安装: npm install -g jscpd
```

**解决方案 / Solution:**
```bash
# 手动安装 jscpd / Manually install jscpd
npm install -g jscpd

# 或使用 sudo (Linux/Mac) / Or use sudo (Linux/Mac)
sudo npm install -g jscpd

# Windows 用户需要以管理员身份运行 / Windows users need to run as administrator
```

### 问题 2: 权限被拒绝 / Problem 2: Permission denied

**症状 / Symptoms:**
```
bash: .git/hooks/pre-commit: Permission denied
```

**解决方案 / Solution:**
```bash
chmod +x .git/hooks/pre-commit
chmod +x pre-commit-hook.sh
chmod +x shadow-clone-check.sh
```

### 问题 3: .NET SDK 未找到 / Problem 3: .NET SDK not found

**症状 / Symptoms:**
```
⚠️ .NET SDK 未安装，跳过影分身检测
```

**解决方案 / Solution:**
- 这是可选的，不会影响提交 / This is optional and won't affect commits
- 如需完整检查，请安装 .NET SDK 8.0+ / For complete checks, install .NET SDK 8.0+
- 下载地址 / Download: https://dotnet.microsoft.com/download

### 问题 4: bc 命令未找到 / Problem 4: bc command not found

**解决方案 / Solution:**
- Hook 会自动降级到 awk / Hook will automatically fallback to awk
- 或手动安装 bc / Or manually install bc:
  ```bash
  # Ubuntu/Debian
  sudo apt-get install bc
  
  # CentOS/RHEL
  sudo yum install bc
  
  # macOS
  brew install bc
  ```

## 📚 相关文档 / Related Documentation

- [TECHNICAL_DEBT.md](../TECHNICAL_DEBT.md) - 技术债务文档
- [SHADOW_CLONE_DETECTION_GUIDE.md](../SHADOW_CLONE_DETECTION_GUIDE.md) - 影分身检测指南
- [.github/workflows/ci.yml](../.github/workflows/ci.yml) - CI/CD 工作流

## 💡 最佳实践 / Best Practices

1. **总是启用 pre-commit hook** / Always enable pre-commit hook
   - 在开发前安装 / Install before development
   - 确保团队所有成员都安装 / Ensure all team members install

2. **不要跳过检查** / Don't skip checks
   - 除非紧急情况 / Unless emergency
   - 跳过后要及时修复 / Fix issues after skipping

3. **定期更新脚本** / Regularly update scripts
   - 拉取最新代码后重新安装 / Reinstall after pulling latest code
   - 如使用符号链接，会自动更新 / Automatically updates with symbolic link

4. **理解检查失败原因** / Understand check failure reasons
   - 阅读错误消息 / Read error messages
   - 参考 TECHNICAL_DEBT.md / Refer to TECHNICAL_DEBT.md
   - 解决根本问题，而不是跳过检查 / Fix root cause instead of skipping

## 🤝 贡献 / Contributing

如果你发现 hook 有问题或有改进建议：

If you find issues with the hook or have improvement suggestions:

1. 在 TECHNICAL_DEBT.md 中记录 / Document in TECHNICAL_DEBT.md
2. 提交 Issue / Submit an Issue
3. 提交 PR 改进脚本 / Submit a PR to improve the script

---

**最后更新 / Last Updated:** 2025-12-11
**维护者 / Maintainer:** ZakYip.Sorting.RuleEngine.Core Team
