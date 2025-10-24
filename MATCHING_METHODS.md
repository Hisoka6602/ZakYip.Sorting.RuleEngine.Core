# 匹配方法文档

## 概述

ZakYip分拣规则引擎支持多种强大的匹配方法，用于灵活地定义分拣规则。每种匹配方法都针对特定的数据类型和使用场景进行了优化。

## 匹配方法类型

### 1. 条码正则匹配 (BarcodeRegex)

用于通过正则表达式或预设选项匹配条码。

#### 预设选项

```
STARTSWITH:SF          # 条码以SF开头
CONTAINS:ABC           # 条码包含ABC
NOTCONTAINS:XYZ        # 条码不包含XYZ
ALLDIGITS              # 条码全为数字
ALPHANUMERIC           # 条码为字母+数字
LENGTH:5-10            # 条码长度在5-10之间
```

#### 自定义正则

```
REGEX:^SF\d{6}$        # 自定义正则：以SF开头后跟6位数字
^64\d*$                # 直接使用正则：以64开头后跟任意数字
```

#### 示例规则

```json
{
  "ruleId": "R001",
  "ruleName": "顺丰快递识别",
  "matchingMethod": "BarcodeRegex",
  "conditionExpression": "STARTSWITH:SF",
  "targetChute": "CHUTE-SF-01",
  "priority": 1
}
```

### 2. 重量匹配 (WeightMatch)

用于根据包裹重量进行匹配。支持复杂的逻辑表达式。

#### 支持的操作符

- `>` 大于
- `<` 小于
- `=` 或 `==` 等于
- `>=` 大于等于
- `<=` 小于等于
- `and` 或 `&` 逻辑与
- `or` 或 `|` 逻辑或

#### 表达式示例

```
Weight > 50                           # 重量大于50克
Weight < 100 and Weight > 10          # 重量在10-100克之间
Weight > 1000 or Weight < 50          # 重量大于1000克或小于50克
```

#### 示例规则

```json
{
  "ruleId": "R002",
  "ruleName": "重包裹分拣",
  "matchingMethod": "WeightMatch",
  "conditionExpression": "Weight > 1000",
  "targetChute": "CHUTE-HEAVY-01",
  "priority": 2
}
```

### 3. 体积匹配 (VolumeMatch)

用于根据包裹的长宽高或体积进行匹配。

#### 可用变量

- `Length` 长度（毫米）
- `Width` 宽度（毫米）
- `Height` 高度（毫米）
- `Volume` 体积（立方厘米）

#### 表达式示例

```
Length > 20 and Width > 10            # 长度大于20且宽度大于10
Height = 20.5 or Volume > 200         # 高度等于20.5或体积大于200
Length > 300 and Width > 200 and Height > 150  # 超大件
```

#### 示例规则

```json
{
  "ruleId": "R003",
  "ruleName": "超大件分拣",
  "matchingMethod": "VolumeMatch",
  "conditionExpression": "Length > 500 or Width > 400 or Height > 300",
  "targetChute": "CHUTE-OVERSIZE-01",
  "priority": 3
}
```

### 4. OCR匹配 (OcrMatch)

用于根据OCR识别的地址信息和电话后缀进行匹配。

#### 可用字段

- `threeSegmentCode` 三段码（完整）
- `firstSegmentCode` 第一段码
- `secondSegmentCode` 第二段码
- `thirdSegmentCode` 第三段码
- `recipientAddress` 收件人地址
- `senderAddress` 寄件人地址
- `recipientPhoneSuffix` 收件人电话后缀
- `senderPhoneSuffix` 寄件人电话后缀

#### 表达式示例

```
firstSegmentCode=^64\d*$              # 第一段码以64开头
recipientPhoneSuffix=1234             # 收件人电话后缀为1234
firstSegmentCode=^64\d*$ and recipientPhoneSuffix=1234  # 组合条件
```

#### 示例规则

```json
{
  "ruleId": "R004",
  "ruleName": "西安地区分拣",
  "matchingMethod": "OcrMatch",
  "conditionExpression": "firstSegmentCode=^64\\d*$",
  "targetChute": "CHUTE-XIAN-01",
  "priority": 4
}
```

### 5. API响应内容匹配 (ApiResponseMatch)

用于根据第三方API返回的数据进行匹配。

#### 匹配类型

##### 字符串查找（正向）

```
STRING:keyword                        # 在响应中查找关键字
```

##### 字符串查找（反向）

```
STRING_REVERSE:keyword                # 从后往前查找关键字
```

##### 正则查找

```
REGEX:\d{3}                          # 使用正则表达式匹配
```

##### JSON匹配

```
JSON:status=success                   # 匹配JSON字段
JSON:data.user.name=John              # 匹配嵌套JSON字段
```

#### 示例规则

```json
{
  "ruleId": "R005",
  "ruleName": "VIP客户分拣",
  "matchingMethod": "ApiResponseMatch",
  "conditionExpression": "JSON:customer.vipLevel=Gold",
  "targetChute": "CHUTE-VIP-01",
  "priority": 5
}
```

### 6. 低代码表达式匹配 (LowCodeExpression)

用于编写灵活的自定义表达式，可以混合使用多种条件。

#### 表达式示例

```
if(Weight>10) and firstSegmentCode=^64\d*$
Weight > 50 and Length > 300
Barcode=STARTSWITH:SF and Volume > 1000
```

#### 示例规则

```json
{
  "ruleId": "R006",
  "ruleName": "复杂规则",
  "matchingMethod": "LowCodeExpression",
  "conditionExpression": "if(Weight>10) and firstSegmentCode=^64\\d*$",
  "targetChute": "CHUTE-COMPLEX-01",
  "priority": 6
}
```

### 7. 传统表达式 (LegacyExpression)

兼容旧版本的表达式语法。

#### 表达式示例

```
Weight > 1000
Barcode CONTAINS 'SF'
Volume < 50000
DEFAULT
```

## 规则优先级

- 规则按照 `priority` 字段排序，数字越小优先级越高
- 系统会收集所有匹配的规则，返回优先级最高（priority值最小）的规则
- 一个包裹可以匹配多条规则，但只会使用第一个匹配的规则的格口号

## 完整示例

```json
[
  {
    "ruleId": "R001",
    "ruleName": "顺丰快递",
    "matchingMethod": "BarcodeRegex",
    "conditionExpression": "STARTSWITH:SF",
    "targetChute": "CHUTE-SF-01",
    "priority": 1,
    "isEnabled": true
  },
  {
    "ruleId": "R002",
    "ruleName": "重包裹",
    "matchingMethod": "WeightMatch",
    "conditionExpression": "Weight > 1000",
    "targetChute": "CHUTE-HEAVY-01",
    "priority": 2,
    "isEnabled": true
  },
  {
    "ruleId": "R003",
    "ruleName": "超大件",
    "matchingMethod": "VolumeMatch",
    "conditionExpression": "Length > 500 or Width > 400",
    "targetChute": "CHUTE-OVERSIZE-01",
    "priority": 3,
    "isEnabled": true
  },
  {
    "ruleId": "R004",
    "ruleName": "西安地区",
    "matchingMethod": "OcrMatch",
    "conditionExpression": "firstSegmentCode=^64\\d*$",
    "targetChute": "CHUTE-XIAN-01",
    "priority": 4,
    "isEnabled": true
  },
  {
    "ruleId": "R005",
    "ruleName": "VIP客户",
    "matchingMethod": "ApiResponseMatch",
    "conditionExpression": "JSON:customer.vipLevel=Gold",
    "targetChute": "CHUTE-VIP-01",
    "priority": 5,
    "isEnabled": true
  },
  {
    "ruleId": "R006",
    "ruleName": "复杂规则",
    "matchingMethod": "LowCodeExpression",
    "conditionExpression": "Weight>50 and firstSegmentCode=^64\\d*$",
    "targetChute": "CHUTE-COMPLEX-01",
    "priority": 6,
    "isEnabled": true
  }
]
```

## 最佳实践

1. **合理设置优先级** - 更具体的规则应该有更高的优先级（更小的priority值）
2. **避免过度复杂** - 复杂的表达式可以拆分为多个简单规则
3. **充分测试** - 在生产环境使用前充分测试规则
4. **使用描述** - 在description字段中详细说明规则用途
5. **定期审查** - 定期审查和优化规则配置

## 性能考虑

- 条码正则匹配性能最优
- OCR匹配和API响应匹配依赖第三方数据
- 低代码表达式匹配最灵活但可能稍慢
- 规则缓存提高了整体性能（5分钟滑动过期）
