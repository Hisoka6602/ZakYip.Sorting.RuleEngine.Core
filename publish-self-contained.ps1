# 自含部署发布脚本 / Self-Contained Deployment Publish Script
# 用于在 Windows 上发布应用程序 / For publishing application on Windows

param(
    [Parameter(HelpMessage="发布配置 / Publish profile")]
    [ValidateSet("linux-x64", "linux-single", "win-x64", "win-single")]
    [string]$Profile = ""
)

# 颜色输出函数 / Color output functions
function Write-ColorOutput($ForegroundColor) {
    $fc = $host.UI.RawUI.ForegroundColor
    $host.UI.RawUI.ForegroundColor = $ForegroundColor
    if ($args) {
        Write-Output $args
    }
    $host.UI.RawUI.ForegroundColor = $fc
}

# 脚本目录 / Script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectDir = Join-Path $ScriptDir "Service\ZakYip.Sorting.RuleEngine.Service"

Write-ColorOutput Green "========================================"
Write-ColorOutput Green "ZakYip 分拣规则引擎 - 自含部署发布"
Write-ColorOutput Green "ZakYip Sorting Rule Engine - Self-Contained Publish"
Write-ColorOutput Green "========================================"
Write-Output ""

# 检查 .NET SDK / Check .NET SDK
try {
    $dotnetVersion = & dotnet --version
    Write-ColorOutput Green "✓ 检测到 .NET SDK 版本: $dotnetVersion"
    Write-Output ""
} catch {
    Write-ColorOutput Red "错误: 未找到 dotnet 命令"
    Write-ColorOutput Red "Error: dotnet command not found"
    Write-ColorOutput Yellow "请安装 .NET 8.0 SDK: https://dotnet.microsoft.com/download"
    Write-ColorOutput Yellow "Please install .NET 8.0 SDK: https://dotnet.microsoft.com/download"
    exit 1
}

# 如果没有指定配置，显示菜单 / If profile not specified, show menu
if ([string]::IsNullOrEmpty($Profile)) {
    Write-ColorOutput Yellow "请选择发布配置 / Please select publish profile:"
    Write-Output "1) Linux x64 (多文件) / Linux x64 (Multiple files)"
    Write-Output "2) Linux x64 (单文件) / Linux x64 (Single file)"
    Write-Output "3) Windows x64 (多文件) / Windows x64 (Multiple files)"
    Write-Output "4) Windows x64 (单文件) / Windows x64 (Single file)"
    Write-Output ""
    
    $choice = Read-Host "请输入选项 (1-4) / Enter option (1-4)"
    
    switch ($choice) {
        "1" { $Profile = "linux-x64" }
        "2" { $Profile = "linux-single" }
        "3" { $Profile = "win-x64" }
        "4" { $Profile = "win-single" }
        default {
            Write-ColorOutput Red "无效的选项 / Invalid option"
            exit 1
        }
    }
}

# 映射配置到发布配置文件 / Map profile to publish profile
$publishProfile = switch ($Profile) {
    "linux-x64"     { "SelfContained-Linux-x64" }
    "linux-single"  { "SelfContained-SingleFile-Linux-x64" }
    "win-x64"       { "SelfContained-Win-x64" }
    "win-single"    { "SelfContained-SingleFile-Win-x64" }
}

# 确定运行时标识符 / Determine runtime identifier
$runtime = if ($Profile -like "linux*") { "linux-x64" } else { "win-x64" }

Write-Output ""
Write-ColorOutput Green "========================================"
Write-ColorOutput Green "开始发布 / Starting publish"
Write-ColorOutput Green "配置文件 / Profile: $publishProfile"
Write-ColorOutput Green "运行时 / Runtime: $runtime"
Write-ColorOutput Green "========================================"
Write-Output ""

# 切换到项目目录 / Change to project directory
Set-Location $ProjectDir

# 清理之前的发布 / Clean previous publish
Write-ColorOutput Yellow "清理之前的发布输出... / Cleaning previous publish output..."
$publishDir = "bin\Release\net8.0\$runtime\publish"
if (Test-Path $publishDir) {
    Remove-Item -Path $publishDir -Recurse -Force
}

# 执行发布 / Execute publish
Write-ColorOutput Yellow "开始发布... / Starting publish..."
& dotnet publish -c Release -p:PublishProfile=$publishProfile -p:RuntimeIdentifier=$runtime

if ($LASTEXITCODE -eq 0) {
    Write-Output ""
    Write-ColorOutput Green "========================================"
    Write-ColorOutput Green "✓ 发布成功 / Publish successful"
    Write-ColorOutput Green "========================================"
    Write-Output ""
    Write-ColorOutput Green "发布输出目录 / Publish output directory:"
    Write-ColorOutput Yellow "$ProjectDir\$publishDir"
    Write-Output ""
    
    # 显示发布文件大小 / Show publish file size
    if (Test-Path $publishDir) {
        Write-ColorOutput Green "发布包大小 / Publish package size:"
        $totalSize = (Get-ChildItem $publishDir -Recurse | Measure-Object -Property Length -Sum).Sum
        if ($totalSize -ne $null -and $totalSize -gt 0) {
            $sizeMB = $totalSize / 1MB
            Write-Output ("{0:N2} MB" -f $sizeMB)
        } else {
            Write-Output "0 MB (empty directory)"
        }
        Write-Output ""
        
        Write-ColorOutput Green "主要文件列表 / Main files list:"
        Get-ChildItem $publishDir | Where-Object { $_.Name -match "ZakYip|\.exe$|\.dll$|appsettings|nlog" } | 
            Select-Object -First 10 | Format-Table Name, @{Name="Size(MB)";Expression={"{0:N2}" -f ($_.Length/1MB)}}
        Write-Output ""
    }
    
    Write-ColorOutput Yellow "提示 / Tips:"
    Write-Output "1. 发布包已包含 .NET 运行时，无需在目标机器上安装 .NET"
    Write-Output "   The publish package includes .NET runtime, no need to install .NET on target machine"
    Write-Output ""
    Write-Output "2. 复制整个 publish 目录到目标机器"
    Write-Output "   Copy the entire publish directory to target machine"
    Write-Output ""
    if ($runtime -eq "win-x64") {
        Write-Output "3. 运行应用程序: ZakYip.Sorting.RuleEngine.Service.exe"
        Write-Output "   Run application: ZakYip.Sorting.RuleEngine.Service.exe"
    } else {
        Write-Output "3. Linux 上需要设置可执行权限: chmod +x ZakYip.Sorting.RuleEngine.Service"
        Write-Output "   On Linux, set executable permission: chmod +x ZakYip.Sorting.RuleEngine.Service"
        Write-Output ""
        Write-Output "4. 运行应用程序: ./ZakYip.Sorting.RuleEngine.Service"
        Write-Output "   Run application: ./ZakYip.Sorting.RuleEngine.Service"
    }
    Write-Output ""
} else {
    Write-Output ""
    Write-ColorOutput Red "========================================"
    Write-ColorOutput Red "✗ 发布失败 / Publish failed"
    Write-ColorOutput Red "========================================"
    exit 1
}
