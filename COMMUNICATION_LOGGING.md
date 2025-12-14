# 通信日志配置说明 / Communication Logging Configuration

## 概述 / Overview

本系统为下游分拣机和DWS设备的所有通信内容提供了独立的日志文件记录功能。

This system provides separate log files for all communication with downstream sorter devices and DWS devices.

## 日志文件 / Log Files

### 1. 下游分拣机通信日志 / Downstream Sorter Communication Logs

**文件位置** / **File Location:**
```
./logs/comm-sorter-{date}.log
./logs/archives/comm-sorter-{date}.log (归档/archived)
```

**记录内容** / **Logged Content:**
- TCP JSON Server 所有通信（Server 模式）
- TCP Client 所有通信（Client 模式）
- SorterAdapterManager 操作日志
- 包裹检测通知接收
- 格口分配通知发送
- 落格完成通知接收
- 连接建立/断开事件
- 所有异常和错误

**日志级别** / **Log Level:** Trace 及以上

**保留时间** / **Retention:** 30 天

### 2. DWS设备通信日志 / DWS Device Communication Logs

**文件位置** / **File Location:**
```
./logs/comm-dws-{date}.log
./logs/archives/comm-dws-{date}.log (归档/archived)
```

**记录内容** / **Logged Content:**
- DWS TCP Server 所有通信
- DwsAdapterManager 操作日志
- DWS 数据接收
- DWS 数据解析
- 连接建立/断开事件
- 所有异常和错误

**日志级别** / **Log Level:** Trace 及以上

**保留时间** / **Retention:** 30 天

## 日志格式 / Log Format

```
{timestamp}|{level}|{logger}|{message} {exception}
```

**示例** / **Example:**
```
2024-12-14 21:00:00.123|INFO|ZakYip.Sorting.RuleEngine.Infrastructure.Communication.DownstreamTcpJsonServer|下游分拣机已连接: 192.168.1.100:5001, 当前连接数: 1
2024-12-14 21:00:01.456|INFO|ZakYip.Sorting.RuleEngine.Infrastructure.Communication.DownstreamTcpJsonServer|已发送格口分配到客户端 192.168.1.100:5001: ParcelId=123, ChuteId=5
```

## 配置位置 / Configuration Location

日志配置文件：`Service/ZakYip.Sorting.RuleEngine.Service/nlog.config`

Log configuration file: `Service/ZakYip.Sorting.RuleEngine.Service/nlog.config`

## NLog 配置规则 / NLog Configuration Rules

```xml
<!-- 下游分拣机通信日志 -->
<logger name="*.Communication.DownstreamTcpJsonServer" minlevel="Trace" writeTo="sorterCommFile" final="true" />
<logger name="*.Adapters.Sorter.*" minlevel="Trace" writeTo="sorterCommFile" final="true" />
<logger name="*.SorterAdapterManager" minlevel="Trace" writeTo="sorterCommFile" final="true" />

<!-- DWS设备通信日志 -->
<logger name="*.Adapters.Dws.*" minlevel="Trace" writeTo="dwsCommFile" final="true" />
<logger name="*.DwsAdapterManager" minlevel="Trace" writeTo="dwsCommFile" final="true" />
```

## 日志文件自动管理 / Automatic Log File Management

- **每日归档** / **Daily Archive:** 每天自动创建新的日志文件
- **自动清理** / **Auto Cleanup:** 超过保留期的日志自动删除
- **并发写入** / **Concurrent Writes:** 支持多线程并发写入
- **UTF-8编码** / **UTF-8 Encoding:** 统一使用 UTF-8 编码

## 日志查看建议 / Log Viewing Recommendations

### Windows
```powershell
# 实时查看下游分拣机通信日志
Get-Content ./logs/comm-sorter-20241214.log -Wait -Tail 50

# 实时查看 DWS 通信日志
Get-Content ./logs/comm-dws-20241214.log -Wait -Tail 50
```

### Linux
```bash
# 实时查看下游分拣机通信日志
tail -f ./logs/comm-sorter-20241214.log

# 实时查看 DWS 通信日志
tail -f ./logs/comm-dws-20241214.log
```

## 代码重复检查 / Code Duplication Check

### 检查结果 / Check Results

✅ **无影分身代码** / **No Shadow Clone Code**

下游分拣机和DWS设备的通信实现使用了不同的技术栈和设计模式：

- **下游分拣机** / **Downstream Sorter:**
  - Server 模式：DownstreamTcpJsonServer (支持多设备连接)
  - Client 模式：TcpSorterAdapter (基础 TcpClient)
  - 协议：TCP + JSON (每行一个JSON对象)
  - 用途：接收包裹检测、发送格口分配、接收落格完成

- **DWS设备** / **DWS Device:**
  - TouchSocketDwsAdapter (基于 TouchSocket 高性能服务器)
  - TouchSocketDwsTcpClientAdapter (TouchSocket 客户端)
  - MqttDwsAdapter (MQTT 协议)
  - 协议：TCP + 自定义数据模板
  - 用途：接收DWS测量数据

**结论** / **Conclusion:** 两者虽然都使用TCP通信，但实现方式、协议格式、用途完全不同，不存在重复代码。

---

**文档版本** / **Document Version:** 1.0  
**最后更新** / **Last Updated:** 2024-12-14  
**维护者** / **Maintainer:** ZakYip Development Team
