# 部署指南

本文档提供ZakYip分拣规则引擎系统的详细部署说明。

## 前置要求

### 必需
- Windows Server 2019 或更高版本（用于Windows服务）
- .NET 8.0 Runtime
- 至少 2GB 可用内存
- 至少 1GB 可用磁盘空间

### 可选
- MySQL 8.0 或更高版本（用于日志存储）
- IIS（如果需要反向代理）

## 部署步骤

### 1. 准备环境

#### 安装 .NET 8.0 Runtime

```powershell
# 下载并安装 .NET 8.0 Runtime
# https://dotnet.microsoft.com/download/dotnet/8.0
```

#### 配置MySQL（可选）

如果使用MySQL进行日志存储：

```sql
-- 创建数据库
CREATE DATABASE sorting_logs CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- 创建用户
CREATE USER 'sorting_user'@'localhost' IDENTIFIED BY 'your_secure_password';

-- 授权
GRANT ALL PRIVILEGES ON sorting_logs.* TO 'sorting_user'@'localhost';
FLUSH PRIVILEGES;
```

### 2. 发布应用

在开发机器上：

```bash
# 进入项目目录
cd ZakYip.Sorting.RuleEngine.Core

# 发布为自包含应用（推荐）
dotnet publish ZakYip.Sorting.RuleEngine.Service/ZakYip.Sorting.RuleEngine.Service.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -o ./publish

# 或发布为框架依赖应用
dotnet publish ZakYip.Sorting.RuleEngine.Service/ZakYip.Sorting.RuleEngine.Service.csproj \
  -c Release \
  -o ./publish
```

### 3. 复制文件到服务器

将 `publish` 文件夹复制到服务器，例如：

```
C:\Programs\ZakYipSortingEngine\
```

### 4. 配置应用

编辑 `appsettings.json`：

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AppSettings": {
    "LiteDb": {
      "ConnectionString": "Filename=C:\\ProgramData\\ZakYipSortingEngine\\config.db;Connection=shared"
    },
    "MySql": {
      "ConnectionString": "Server=localhost;Database=sorting_logs;User=sorting_user;Password=your_secure_password;",
      "Enabled": true
    },
    "Sqlite": {
      "ConnectionString": "Data Source=C:\\ProgramData\\ZakYipSortingEngine\\logs.db"
    },
    "ThirdPartyApi": {
      "BaseUrl": "https://your-api-server.com",
      "TimeoutSeconds": 30,
      "ApiKey": "your-api-key-here"
    },
    "MiniApi": {
      "Urls": [ "http://0.0.0.0:5000" ],
      "EnableSwagger": false
    }
  }
}
```

### 5. 创建数据目录

```powershell
# 创建数据目录
New-Item -Path "C:\ProgramData\ZakYipSortingEngine" -ItemType Directory -Force

# 设置权限
icacls "C:\ProgramData\ZakYipSortingEngine" /grant "NETWORK SERVICE:(OI)(CI)F" /T
```

### 6. 安装为Windows服务

#### 方法1: 使用 sc 命令

```powershell
# 创建服务
sc create "ZakYipSortingEngine" `
  binPath= "C:\Programs\ZakYipSortingEngine\ZakYip.Sorting.RuleEngine.Service.exe" `
  start= auto `
  DisplayName= "ZakYip Sorting Rule Engine"

# 设置服务描述
sc description "ZakYipSortingEngine" "高性能分拣规则引擎核心系统"

# 配置服务恢复选项
sc failure "ZakYipSortingEngine" reset= 86400 actions= restart/5000/restart/10000/restart/30000

# 启动服务
sc start "ZakYipSortingEngine"

# 查看服务状态
sc query "ZakYipSortingEngine"
```

#### 方法2: 使用 PowerShell

```powershell
$serviceName = "ZakYipSortingEngine"
$displayName = "ZakYip Sorting Rule Engine"
$description = "高性能分拣规则引擎核心系统"
$binaryPath = "C:\Programs\ZakYipSortingEngine\ZakYip.Sorting.RuleEngine.Service.exe"

# 创建服务
New-Service -Name $serviceName `
  -BinaryPathName $binaryPath `
  -DisplayName $displayName `
  -Description $description `
  -StartupType Automatic

# 启动服务
Start-Service -Name $serviceName

# 查看服务状态
Get-Service -Name $serviceName
```

### 7. 配置防火墙

```powershell
# 允许入站连接
New-NetFirewallRule -DisplayName "ZakYip Sorting Engine API" `
  -Direction Inbound `
  -Protocol TCP `
  -LocalPort 5000 `
  -Action Allow
```

### 8. 验证部署

#### 检查服务状态

```powershell
Get-Service -Name "ZakYipSortingEngine"
```

#### 测试API

```powershell
# 健康检查
Invoke-RestMethod -Uri "http://localhost:5000/health" -Method Get

# 版本信息
Invoke-RestMethod -Uri "http://localhost:5000/version" -Method Get
```

## 高级配置

### 配置HTTPS

1. 生成SSL证书：

```powershell
# 使用 PowerShell 生成自签名证书（开发/测试用）
$cert = New-SelfSignedCertificate -DnsName "sorting-engine.local" -CertStoreLocation "cert:\LocalMachine\My"

# 导出证书
Export-Certificate -Cert $cert -FilePath "C:\Programs\ZakYipSortingEngine\cert.cer"
```

2. 更新 `appsettings.json`：

```json
{
  "AppSettings": {
    "MiniApi": {
      "Urls": [ "https://0.0.0.0:5001" ],
      "EnableSwagger": false
    }
  },
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://0.0.0.0:5001",
        "Certificate": {
          "Path": "cert.pfx",
          "Password": "your-password"
        }
      }
    }
  }
}
```

### 使用IIS作为反向代理

1. 安装 ASP.NET Core Module for IIS

2. 创建IIS站点指向应用目录

3. 配置 `web.config`：

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <handlers>
      <add name="aspNetCore" path="*" verb="*" 
           modules="AspNetCoreModuleV2" 
           resourceType="Unspecified" />
    </handlers>
    <aspNetCore processPath=".\ZakYip.Sorting.RuleEngine.Service.exe" 
                stdoutLogEnabled="true" 
                stdoutLogFile=".\logs\stdout" 
                hostingModel="OutOfProcess" />
  </system.webServer>
</configuration>
```

### 配置负载均衡

使用Nginx作为负载均衡器：

```nginx
upstream sorting_engine {
    server 127.0.0.1:5001;
    server 127.0.0.1:5002;
    server 127.0.0.1:5003;
}

server {
    listen 80;
    server_name sorting-engine.local;

    location / {
        proxy_pass http://sorting_engine;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

## 监控和维护

### 日志位置

- **应用日志**: `C:\ProgramData\ZakYipSortingEngine\logs.db` (SQLite)
- **Windows事件日志**: Event Viewer → Windows Logs → Application

### 性能监控

使用Windows性能监视器监控：

```powershell
# 查看服务进程
Get-Process -Name "ZakYip.Sorting.RuleEngine.Service"

# 监控CPU和内存
Get-Counter '\Process(ZakYip.Sorting.RuleEngine.Service)\% Processor Time',
            '\Process(ZakYip.Sorting.RuleEngine.Service)\Working Set'
```

### 定期维护

#### 数据库清理

```sql
-- MySQL: 删除30天前的日志
DELETE FROM log_entries 
WHERE CreatedAt < DATE_SUB(NOW(), INTERVAL 30 DAY);

-- SQLite: 删除30天前的日志
DELETE FROM log_entries 
WHERE CreatedAt < datetime('now', '-30 days');

-- 优化数据库
VACUUM;
```

#### 服务重启脚本

创建 `restart-service.ps1`：

```powershell
$serviceName = "ZakYipSortingEngine"

# 停止服务
Stop-Service -Name $serviceName -Force

# 等待服务完全停止
Start-Sleep -Seconds 5

# 启动服务
Start-Service -Name $serviceName

# 验证服务状态
Get-Service -Name $serviceName

# 等待API就绪
Start-Sleep -Seconds 10

# 健康检查
try {
    $response = Invoke-RestMethod -Uri "http://localhost:5000/health" -Method Get
    Write-Host "服务健康状态: $($response.status)"
} catch {
    Write-Error "健康检查失败: $_"
}
```

## 卸载

### 停止并删除服务

```powershell
# 停止服务
Stop-Service -Name "ZakYipSortingEngine" -Force

# 删除服务
sc delete "ZakYipSortingEngine"
```

### 清理数据

```powershell
# 删除应用程序文件
Remove-Item -Path "C:\Programs\ZakYipSortingEngine" -Recurse -Force

# 删除数据文件
Remove-Item -Path "C:\ProgramData\ZakYipSortingEngine" -Recurse -Force
```

### 删除防火墙规则

```powershell
Remove-NetFirewallRule -DisplayName "ZakYip Sorting Engine API"
```

## 故障排查

### 服务无法启动

1. 检查事件查看器中的错误日志
2. 验证配置文件格式
3. 确认端口未被占用：`netstat -ano | findstr :5000`
4. 检查文件权限

### 数据库连接失败

1. 验证MySQL服务运行：`Get-Service -Name "MySQL"`
2. 测试连接字符串
3. 检查防火墙规则
4. 查看SQLite降级日志

### 性能问题

1. 监控CPU和内存使用
2. 检查数据库索引
3. 清理旧日志数据
4. 调整对象池大小

## 备份和恢复

### 备份

```powershell
# 创建备份脚本
$backupPath = "C:\Backups\ZakYipSortingEngine\$(Get-Date -Format 'yyyyMMdd')"
New-Item -Path $backupPath -ItemType Directory -Force

# 备份配置数据库
Copy-Item "C:\ProgramData\ZakYipSortingEngine\config.db" -Destination $backupPath

# 备份日志数据库
Copy-Item "C:\ProgramData\ZakYipSortingEngine\logs.db" -Destination $backupPath

# 备份配置文件
Copy-Item "C:\Programs\ZakYipSortingEngine\appsettings.json" -Destination $backupPath
```

### 恢复

```powershell
# 停止服务
Stop-Service -Name "ZakYipSortingEngine" -Force

# 恢复数据
$restorePath = "C:\Backups\ZakYipSortingEngine\20240101"
Copy-Item "$restorePath\config.db" -Destination "C:\ProgramData\ZakYipSortingEngine\" -Force
Copy-Item "$restorePath\logs.db" -Destination "C:\ProgramData\ZakYipSortingEngine\" -Force

# 启动服务
Start-Service -Name "ZakYipSortingEngine"
```

## 安全建议

1. **更改默认端口**: 修改 `appsettings.json` 中的端口配置
2. **使用强密码**: 为数据库和API密钥使用复杂密码
3. **启用HTTPS**: 在生产环境中始终使用HTTPS
4. **定期更新**: 保持.NET运行时和依赖项最新
5. **限制访问**: 使用防火墙规则限制API访问
6. **审计日志**: 定期审查系统日志

## 容器化部署（可选）

如果希望使用Docker部署：

### Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app
COPY publish/ .

ENV ASPNETCORE_URLS=http://+:5000

EXPOSE 5000

ENTRYPOINT ["dotnet", "ZakYip.Sorting.RuleEngine.Service.dll"]
```

### Docker Compose

```yaml
version: '3.8'

services:
  sorting-engine:
    build: .
    ports:
      - "5000:5000"
    environment:
      - AppSettings__MySql__ConnectionString=Server=mysql;Database=sorting_logs;User=root;Password=password;
    volumes:
      - ./data:/app/data
    depends_on:
      - mysql
    restart: unless-stopped

  mysql:
    image: mysql:8.0
    environment:
      - MYSQL_ROOT_PASSWORD=password
      - MYSQL_DATABASE=sorting_logs
    volumes:
      - mysql-data:/var/lib/mysql
    restart: unless-stopped

volumes:
  mysql-data:
```

---

如有问题，请参考：
- [README.md](README.md) - 项目概述
- [USAGE.md](USAGE.md) - 使用示例
- [ARCHITECTURE.md](ARCHITECTURE.md) - 架构设计
