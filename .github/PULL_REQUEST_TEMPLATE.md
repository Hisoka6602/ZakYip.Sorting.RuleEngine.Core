## 📝 PR 描述 / PR Description

<!-- 请简要描述此 PR 的更改内容 / Please briefly describe the changes in this PR -->



## 🔗 关联的 Issue / Related Issues

<!-- 请填写关联的 Issue 编号 / Please fill in the related issue number -->
Closes #

## ✅ 技术债务检查清单 / Technical Debt Checklist

**⚠️ 重要：提交 PR 前必须完成以下检查 / IMPORTANT: The following checks must be completed before submitting PR**

### 基础检查 / Basic Checks
- [ ] 📖 我已通读 [TECHNICAL_DEBT.md](../TECHNICAL_DEBT.md) 文档 / I have read through the TECHNICAL_DEBT.md document
- [ ] 🔍 我已运行 `jscpd` 检查，代码重复率未超过 5% / I have run `jscpd` check and code duplication rate does not exceed 5%
- [ ] 🚫 此 PR 未引入新的重复代码（影分身代码） / This PR does not introduce new duplicate code (shadow clone code)
- [ ] 📝 如果解决了技术债务，我已更新 TECHNICAL_DEBT.md / If technical debt was resolved, I have updated TECHNICAL_DEBT.md
- [ ] ⚡ 如果引入了新的技术债务，我已在 TECHNICAL_DEBT.md 中记录 / If new technical debt was introduced, it has been documented in TECHNICAL_DEBT.md

### 影分身语义检查 / Shadow Clone Semantic Checks (7 Types)

我已检查以下 7 种类型的影分身代码 / I have checked the following 7 types of shadow clone code:

- [ ] 1️⃣ **枚举检查 / Enum Check**: 是否新增枚举？是否与现有枚举语义重复？/ New enums added? Semantic duplicates with existing enums?
- [ ] 2️⃣ **接口检查 / Interface Check**: 是否新增接口？是否与现有接口方法签名重叠？/ New interfaces added? Method signature overlaps with existing interfaces?
- [ ] 3️⃣ **DTO检查 / DTO Check**: 是否新增DTO？是否与现有DTO字段结构相同？/ New DTOs added? Field structures identical to existing DTOs?
- [ ] 4️⃣ **Options检查 / Options Check**: 是否新增配置类？是否在多个命名空间重复？/ New config classes added? Duplicated across multiple namespaces?
- [ ] 5️⃣ **扩展方法检查 / Extension Method Check**: 是否新增扩展方法？是否与现有扩展方法签名相同？/ New extension methods added? Signatures identical to existing extension methods?
- [ ] 6️⃣ **静态类检查 / Static Class Check**: 是否新增工具类？是否与现有工具类功能重复？/ New utility classes added? Functionality duplicates existing utility classes?
- [ ] 7️⃣ **常量检查 / Constant Check**: 是否定义常量？是否与现有常量值相同？/ Constants defined? Values identical to existing constants?

**检测方法 / Detection Method:**
```bash
# 运行影分身语义检测工具 / Run shadow clone semantic detector
./shadow-clone-check.sh .
```

## 🔄 代码重复检测结果 / Code Duplication Detection Result

<!-- 请粘贴 jscpd 运行结果摘要 / Please paste the jscpd run result summary -->
```
重复代码比例 / Duplicate code ratio: ___%
```

## 📋 更改类型 / Type of Change

- [ ] 🐛 Bug 修复 / Bug fix
- [ ] ✨ 新功能 / New feature
- [ ] 💥 破坏性更改 / Breaking change
- [ ] 📚 文档更新 / Documentation update
- [ ] ♻️ 代码重构 / Code refactoring
- [ ] 🧹 技术债务清理 / Technical debt cleanup

## 🧪 测试 / Testing

- [ ] 我已添加/更新测试来覆盖我的更改 / I have added/updated tests to cover my changes
- [ ] 所有现有测试都通过 / All existing tests pass
- [ ] 我已在本地测试了我的更改 / I have tested my changes locally

## 📸 截图 / Screenshots (如适用 / if applicable)

<!-- 如果是 UI 更改，请添加截图 / If this is a UI change, please add screenshots -->

## 📌 其他说明 / Additional Notes

<!-- 任何其他相关信息 / Any other relevant information -->

---

**⚠️ 注意 / Note:**
- PR 如未勾选技术债务检查清单，将被要求补充后再进行审查
- PRs without completed technical debt checklist will be asked to complete before review
- 代码重复率超过 5% 的 PR 将被自动拒绝
- PRs with code duplication rate exceeding 5% will be automatically rejected
