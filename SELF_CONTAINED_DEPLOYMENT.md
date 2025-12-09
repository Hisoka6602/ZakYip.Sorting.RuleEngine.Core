# 自含部署指南 / Self-Contained Deployment Guide

## 概述 / Overview

本项目已配置为支持自含部署（Self-Contained Deployment），使应用程序在编译时包含 .NET 运行环境，无需在目标设备上安装 .NET SDK 或 Runtime。

This project is configured to support self-contained deployment, allowing the application to include the .NET runtime environment at compile time without requiring .NET SDK or Runtime installation on target devices.

## 什么是自含部署？ / What is Self-Contained Deployment?

自含部署会将 .NET 运行时和所有依赖项打包到应用程序中，使其能够在没有安装 .NET 的机器上运行。

Self-contained deployment packages the .NET runtime and all dependencies into the application, enabling it to run on machines without .NET installed.

### 优点 / Advantages

- ✅ 目标机器无需安装 .NET SDK 或 Runtime
- ✅ 可以控制应用程序使用的 .NET 版本
- ✅ 确保在不同环境中的一致性
- ✅ 避免 .NET 版本冲突
- ✅ 更容易部署到隔离环境

- ✅ Target machine does not need .NET SDK or Runtime installed
- ✅ Can control the .NET version used by the application
- ✅ Ensures consistency across different environments
- ✅ Avoids .NET version conflicts
- ✅ Easier to deploy to isolated environments

### 缺点 / Disadvantages

- ⚠️ 发布包体积较大（约 60-100 MB）
- ⚠️ 每个平台需要单独发布
- ⚠️ 更新 .NET 运行时需要重新发布应用程序

- ⚠️ Larger publish package size (approximately 60-100 MB)
- ⚠️ Each platform requires separate publishing
- ⚠️ Updating .NET runtime requires republishing the application

## 快速开始 / Quick Start

### 使用发布脚本（推荐）/ Using Publish Scripts (Recommended)

#### Windows

```powershell
# 运行发布脚本 / Run publish script
.\publish-self-contained.ps1

# 或指定配置 / Or specify profile
.\publish-self-contained.ps1 -Profile win-x64
.\publish-self-contained.ps1 -Profile win-single
```

#### Linux / macOS

```bash
# 运行发布脚本 / Run publish script
./publish-self-contained.sh

# 脚本会提示选择发布配置 / Script will prompt to select publish profile
```

### 使用 dotnet 命令 / Using dotnet Command

#### 方法 1：使用发布配置文件 / Method 1: Using Publish Profile

```bash
# Windows x64 多文件 / Windows x64 Multiple files
dotnet publish -c Release -p:PublishProfile=SelfContained-Win-x64

# Windows x64 单文件 / Windows x64 Single file
dotnet publish -c Release -p:PublishProfile=SelfContained-SingleFile-Win-x64

# Linux x64 多文件 / Linux x64 Multiple files
dotnet publish -c Release -p:PublishProfile=SelfContained-Linux-x64

# Linux x64 单文件 / Linux x64 Single file
dotnet publish -c Release -p:PublishProfile=SelfContained-SingleFile-Linux-x64
```

#### 方法 2：直接指定参数 / Method 2: Direct Parameters

```bash
# Windows x64
dotnet publish -c Release -r win-x64 --self-contained true

# Linux x64
dotnet publish -c Release -r linux-x64 --self-contained true

# macOS x64
dotnet publish -c Release -r osx-x64 --self-contained true

# Linux ARM64 (树莓派等) / Linux ARM64 (Raspberry Pi, etc.)
dotnet publish -c Release -r linux-arm64 --self-contained true
```

## 发布配置说明 / Publish Configuration Details

项目提供了 4 种预配置的发布配置文件：

The project provides 4 pre-configured publish profiles:

### 1. SelfContained-Win-x64

- **目标平台 / Target Platform**: Windows x64
- **发布方式 / Publish Type**: 多文件 / Multiple files
- **ReadyToRun**: 启用 / Enabled
- **输出目录 / Output Directory**: `bin\Release\net8.0\win-x64\publish\`

**适用场景 / Use Cases**:
- 需要在 Windows 服务器上部署
- 希望保留文件结构便于调试
- 需要修改配置文件（appsettings.json, nlog.config）

### 2. SelfContained-Linux-x64

- **目标平台 / Target Platform**: Linux x64
- **发布方式 / Publish Type**: 多文件 / Multiple files
- **ReadyToRun**: 启用 / Enabled
- **输出目录 / Output Directory**: `bin/Release/net8.0/linux-x64/publish/`

**适用场景 / Use Cases**:
- 需要在 Linux 服务器上部署（Ubuntu, CentOS, Debian 等）
- Docker 容器部署
- 云服务器部署

### 3. SelfContained-SingleFile-Win-x64

- **目标平台 / Target Platform**: Windows x64
- **发布方式 / Publish Type**: 单文件 / Single file
- **压缩 / Compression**: 启用 / Enabled
- **输出目录 / Output Directory**: `bin\Release\net8.0\win-x64\publish\`

**适用场景 / Use Cases**:
- 简化分发和部署
- 便于版本管理
- 客户端环境部署

**注意 / Note**: 配置文件（appsettings.json, nlog.config）仍会作为单独文件输出

### 4. SelfContained-SingleFile-Linux-x64

- **目标平台 / Target Platform**: Linux x64
- **发布方式 / Publish Type**: 单文件 / Single file
- **压缩 / Compression**: 启用 / Enabled
- **输出目录 / Output Directory**: `bin/Release/net8.0/linux-x64/publish/`

**适用场景 / Use Cases**:
- 简化 Linux 部署
- 便于自动化部署脚本
- 容器化部署

## 部署步骤 / Deployment Steps

### Windows 部署 / Windows Deployment

1. **发布应用程序 / Publish Application**
   ```powershell
   .\publish-self-contained.ps1 -Profile win-x64
   ```

2. **复制发布目录 / Copy Publish Directory**
   ```
   复制整个 publish 文件夹到目标服务器
   Copy the entire publish folder to target server
   ```

3. **配置应用程序 / Configure Application**
   ```
   编辑 appsettings.json 和 nlog.config
   Edit appsettings.json and nlog.config
   ```

4. **运行应用程序 / Run Application**

   **方式 1: 直接运行（用于测试）/ Direct Run (for testing)**
   ```powershell
   .\ZakYip.Sorting.RuleEngine.Service.exe
   ```

   **方式 2: 安装为 Windows 服务（推荐用于生产）/ Install as Windows Service (Recommended for Production)**
   
   项目已配置 Windows 服务支持，可以作为 Windows 服务运行。
   
   The project is configured with Windows Service support and can run as a Windows Service.

   **安装服务 / Install Service:**
   ```powershell
   # 使用管理员权限打开 PowerShell
   # Open PowerShell as Administrator
   
   # 方法 1: 使用 sc 命令（推荐）
   # Method 1: Using sc command (Recommended)
   sc create "ZakYipSortingService" `
     binPath="C:\ZakYip\ZakYip.Sorting.RuleEngine.Service.exe" `
     DisplayName="ZakYip Sorting Rule Engine Service" `
     Description="高性能包裹分拣规则引擎服务 / High-performance parcel sorting rule engine service" `
     start=auto
   
   # 方法 2: 使用 New-Service cmdlet
   # Method 2: Using New-Service cmdlet
   New-Service -Name "ZakYipSortingService" `
     -BinaryPathName "C:\ZakYip\ZakYip.Sorting.RuleEngine.Service.exe" `
     -DisplayName "ZakYip Sorting Rule Engine Service" `
     -Description "高性能包裹分拣规则引擎服务 / High-performance parcel sorting rule engine service" `
     -StartupType Automatic
   ```

   **配置服务恢复选项 / Configure Service Recovery Options:**
   ```powershell
   # 配置服务失败后自动重启
   # Configure automatic restart on failure
   sc failure "ZakYipSortingService" reset=86400 actions=restart/60000/restart/60000/restart/60000
   ```

   **启动服务 / Start Service:**
   ```powershell
   # 启动服务
   # Start service
   sc start ZakYipSortingService
   # 或 / Or
   Start-Service -Name "ZakYipSortingService"
   
   # 检查服务状态
   # Check service status
   sc query ZakYipSortingService
   # 或 / Or
   Get-Service -Name "ZakYipSortingService"
   ```

   **停止服务 / Stop Service:**
   ```powershell
   sc stop ZakYipSortingService
   # 或 / Or
   Stop-Service -Name "ZakYipSortingService"
   ```

   **卸载服务 / Uninstall Service:**
   ```powershell
   # 先停止服务
   # Stop service first
   sc stop ZakYipSortingService
   
   # 删除服务
   # Delete service
   sc delete ZakYipSortingService
   # 或 / Or
   Remove-Service -Name "ZakYipSortingService"
   ```

   **查看服务日志 / View Service Logs:**
   ```powershell
   # 方法 1: 使用事件查看器
   # Method 1: Using Event Viewer
   eventvwr.msc
   # 导航到: Windows 日志 -> 应用程序
   # Navigate to: Windows Logs -> Application
   
   # 方法 2: 使用 PowerShell
   # Method 2: Using PowerShell
   Get-EventLog -LogName Application -Source "ZakYipSortingService" -Newest 50
   
   # 方法 3: 查看 NLog 日志文件
   # Method 3: View NLog log files
   Get-Content -Path "C:\ZakYip\logs\*.log" -Tail 100
   ```

   **服务配置文件位置 / Service Configuration File Location:**
   - 配置文件与可执行文件在同一目录
   - Configuration files are in the same directory as the executable
   - `appsettings.json` - 应用配置 / Application configuration
   - `nlog.config` - 日志配置 / Logging configuration
   - `logs\` - 日志输出目录 / Log output directory

   **注意事项 / Important Notes:**
   - ✅ 服务会以 LocalSystem 账户运行，具有完整系统权限
   - ✅ Service runs as LocalSystem account with full system privileges
   - ⚠️ 建议创建专用服务账户以限制权限
   - ⚠️ Recommended to create a dedicated service account to limit privileges
   - ✅ 服务配置为自动启动（系统重启后自动运行）
   - ✅ Service is configured to start automatically (runs after system reboot)
   - ✅ 配置了失败自动重启（3 次重试，每次间隔 60 秒）
   - ✅ Configured for automatic restart on failure (3 retries, 60 seconds interval)

### Linux 部署 / Linux Deployment

1. **发布应用程序 / Publish Application**
   ```bash
   ./publish-self-contained.sh
   # 选择选项 1 或 2 / Select option 1 or 2
   ```

2. **复制到目标服务器 / Copy to Target Server**
   ```bash
   scp -r bin/Release/net8.0/linux-x64/publish user@server:/opt/zakyip-sorting/
   ```

3. **设置权限 / Set Permissions**
   ```bash
   chmod +x /opt/zakyip-sorting/ZakYip.Sorting.RuleEngine.Service
   ```

4. **配置应用程序 / Configure Application**
   ```bash
   nano /opt/zakyip-sorting/appsettings.json
   nano /opt/zakyip-sorting/nlog.config
   ```

5. **运行应用程序 / Run Application**
   ```bash
   # 直接运行 / Direct run
   /opt/zakyip-sorting/ZakYip.Sorting.RuleEngine.Service
   
   # 或使用 systemd 服务 / Or use systemd service
   ```

### 创建 systemd 服务（Linux）/ Create systemd Service (Linux)

创建服务文件 / Create service file: `/etc/systemd/system/zakyip-sorting.service`

```ini
[Unit]
Description=ZakYip Sorting Rule Engine Service
After=network.target

[Service]
Type=notify
User=zakyip
WorkingDirectory=/opt/zakyip-sorting
ExecStart=/opt/zakyip-sorting/ZakYip.Sorting.RuleEngine.Service
Restart=on-failure
RestartSec=5s
KillSignal=SIGINT
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

启用和启动服务 / Enable and start service:

```bash
sudo systemctl daemon-reload
sudo systemctl enable zakyip-sorting.service
sudo systemctl start zakyip-sorting.service
sudo systemctl status zakyip-sorting.service
```

## Docker 部署 / Docker Deployment

虽然项目配置了自含部署，但在 Docker 中建议使用框架依赖部署（Framework-Dependent）以减小镜像大小。

Although the project is configured for self-contained deployment, framework-dependent deployment is recommended in Docker to reduce image size.

### Dockerfile 示例 / Dockerfile Example

```dockerfile
# 使用官方 .NET 8 运行时镜像（体积更小）
# Use official .NET 8 runtime image (smaller size)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# 构建阶段 / Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Service/ZakYip.Sorting.RuleEngine.Service/ZakYip.Sorting.RuleEngine.Service.csproj", "Service/ZakYip.Sorting.RuleEngine.Service/"]
COPY ["Application/ZakYip.Sorting.RuleEngine.Application/ZakYip.Sorting.RuleEngine.Application.csproj", "Application/ZakYip.Sorting.RuleEngine.Application/"]
COPY ["Domain/ZakYip.Sorting.RuleEngine.Domain/ZakYip.Sorting.RuleEngine.Domain.csproj", "Domain/ZakYip.Sorting.RuleEngine.Domain/"]
COPY ["Infrastructure/ZakYip.Sorting.RuleEngine.Infrastructure/ZakYip.Sorting.RuleEngine.Infrastructure.csproj", "Infrastructure/ZakYip.Sorting.RuleEngine.Infrastructure/"]
RUN dotnet restore "Service/ZakYip.Sorting.RuleEngine.Service/ZakYip.Sorting.RuleEngine.Service.csproj"
COPY . .
WORKDIR "/src/Service/ZakYip.Sorting.RuleEngine.Service"
RUN dotnet build "ZakYip.Sorting.RuleEngine.Service.csproj" -c Release -o /app/build

# 发布阶段 / Publish stage
FROM build AS publish
RUN dotnet publish "ZakYip.Sorting.RuleEngine.Service.csproj" -c Release -o /app/publish --no-self-contained

# 运行阶段 / Runtime stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ZakYip.Sorting.RuleEngine.Service.dll"]
```

## 配置选项说明 / Configuration Options

项目的自含部署配置在 `Service.csproj` 中定义：

The self-contained deployment configuration is defined in `Service.csproj`:

### SelfContained
```xml
<SelfContained>true</SelfContained>
```
启用自含部署，包含 .NET 运行时 / Enable self-contained deployment, includes .NET runtime

### PublishReadyToRun
```xml
<PublishReadyToRun>true</PublishReadyToRun>
```
启用 ReadyToRun 编译，将 IL 代码预编译为本机代码，提高启动性能 / Enable ReadyToRun compilation, precompile IL to native code, improve startup performance

### PublishSingleFile
```xml
<PublishSingleFile>false</PublishSingleFile>
```
是否打包为单个可执行文件。默认 false，保留文件结构 / Whether to package as a single executable file. Default false, preserve file structure

### PublishTrimmed
```xml
<PublishTrimmed>false</PublishTrimmed>
```
是否启用程序集裁剪。默认 false，因为裁剪可能导致反射相关功能失效 / Whether to enable assembly trimming. Default false, as trimming may break reflection-related features

### EnableCompressionInSingleFile
```xml
<EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
```
单文件发布时启用压缩，减小文件大小 / Enable compression for single file publish, reduce file size

## 常见问题 / FAQ

### Q1: 发布包为什么这么大？ / Why is the publish package so large?

A: 自含部署包含了完整的 .NET 运行时（约 60 MB）。如果对大小敏感，可以：
- 使用框架依赖部署（需要目标机器安装 .NET）
- 启用程序集裁剪（可能影响功能）
- 使用单文件发布并启用压缩

A: Self-contained deployment includes the complete .NET runtime (approximately 60 MB). If size-sensitive, you can:
- Use framework-dependent deployment (requires .NET on target machine)
- Enable assembly trimming (may affect functionality)
- Use single file publish with compression enabled

### Q2: 如何更新应用程序？ / How to update the application?

A: 停止应用程序，替换发布目录中的文件，然后重新启动。建议保留配置文件（appsettings.json, nlog.config）。

A: Stop the application, replace files in the publish directory, then restart. Recommend keeping configuration files (appsettings.json, nlog.config).

### Q3: 支持哪些平台？ / What platforms are supported?

A: 项目支持所有 .NET 8 支持的平台，包括：
- Windows (x64, x86, ARM64)
- Linux (x64, ARM64, ARM32)
- macOS (x64, ARM64)

A: The project supports all platforms supported by .NET 8, including:
- Windows (x64, x86, ARM64)
- Linux (x64, ARM64, ARM32)
- macOS (x64, ARM64)

要为其他平台发布，只需更改运行时标识符 / To publish for other platforms, just change the runtime identifier:
```bash
dotnet publish -c Release -r linux-arm64 --self-contained true
dotnet publish -c Release -r osx-arm64 --self-contained true
```

### Q4: ReadyToRun 是什么？ / What is ReadyToRun?

A: ReadyToRun (R2R) 是一种编译技术，将 IL 代码预编译为本机代码，提高应用程序启动速度。启用 R2R 会增加发布包大小，但显著提高首次启动性能。

A: ReadyToRun (R2R) is a compilation technique that precompiles IL code to native code, improving application startup speed. Enabling R2R increases publish package size but significantly improves first-start performance.

### Q5: 单文件发布和多文件发布的区别？ / What's the difference between single file and multiple files publish?

A: 
**多文件发布 / Multiple Files Publish**:
- 所有 DLL 和可执行文件分开存放
- 便于调试和更新单个组件
- 配置文件可直接修改

**单文件发布 / Single File Publish**:
- 大部分文件打包为单个可执行文件
- 简化部署和分发
- 首次启动时会解压到临时目录
- 配置文件仍作为独立文件（便于修改）

A:
**Multiple Files Publish**:
- All DLLs and executables stored separately
- Easy to debug and update individual components
- Configuration files can be directly modified

**Single File Publish**:
- Most files packaged into a single executable
- Simplifies deployment and distribution
- Extracts to temporary directory on first start
- Configuration files remain as separate files (for easy modification)

### Q6: 如何验证发布包是否包含运行时？ / How to verify the publish package includes the runtime?

A: 检查发布目录中是否存在以下文件 / Check if the following files exist in the publish directory:
- `System.*.dll` - .NET 基础类库
- `Microsoft.*.dll` - .NET 框架库
- `hostfxr.dll` / `hostfxr.so` - .NET 主机库
- 以及平台特定的本机库文件

发布包大小通常为 60-100 MB，而框架依赖部署通常只有 1-5 MB。

The publish package size is typically 60-100 MB, while framework-dependent deployment is usually only 1-5 MB.

### Q7: 如何以 Windows 服务方式运行？ / How to run as a Windows Service?

A: 项目已内置 Windows 服务支持（`Microsoft.Extensions.Hosting.WindowsServices`），可直接安装为 Windows 服务。

A: The project has built-in Windows Service support (`Microsoft.Extensions.Hosting.WindowsServices`) and can be directly installed as a Windows Service.

**快速安装 / Quick Install:**
```powershell
# 以管理员身份运行 PowerShell / Run PowerShell as Administrator
sc create "ZakYipSortingService" `
  binPath="C:\ZakYip\ZakYip.Sorting.RuleEngine.Service.exe" `
  start=auto

# 启动服务 / Start service
sc start ZakYipSortingService
```

**服务特性 / Service Features:**
- ✅ 支持自动启动（系统重启后自动运行）
- ✅ Supports automatic startup (runs after system reboot)
- ✅ 支持服务控制命令（Start、Stop、Restart）
- ✅ Supports service control commands (Start, Stop, Restart)
- ✅ 支持优雅关闭（Graceful Shutdown）
- ✅ Supports graceful shutdown
- ✅ 集成 Windows 事件日志
- ✅ Integrated with Windows Event Log
- ✅ 支持服务恢复选项（失败自动重启）
- ✅ Supports service recovery options (automatic restart on failure)

**服务管理命令 / Service Management Commands:**
```powershell
# 查看服务状态 / Check service status
Get-Service -Name "ZakYipSortingService"

# 停止服务 / Stop service
Stop-Service -Name "ZakYipSortingService"

# 重启服务 / Restart service
Restart-Service -Name "ZakYipSortingService"

# 卸载服务 / Uninstall service
sc delete ZakYipSortingService
```

**配置服务账户 / Configure Service Account:**
```powershell
# 创建专用服务账户（推荐）
# Create dedicated service account (recommended)
sc config ZakYipSortingService obj="NT AUTHORITY\NETWORK SERVICE"

# 或使用域账户 / Or use domain account
sc config ZakYipSortingService obj="DOMAIN\ServiceAccount" password="Password123"
```

**故障排查 / Troubleshooting:**
- 服务启动失败：检查事件查看器（Application 日志）
- Service fails to start: Check Event Viewer (Application log)
- 权限问题：确保服务账户有访问应用目录和配置文件的权限
- Permission issues: Ensure service account has access to app directory and config files
- 日志查看：检查 `logs\` 目录下的 NLog 日志文件
- View logs: Check NLog log files in `logs\` directory

详细配置请参见"Windows 部署"章节。

For detailed configuration, see the "Windows Deployment" section.

## 性能考虑 / Performance Considerations

### 启动性能 / Startup Performance

- **ReadyToRun**: 启用后首次启动速度提高约 30-40%
- **Single File**: 首次启动会解压文件，略微增加启动时间（1-2秒）
- **建议 / Recommendation**: 生产环境启用 ReadyToRun，使用多文件发布

- **ReadyToRun**: Improves first startup speed by approximately 30-40% when enabled
- **Single File**: First startup extracts files, slightly increases startup time (1-2 seconds)
- **Recommendation**: Enable ReadyToRun in production, use multiple files publish

### 内存使用 / Memory Usage

自含部署和框架依赖部署的内存使用基本相同。

Self-contained and framework-dependent deployments have similar memory usage.

### 磁盘空间 / Disk Space

- 多文件发布：约 80-120 MB
- 单文件发布（压缩）：约 60-80 MB
- 框架依赖部署：约 1-5 MB

- Multiple files publish: approximately 80-120 MB
- Single file publish (compressed): approximately 60-80 MB
- Framework-dependent deployment: approximately 1-5 MB

## 安全性建议 / Security Recommendations

1. **定期更新 / Regular Updates**: 自含部署不会自动更新 .NET 运行时，需要定期重新发布以获取安全更新
2. **最小权限 / Minimum Privileges**: 应用程序应以最小权限运行
3. **文件权限 / File Permissions**: Linux 上确保可执行文件权限设置正确
4. **网络隔离 / Network Isolation**: 考虑使用防火墙限制应用程序的网络访问

1. **Regular Updates**: Self-contained deployment does not automatically update .NET runtime, need to republish regularly for security updates
2. **Minimum Privileges**: Application should run with minimum privileges
3. **File Permissions**: Ensure executable file permissions are set correctly on Linux
4. **Network Isolation**: Consider using firewall to restrict application network access

## 参考资料 / References

- [.NET Application Publishing Overview](https://docs.microsoft.com/en-us/dotnet/core/deploying/)
- [Self-Contained Deployment](https://docs.microsoft.com/en-us/dotnet/core/deploying/#publish-self-contained)
- [ReadyToRun Compilation](https://docs.microsoft.com/en-us/dotnet/core/deploying/ready-to-run)
- [Single File Deployment](https://docs.microsoft.com/en-us/dotnet/core/deploying/single-file)

---

**最后更新 / Last Updated**: 2025-12-09  
**版本 / Version**: 1.0
