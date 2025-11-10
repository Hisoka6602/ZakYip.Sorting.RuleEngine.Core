using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Services
{
    /// <summary>
    /// Windows防火墙和网络管理服务
    /// Manages Windows Firewall settings, port rules, and network adapter configuration
    /// </summary>
    public class WindowsFirewallManager
    {
        private readonly ILogger<WindowsFirewallManager> _logger;
        private readonly SafetyIsolator _safetyIsolator;

        public WindowsFirewallManager(ILogger<WindowsFirewallManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _safetyIsolator = new SafetyIsolator(logger);
        }

        /// <summary>
        /// 检查并配置Windows防火墙，确保所需端口已开放
        /// Check and configure Windows Firewall to ensure required ports are open
        /// </summary>
        /// <param name="ports">需要开放的端口列表</param>
        /// <returns>是否成功配置</returns>
        public bool EnsureFirewallConfigured(IEnumerable<int> ports)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _logger.LogInformation("非Windows平台，跳过防火墙配置 | Not a Windows platform, skipping firewall configuration");
                return true;
            }

            try
            {
                _logger.LogInformation("开始检查Windows防火墙配置 | Starting Windows Firewall configuration check");

                // 检查是否以管理员权限运行
                if (!IsRunningAsAdministrator())
                {
                    _logger.LogWarning("程序未以管理员权限运行，无法配置防火墙。建议以管理员身份运行程序。| Program is not running with administrator privileges. Cannot configure firewall. Please run as administrator.");
                    return false;
                }

                // 检查防火墙状态
                var firewallEnabled = IsFirewallEnabled();
                if (firewallEnabled)
                {
                    _logger.LogWarning("Windows防火墙已启用 | Windows Firewall is enabled");
                    
                    // 尝试关闭防火墙
                    var disabled = DisableFirewall();
                    if (disabled)
                    {
                        _logger.LogInformation("已成功关闭Windows防火墙 | Successfully disabled Windows Firewall");
                    }
                    else
                    {
                        _logger.LogWarning("无法关闭Windows防火墙，将尝试添加端口规则 | Cannot disable Windows Firewall, will try to add port rules");
                    }
                }
                else
                {
                    _logger.LogInformation("Windows防火墙未启用 | Windows Firewall is not enabled");
                }

                // 确保所需端口的防火墙规则存在
                foreach (var port in ports)
                {
                    EnsurePortRuleExists(port);
                }

                _logger.LogInformation("防火墙配置检查完成 | Firewall configuration check completed");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "配置Windows防火墙时发生错误 | Error occurred while configuring Windows Firewall");
                return false;
            }
        }

        /// <summary>
        /// 检查是否以管理员权限运行
        /// Check if running with administrator privileges
        /// </summary>
        private bool IsRunningAsAdministrator()
        {
            try
            {
                using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "检查管理员权限时发生错误 | Error checking administrator privileges");
                return false;
            }
        }

        /// <summary>
        /// 检查Windows防火墙是否启用
        /// Check if Windows Firewall is enabled
        /// </summary>
        private bool IsFirewallEnabled()
        {
            return _safetyIsolator.Execute(() =>
            {
                var result = ExecuteNetshCommand("advfirewall show allprofiles state");
                // 检查输出中是否包含 "ON"
                return result.Contains("ON", StringComparison.OrdinalIgnoreCase);
            }, "检查防火墙状态 | Check firewall status", false);
        }

        /// <summary>
        /// 关闭Windows防火墙
        /// Disable Windows Firewall
        /// </summary>
        private bool DisableFirewall()
        {
            return _safetyIsolator.Execute(() =>
            {
                _logger.LogInformation("尝试关闭Windows防火墙所有配置文件 | Attempting to disable Windows Firewall for all profiles");
                
                // 关闭所有配置文件的防火墙（域、专用、公用）
                var result = ExecuteNetshCommand("advfirewall set allprofiles state off");
                
                if (result.Contains("Ok", StringComparison.OrdinalIgnoreCase) || 
                    result.Contains("确定", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("成功关闭Windows防火墙 | Successfully disabled Windows Firewall");
                    return true;
                }
                else
                {
                    _logger.LogWarning("关闭防火墙命令执行，但结果未知: {Result} | Firewall disable command executed, but result unknown: {Result}", result);
                    return false;
                }
            }, "关闭防火墙 | Disable firewall", false);
        }

        /// <summary>
        /// 确保指定端口的防火墙规则存在
        /// Ensure firewall rule exists for the specified port
        /// </summary>
        private void EnsurePortRuleExists(int port)
        {
            _safetyIsolator.Execute(() =>
            {
                var ruleName = $"ZakYip.Sorting.RuleEngine.Port{port}";
                
                // 检查规则是否已存在
                if (CheckRuleExists(ruleName))
                {
                    _logger.LogInformation("端口 {Port} 的防火墙规则已存在 | Firewall rule for port {Port} already exists", port);
                    return;
                }

                _logger.LogInformation("为端口 {Port} 添加防火墙规则 | Adding firewall rule for port {Port}", port);

                // 添加入站规则
                var inboundResult = ExecuteNetshCommand(
                    $"advfirewall firewall add rule name=\"{ruleName}_Inbound\" " +
                    $"dir=in action=allow protocol=TCP localport={port}");

                if (inboundResult.Contains("Ok", StringComparison.OrdinalIgnoreCase) || 
                    inboundResult.Contains("确定", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("成功添加端口 {Port} 的入站规则 | Successfully added inbound rule for port {Port}", port);
                }
                else
                {
                    _logger.LogWarning("添加端口 {Port} 入站规则失败: {Result} | Failed to add inbound rule for port {Port}: {Result}", port, inboundResult);
                }

                // 添加出站规则
                var outboundResult = ExecuteNetshCommand(
                    $"advfirewall firewall add rule name=\"{ruleName}_Outbound\" " +
                    $"dir=out action=allow protocol=TCP localport={port}");

                if (outboundResult.Contains("Ok", StringComparison.OrdinalIgnoreCase) || 
                    outboundResult.Contains("确定", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("成功添加端口 {Port} 的出站规则 | Successfully added outbound rule for port {Port}", port);
                }
                else
                {
                    _logger.LogWarning("添加端口 {Port} 出站规则失败: {Result} | Failed to add outbound rule for port {Port}: {Result}", port, outboundResult);
                }
            }, $"添加端口{port}的防火墙规则 | Add firewall rule for port {port}");
        }

        /// <summary>
        /// 检查防火墙规则是否存在
        /// Check if firewall rule exists
        /// </summary>
        private bool CheckRuleExists(string ruleName)
        {
            return _safetyIsolator.Execute(() =>
            {
                var result = ExecuteNetshCommand("advfirewall firewall show rule name=all");
                return result.Contains($"{ruleName}_Inbound", StringComparison.OrdinalIgnoreCase) ||
                       result.Contains($"{ruleName}_Outbound", StringComparison.OrdinalIgnoreCase);
            }, $"检查防火墙规则 {ruleName} | Check firewall rule {ruleName}", false);
        }

        /// <summary>
        /// 执行netsh命令
        /// Execute netsh command
        /// </summary>
        private string ExecuteNetshCommand(string arguments)
        {
            return _safetyIsolator.Execute(() =>
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processStartInfo);
                if (process == null)
                {
                    throw new InvalidOperationException("无法启动netsh进程 | Failed to start netsh process");
                }

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(error))
                {
                    _logger.LogWarning("Netsh命令执行警告: {Error} | Netsh command execution warning: {Error}", error);
                }

                return output;
            }, $"执行netsh命令: {arguments} | Execute netsh command: {arguments}", string.Empty);
        }

        /// <summary>
        /// 从URL列表中提取端口
        /// Extract ports from URL list
        /// </summary>
        public static IEnumerable<int> ExtractPortsFromUrls(string[] urls)
        {
            var ports = new HashSet<int>();

            if (urls == null || urls.Length == 0)
            {
                return ports;
            }

            foreach (var url in urls)
            {
                try
                {
                    if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                    {
                        // 如果URI中明确指定了端口，使用该端口
                        if (uri.Port > 0 && uri.Port != 80 && uri.Port != 443)
                        {
                            ports.Add(uri.Port);
                        }
                        // 否则使用默认端口
                        else if (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase))
                        {
                            ports.Add(80);
                        }
                        else if (uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                        {
                            ports.Add(443);
                        }
                    }
                }
                catch
                {
                    // 忽略无效URL
                }
            }

            return ports;
        }

        /// <summary>
        /// 配置所有网卡启用巨帧并设置传输缓存到最大值
        /// Configure all network adapters to enable Jumbo Frames and set transmit buffers to maximum
        /// </summary>
        public bool ConfigureNetworkAdapters()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _logger.LogInformation("非Windows平台，跳过网卡配置 | Not a Windows platform, skipping network adapter configuration");
                return true;
            }

            try
            {
                _logger.LogInformation("开始配置网络适配器 | Starting network adapter configuration");

                // 检查是否以管理员权限运行
                if (!IsRunningAsAdministrator())
                {
                    _logger.LogWarning("程序未以管理员权限运行，无法配置网络适配器。建议以管理员身份运行程序。| Program is not running with administrator privileges. Cannot configure network adapters. Please run as administrator.");
                    return false;
                }

                // 获取所有物理网络适配器
                var adapters = GetPhysicalNetworkAdapters();
                if (adapters.Count == 0)
                {
                    _logger.LogWarning("未找到物理网络适配器 | No physical network adapters found");
                    return false;
                }

                _logger.LogInformation("找到 {Count} 个物理网络适配器 | Found {Count} physical network adapters", adapters.Count);

                var successCount = 0;
                foreach (var adapter in adapters)
                {
                    _logger.LogInformation("配置网络适配器: {Adapter} | Configuring network adapter: {Adapter}", adapter);
                    
                    // 启用巨帧 (Jumbo Frames)
                    if (EnableJumboFrames(adapter))
                    {
                        _logger.LogInformation("成功为适配器 {Adapter} 启用巨帧 | Successfully enabled Jumbo Frames for adapter {Adapter}", adapter);
                    }
                    
                    // 设置传输缓存到最大值
                    if (SetMaxTransmitBuffers(adapter))
                    {
                        _logger.LogInformation("成功为适配器 {Adapter} 设置最大传输缓存 | Successfully set maximum transmit buffers for adapter {Adapter}", adapter);
                    }
                    
                    // 关闭网卡节能功能
                    if (DisablePowerSaving(adapter))
                    {
                        _logger.LogInformation("成功为适配器 {Adapter} 关闭节能功能 | Successfully disabled power saving for adapter {Adapter}", adapter);
                        successCount++;
                    }
                }

                _logger.LogInformation("网络适配器配置完成，成功配置 {Success}/{Total} 个适配器 | Network adapter configuration completed, successfully configured {Success}/{Total} adapters", successCount, adapters.Count);
                return successCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "配置网络适配器时发生错误 | Error occurred while configuring network adapters");
                return false;
            }
        }

        /// <summary>
        /// 获取所有物理网络适配器的名称
        /// Get names of all physical network adapters
        /// </summary>
        private List<string> GetPhysicalNetworkAdapters()
        {
            var adapters = new List<string>();
            try
            {
                // 使用PowerShell获取物理网络适配器
                var result = ExecutePowerShellCommand(
                    "Get-NetAdapter | Where-Object {$_.Status -eq 'Up' -and $_.Virtual -eq $false} | Select-Object -ExpandProperty Name");

                if (!string.IsNullOrWhiteSpace(result))
                {
                    var lines = result.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        var trimmed = line.Trim();
                        if (!string.IsNullOrWhiteSpace(trimmed) && !trimmed.Contains("Name") && !trimmed.Contains("---"))
                        {
                            adapters.Add(trimmed);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取网络适配器列表时发生错误 | Error getting network adapter list");
            }

            return adapters;
        }

        /// <summary>
        /// 为网络适配器启用巨帧
        /// Enable Jumbo Frames for network adapter
        /// </summary>
        private bool EnableJumboFrames(string adapterName)
        {
            try
            {
                // 巨帧的典型值是9014字节（9KB）
                // 使用PowerShell设置巨帧，不同网卡驱动可能使用不同的属性名
                var properties = new[]
                {
                    "*JumboPacket",      // 常见属性名
                    "JumboFrame",        // 另一个常见属性名
                    "MTU"                // MTU设置
                };

                var success = false;
                foreach (var property in properties)
                {
                    try
                    {
                        // 尝试设置到9014字节
                        var command = $"Set-NetAdapterAdvancedProperty -Name '{adapterName}' -DisplayName '{property}' -DisplayValue '9014' -ErrorAction SilentlyContinue";
                        ExecutePowerShellCommand(command);
                        
                        // 也尝试直接设置注册表值
                        command = $"Set-NetAdapterAdvancedProperty -Name '{adapterName}' -RegistryKeyword '{property}' -RegistryValue '9014' -ErrorAction SilentlyContinue";
                        ExecutePowerShellCommand(command);
                        
                        success = true;
                    }
                    catch
                    {
                        // 继续尝试下一个属性
                    }
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "为适配器 {Adapter} 启用巨帧时发生错误 | Error enabling Jumbo Frames for adapter {Adapter}", adapterName);
                return false;
            }
        }

        /// <summary>
        /// 设置网络适配器传输缓存到最大值
        /// Set network adapter transmit buffers to maximum
        /// </summary>
        private bool SetMaxTransmitBuffers(string adapterName)
        {
            try
            {
                // 获取当前适配器的高级属性
                var getCommand = $"Get-NetAdapterAdvancedProperty -Name '{adapterName}' | Where-Object {{$_.DisplayName -like '*Transmit*' -or $_.DisplayName -like '*Send*' -or $_.DisplayName -like '*Buffer*'}}";
                var properties = ExecutePowerShellCommand(getCommand);

                // 常见的传输缓存相关属性
                var bufferProperties = new[]
                {
                    "*TransmitBuffers",
                    "TransmitBuffers",
                    "NumTxBuffers",
                    "TxBuffers",
                    "SendBuffers"
                };

                var success = false;
                foreach (var property in bufferProperties)
                {
                    try
                    {
                        // 首先获取该属性的有效范围
                        var rangeCommand = $"Get-NetAdapterAdvancedProperty -Name '{adapterName}' -RegistryKeyword '{property}' -ErrorAction SilentlyContinue | Select-Object -ExpandProperty ValidDisplayValues";
                        var validValues = ExecutePowerShellCommand(rangeCommand);

                        // 尝试设置到大值（常见的最大值范围）
                        var maxValues = new[] { "4096", "2048", "1024", "512", "256" };
                        
                        foreach (var maxValue in maxValues)
                        {
                            try
                            {
                                var setCommand = $"Set-NetAdapterAdvancedProperty -Name '{adapterName}' -RegistryKeyword '{property}' -RegistryValue '{maxValue}' -ErrorAction SilentlyContinue";
                                ExecutePowerShellCommand(setCommand);
                                
                                _logger.LogInformation("为适配器 {Adapter} 设置 {Property} = {Value} | Set {Property} = {Value} for adapter {Adapter}", 
                                    adapterName, property, maxValue);
                                success = true;
                                break; // 设置成功则跳出
                            }
                            catch
                            {
                                // 尝试下一个值
                                continue;
                            }
                        }
                    }
                    catch
                    {
                        // 继续尝试下一个属性
                    }
                }

                // 尝试设置接收缓存
                var recvBufferProperties = new[]
                {
                    "*ReceiveBuffers",
                    "ReceiveBuffers",
                    "NumRxBuffers",
                    "RxBuffers",
                    "RecvBuffers"
                };

                foreach (var property in recvBufferProperties)
                {
                    try
                    {
                        var maxValues = new[] { "4096", "2048", "1024", "512", "256" };
                        
                        foreach (var maxValue in maxValues)
                        {
                            try
                            {
                                var setCommand = $"Set-NetAdapterAdvancedProperty -Name '{adapterName}' -RegistryKeyword '{property}' -RegistryValue '{maxValue}' -ErrorAction SilentlyContinue";
                                ExecutePowerShellCommand(setCommand);
                                
                                _logger.LogInformation("为适配器 {Adapter} 设置 {Property} = {Value} | Set {Property} = {Value} for adapter {Adapter}", 
                                    adapterName, property, maxValue);
                                success = true;
                                break;
                            }
                            catch
                            {
                                continue;
                            }
                        }
                    }
                    catch
                    {
                        // 继续尝试下一个属性
                    }
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "为适配器 {Adapter} 设置最大传输缓存时发生错误 | Error setting maximum transmit buffers for adapter {Adapter}", adapterName);
                return false;
            }
        }

        /// <summary>
        /// 关闭网卡节能功能
        /// Disable power saving features for network adapter
        /// </summary>
        private bool DisablePowerSaving(string adapterName)
        {
            try
            {
                var success = false;

                // 1. 关闭"允许计算机关闭此设备以节约电源"
                // Disable "Allow the computer to turn off this device to save power"
                var disablePowerMgmt = _safetyIsolator.ExecuteSilent(() =>
                {
                    var command = $"Get-NetAdapter -Name '{adapterName}' | Get-NetAdapterPowerManagement | Set-NetAdapterPowerManagement -AllowComputerToTurnOffDevice Disabled -ErrorAction SilentlyContinue";
                    ExecutePowerShellCommand(command);
                });

                if (disablePowerMgmt)
                {
                    _logger.LogInformation("已为适配器 {Adapter} 禁用计算机关闭设备选项 | Disabled computer turn off device option for adapter {Adapter}", adapterName);
                    success = true;
                }

                // 2. 禁用网卡的各种节能选项
                // Disable various power saving options for the adapter
                var powerSavingProperties = new[]
                {
                    "*EEE",                          // Energy Efficient Ethernet
                    "EEE",
                    "GreenEthernet",                 // Green Ethernet
                    "*GreenEthernet",
                    "PowerSavingMode",               // Power Saving Mode
                    "*PowerSavingMode",
                    "ReduceSpeedOnPowerDown",        // Reduce Speed On Power Down
                    "*AdvancedEEE",                  // Advanced EEE
                    "UltraLowPowerMode",             // Ultra Low Power Mode
                    "*UltraLowPowerMode",
                    "EnablePME",                     // Enable PME (Power Management Event)
                    "*PME",
                    "WakeOnMagicPacket",             // Wake on Magic Packet (可选择性禁用)
                    "*WakeOnMagicPacket",
                    "WakeOnPattern",                 // Wake on Pattern
                    "*WakeOnPattern"
                };

                foreach (var property in powerSavingProperties)
                {
                    _safetyIsolator.ExecuteSilent(() =>
                    {
                        // 尝试设置为 0 (禁用)
                        var command = $"Set-NetAdapterAdvancedProperty -Name '{adapterName}' -RegistryKeyword '{property}' -RegistryValue '0' -ErrorAction SilentlyContinue";
                        ExecutePowerShellCommand(command);

                        // 也尝试设置为 Disabled
                        command = $"Set-NetAdapterAdvancedProperty -Name '{adapterName}' -RegistryKeyword '{property}' -DisplayValue 'Disabled' -ErrorAction SilentlyContinue";
                        ExecutePowerShellCommand(command);
                        
                        success = true;
                    });
                }

                // 3. 使用WMI关闭电源管理（备用方法）
                // Use WMI to disable power management (alternative method)
                _safetyIsolator.ExecuteSilent(() =>
                {
                    var wmiCommand = $@"
                        $adapter = Get-WmiObject -Class Win32_NetworkAdapter | Where-Object {{$_.NetConnectionID -eq '{adapterName}'}};
                        if ($adapter) {{
                            $adapterConfig = Get-WmiObject -Class MSPower_DeviceEnable -Namespace root\\wmi | Where-Object {{$_.InstanceName -like ""*$($adapter.PNPDeviceID)*""}};
                            if ($adapterConfig) {{
                                $adapterConfig.Enable = $false;
                                $adapterConfig.Put();
                            }}
                        }}
                    ";
                    ExecutePowerShellCommand(wmiCommand);
                });

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "为适配器 {Adapter} 关闭节能功能时发生错误 | Error disabling power saving for adapter {Adapter}", adapterName);
                return false;
            }
        }

        /// <summary>
        /// 执行PowerShell命令
        /// Execute PowerShell command
        /// </summary>
        private string ExecutePowerShellCommand(string command)
        {
            return _safetyIsolator.Execute(() =>
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processStartInfo);
                if (process == null)
                {
                    throw new InvalidOperationException("无法启动PowerShell进程 | Failed to start PowerShell process");
                }

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(error) && !error.Contains("SilentlyContinue"))
                {
                    _logger.LogDebug("PowerShell命令执行输出: {Error} | PowerShell command execution output: {Error}", error);
                }

                return output;
            }, "执行PowerShell命令 | Execute PowerShell command", string.Empty);
        }
    }
}
