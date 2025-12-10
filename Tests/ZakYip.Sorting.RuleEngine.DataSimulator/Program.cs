using Microsoft.Extensions.Configuration;
using Spectre.Console;
using ZakYip.Sorting.RuleEngine.DataSimulator.Configuration;
using ZakYip.Sorting.RuleEngine.DataSimulator.Generators;
using ZakYip.Sorting.RuleEngine.DataSimulator.Simulators;

namespace ZakYip.Sorting.RuleEngine.DataSimulator;

/// <summary>
/// 分拣机和DWS数据模拟程序
/// Data Simulator for Sorter and DWS
/// </summary>
class Program
{
    private static SimulatorConfig _config = null!;
    private static DataGenerator _generator = null!;
    private static ISorterSimulator? _sorterSimulator;
    private static DwsSimulator? _dwsSimulator;

    static async Task Main(string[] args)
    {
        // Load configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        _config = configuration.GetSection("Simulator").Get<SimulatorConfig>() ?? new SimulatorConfig();
        _generator = new DataGenerator(_config.DataGeneration);

        // Display welcome banner
        DisplayWelcomeBanner();

        // Main menu loop
        while (true)
        {
            var choice = ShowMainMenu();
            
            if (choice == "exit")
                break;

            try
            {
                await ExecuteMenuChoice(choice);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]错误: {ex.Message}[/]");
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[yellow]按任意键继续...[/]");
                Console.ReadKey();
            }
        }

        // Cleanup
        _sorterSimulator?.Dispose();
        _dwsSimulator?.Dispose();
        
        AnsiConsole.MarkupLine("[green]程序已退出。再见！[/]");
    }

    static void DisplayWelcomeBanner()
    {
        AnsiConsole.Clear();
        
        var rule = new Rule("[bold yellow]分拣机和DWS数据模拟程序[/]")
        {
            Justification = Justify.Center
        };
        AnsiConsole.Write(rule);
        
        AnsiConsole.WriteLine();
        
        var communicationType = _config.SorterCommunicationType.ToUpper();
        var connectionInfo = communicationType == "MQTT" 
            ? $"{_config.SorterMqtt.BrokerHost}:{_config.SorterMqtt.BrokerPort}" 
            : $"{_config.SorterTcp.Host}:{_config.SorterTcp.Port}";
        
        var panel = new Panel(
            new Markup(
                "[bold]功能说明:[/]\n" +
                $"• 模拟分拣机信号发送（{communicationType}）\n" +
                "• 模拟DWS数据发送（TCP）\n" +
                "• 支持单次、批量和压力测试模式\n" +
                "• 提供详细的性能统计报告\n" +
                "\n" +
                $"[dim]配置: 分拣机 {communicationType} = {connectionInfo}[/]\n" +
                $"[dim]配置: DWS TCP = {_config.DwsTcpHost}:{_config.DwsTcpPort}[/]"
            )
        );
        panel.Header = new PanelHeader("[bold blue]欢迎[/]");
        panel.Border = BoxBorder.Rounded;
        
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    static string ShowMainMenu()
    {
        AnsiConsole.WriteLine();
        
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold green]请选择操作:[/]")
                .PageSize(10)
                .AddChoices(new[]
                {
                    "1. 发送单个分拣机信号",
                    "2. 批量发送分拣机信号",
                    "3. 分拣机压力测试",
                    "4. 发送单个DWS数据",
                    "5. 批量发送DWS数据",
                    "6. DWS压力测试",
                    "7. 完整流程模拟（包裹+DWS）",
                    "8. 查看当前配置",
                    "9. 退出"
                }));

        return choice.Split('.')[0];
    }

    static async Task ExecuteMenuChoice(string choice)
    {
        switch (choice)
        {
            case "1":
                await SendSingleParcelAsync();
                break;
            case "2":
                await SendBatchParcelsAsync();
                break;
            case "3":
                await RunSorterStressTestAsync();
                break;
            case "4":
                await SendSingleDwsAsync();
                break;
            case "5":
                await SendBatchDwsAsync();
                break;
            case "6":
                await RunDwsStressTestAsync();
                break;
            case "7":
                await RunCompleteFlowAsync();
                break;
            case "8":
                DisplayConfiguration();
                break;
            case "9":
                // Will be handled in main loop
                break;
        }
    }

    static async Task SendSingleParcelAsync()
    {
        AnsiConsole.MarkupLine("\n[bold cyan]发送单个分拣机信号[/]\n");

        // Ensure connected
        if (_sorterSimulator == null)
        {
            _sorterSimulator = CreateSorterSimulator();
            var connected = await _sorterSimulator.ConnectAsync();
            if (!connected)
            {
                AnsiConsole.MarkupLine("[red]无法连接到分拣机[/]");
                WaitForKeyPress();
                return;
            }
        }

        var parcel = _generator.GenerateParcel();
        
        var table = new Table();
        table.AddColumn("字段");
        table.AddColumn("值");
        table.AddRow("包裹ID", parcel.ParcelId);
        table.AddRow("小车号", parcel.CartNumber);
        table.AddRow("条码", parcel.Barcode);
        
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("发送中...", async ctx =>
            {
                var result = await _sorterSimulator.SendParcelAsync(parcel);
                
                AnsiConsole.WriteLine();
                if (result.Success)
                {
                    AnsiConsole.MarkupLine($"[green]✓ 发送成功[/]");
                    AnsiConsole.MarkupLine($"[dim]响应: {result.Message}[/]");
                    AnsiConsole.MarkupLine($"[dim]耗时: {result.ElapsedMs}ms[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗ 发送失败[/]");
                    AnsiConsole.MarkupLine($"[dim]错误: {result.Message}[/]");
                }
            });

        WaitForKeyPress();
    }

    static ISorterSimulator CreateSorterSimulator()
    {
        return _config.SorterCommunicationType.ToUpper() switch
        {
            "MQTT" => new MqttSorterSimulator(_config.SorterMqtt, _generator),
            "TCP" => new TcpSorterSimulator(_config.SorterTcp, _generator),
            _ => throw new InvalidOperationException($"不支持的通信类型: {_config.SorterCommunicationType}")
        };
    }

    static async Task SendBatchParcelsAsync()
    {
        AnsiConsole.MarkupLine("\n[bold cyan]批量发送分拣机信号[/]\n");

        // Ensure connected
        if (_sorterSimulator == null)
        {
            _sorterSimulator = CreateSorterSimulator();
            var connected = await _sorterSimulator.ConnectAsync();
            if (!connected)
            {
                AnsiConsole.MarkupLine("[red]无法连接到分拣机[/]");
                WaitForKeyPress();
                return;
            }
        }

        var count = AnsiConsole.Ask<int>("请输入发送数量:", 10);
        var delayMs = AnsiConsole.Ask<int>("每次发送间隔(毫秒):", 100);

        AnsiConsole.WriteLine();

        BatchResult? batchResult = null;

        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask($"[green]发送 {count} 个包裹信号[/]");
                task.MaxValue = count;

                var sendTask = Task.Run(async () =>
                {
                    var result = await _sorterSimulator.SendBatchAsync(count, delayMs);
                    return result;
                });

                while (!sendTask.IsCompleted)
                {
                    await Task.Delay(100);
                    // Estimate progress based on time
                    var expectedTimeMs = count * delayMs;
                    var increment = (double)count / (expectedTimeMs / 100.0);
                    task.Increment(increment);
                }

                batchResult = await sendTask;
                task.Value = count;
            });

        if (batchResult != null)
        {
            DisplayBatchResult(batchResult);
        }

        WaitForKeyPress();
    }

    static async Task RunSorterStressTestAsync()
    {
        AnsiConsole.MarkupLine("\n[bold cyan]分拣机压力测试[/]\n");

        // Ensure connected
        if (_sorterSimulator == null)
        {
            _sorterSimulator = CreateSorterSimulator();
            var connected = await _sorterSimulator.ConnectAsync();
            if (!connected)
            {
                AnsiConsole.MarkupLine("[red]无法连接到分拣机[/]");
                WaitForKeyPress();
                return;
            }
        }

        var duration = AnsiConsole.Ask("测试持续时间(秒):", _config.StressTest.Duration);
        var rate = AnsiConsole.Ask("目标速率(包裹/秒):", _config.StressTest.RatePerSecond);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[yellow]准备开始压力测试...[/]");
        AnsiConsole.MarkupLine($"持续时间: {duration}秒");
        AnsiConsole.MarkupLine($"目标速率: {rate}包裹/秒");
        AnsiConsole.MarkupLine($"预计总数: {duration * rate}个包裹");
        AnsiConsole.WriteLine();

        if (!AnsiConsole.Confirm("确认开始测试?"))
        {
            return;
        }

        var cts = new CancellationTokenSource();
        StressTestResult? testResult = null;

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("压力测试进行中...", async ctx =>
            {
                testResult = await _sorterSimulator.RunStressTestAsync(duration, rate, cts.Token);
            });

        if (testResult != null)
        {
            DisplayStressTestResult(testResult);
        }

        WaitForKeyPress();
    }

    static async Task SendSingleDwsAsync()
    {
        AnsiConsole.MarkupLine("\n[bold cyan]发送单个DWS数据[/]\n");

        // Ensure connected
        if (_dwsSimulator == null)
        {
            _dwsSimulator = new DwsSimulator(_config, _generator);
            var connected = await _dwsSimulator.ConnectAsync();
            if (!connected)
            {
                AnsiConsole.MarkupLine("[red]无法连接到DWS服务器[/]");
                WaitForKeyPress();
                return;
            }
        }

        var dwsData = _generator.GenerateDwsData();
        
        var table = new Table();
        table.AddColumn("字段");
        table.AddColumn("值");
        table.AddRow("条码", dwsData.Barcode);
        table.AddRow("重量(克)", dwsData.Weight.ToString("F2"));
        table.AddRow("长度(毫米)", dwsData.Length.ToString("F2"));
        table.AddRow("宽度(毫米)", dwsData.Width.ToString("F2"));
        table.AddRow("高度(毫米)", dwsData.Height.ToString("F2"));
        table.AddRow("体积(立方厘米)", dwsData.Volume.ToString("F2"));
        
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        var result = await _dwsSimulator.SendDwsDataAsync(dwsData);
        
        if (result.Success)
        {
            AnsiConsole.MarkupLine($"[green]✓ 发送成功[/]");
            AnsiConsole.MarkupLine($"[dim]耗时: {result.ElapsedMs}ms[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]✗ 发送失败[/]");
            AnsiConsole.MarkupLine($"[dim]错误: {result.Message}[/]");
        }

        WaitForKeyPress();
    }

    static async Task SendBatchDwsAsync()
    {
        AnsiConsole.MarkupLine("\n[bold cyan]批量发送DWS数据[/]\n");

        // Ensure connected
        if (_dwsSimulator == null)
        {
            _dwsSimulator = new DwsSimulator(_config, _generator);
            var connected = await _dwsSimulator.ConnectAsync();
            if (!connected)
            {
                AnsiConsole.MarkupLine("[red]无法连接到DWS服务器[/]");
                WaitForKeyPress();
                return;
            }
        }

        var count = AnsiConsole.Ask<int>("请输入发送数量:", 10);
        var delayMs = AnsiConsole.Ask<int>("每次发送间隔(毫秒):", 100);

        AnsiConsole.WriteLine();

        BatchResult? batchResult = null;

        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask($"[green]发送 {count} 条DWS数据[/]");
                task.MaxValue = count;

                var sendTask = Task.Run(async () =>
                {
                    var result = await _dwsSimulator.SendBatchAsync(count, delayMs);
                    return result;
                });

                while (!sendTask.IsCompleted)
                {
                    await Task.Delay(100);
                    var expectedTimeMs = count * delayMs;
                    var increment = (double)count / (expectedTimeMs / 100.0);
                    task.Increment(increment);
                }

                batchResult = await sendTask;
                task.Value = count;
            });

        if (batchResult != null)
        {
            DisplayBatchResult(batchResult);
        }

        WaitForKeyPress();
    }

    static async Task RunDwsStressTestAsync()
    {
        AnsiConsole.MarkupLine("\n[bold cyan]DWS压力测试[/]\n");

        // Ensure connected
        if (_dwsSimulator == null)
        {
            _dwsSimulator = new DwsSimulator(_config, _generator);
            var connected = await _dwsSimulator.ConnectAsync();
            if (!connected)
            {
                AnsiConsole.MarkupLine("[red]无法连接到DWS服务器[/]");
                WaitForKeyPress();
                return;
            }
        }

        var duration = AnsiConsole.Ask("测试持续时间(秒):", _config.StressTest.Duration);
        var rate = AnsiConsole.Ask("目标速率(数据/秒):", _config.StressTest.RatePerSecond);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[yellow]准备开始DWS压力测试...[/]");
        AnsiConsole.MarkupLine($"持续时间: {duration}秒");
        AnsiConsole.MarkupLine($"目标速率: {rate}数据/秒");
        AnsiConsole.MarkupLine($"预计总数: {duration * rate}条DWS数据");
        AnsiConsole.WriteLine();

        if (!AnsiConsole.Confirm("确认开始测试?"))
        {
            return;
        }

        var cts = new CancellationTokenSource();
        StressTestResult? testResult = null;

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("压力测试进行中...", async ctx =>
            {
                testResult = await _dwsSimulator.RunStressTestAsync(duration, rate, cts.Token);
            });

        if (testResult != null)
        {
            DisplayStressTestResult(testResult);
        }

        WaitForKeyPress();
    }

    static async Task RunCompleteFlowAsync()
    {
        AnsiConsole.MarkupLine("\n[bold cyan]完整流程模拟（包裹+DWS）[/]\n");

        var count = AnsiConsole.Ask<int>("请输入模拟数量:", 5);
        var delayMs = AnsiConsole.Ask<int>("包裹和DWS之间的间隔(毫秒):", 500);

        AnsiConsole.WriteLine();

        // Ensure sorter connected
        if (_sorterSimulator == null)
        {
            _sorterSimulator = CreateSorterSimulator();
            var connected = await _sorterSimulator.ConnectAsync();
            if (!connected)
            {
                AnsiConsole.MarkupLine("[red]无法连接到分拣机[/]");
                WaitForKeyPress();
                return;
            }
        }

        // Ensure DWS connected
        if (_dwsSimulator == null)
        {
            _dwsSimulator = new DwsSimulator(_config, _generator);
            var connected = await _dwsSimulator.ConnectAsync();
            if (!connected)
            {
                AnsiConsole.MarkupLine("[red]无法连接到DWS服务器[/]");
                WaitForKeyPress();
                return;
            }
        }

        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask($"[green]模拟完整流程[/]", maxValue: count);

                for (int i = 0; i < count; i++)
                {
                    var (parcel, dws) = _generator.GenerateCompletePair();

                    AnsiConsole.MarkupLine($"\n[yellow]处理包裹 {i + 1}/{count}[/]");
                    AnsiConsole.MarkupLine($"包裹ID: {parcel.ParcelId}");
                    AnsiConsole.MarkupLine($"条码: {parcel.Barcode}");

                    // Send parcel signal
                    var parcelResult = await _sorterSimulator.SendParcelAsync(parcel);
                    if (parcelResult.Success)
                    {
                        AnsiConsole.MarkupLine($"[green]✓ 包裹信号发送成功[/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red]✗ 包裹信号发送失败: {parcelResult.Message}[/]");
                    }

                    // Wait before sending DWS
                    await Task.Delay(delayMs);

                    // Send DWS data
                    var dwsResult = await _dwsSimulator.SendDwsDataAsync(dws);
                    if (dwsResult.Success)
                    {
                        AnsiConsole.MarkupLine($"[green]✓ DWS数据发送成功[/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red]✗ DWS数据发送失败: {dwsResult.Message}[/]");
                    }

                    task.Increment(1);
                }
            });

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[green]✓ 完整流程模拟完成[/]");

        WaitForKeyPress();
    }

    static void DisplayConfiguration()
    {
        AnsiConsole.MarkupLine("\n[bold cyan]当前配置[/]\n");

        var table = new Table();
        table.AddColumn("配置项");
        table.AddColumn("值");

        table.AddRow("分拣机通信类型", _config.SorterCommunicationType);
        
        if (_config.SorterCommunicationType.ToUpper() == "MQTT")
        {
            table.AddRow("MQTT代理地址", _config.SorterMqtt.BrokerHost);
            table.AddRow("MQTT代理端口", _config.SorterMqtt.BrokerPort.ToString());
            table.AddRow("MQTT发布主题", _config.SorterMqtt.PublishTopic);
            table.AddRow("MQTT客户端ID", _config.SorterMqtt.ClientId);
        }
        else
        {
            table.AddRow("分拣机TCP主机", _config.SorterTcp.Host);
            table.AddRow("分拣机TCP端口", _config.SorterTcp.Port.ToString());
        }
        
        table.AddRow("DWS TCP 主机", _config.DwsTcpHost);
        table.AddRow("DWS TCP 端口", _config.DwsTcpPort.ToString());
        table.AddRow("压力测试持续时间", $"{_config.StressTest.Duration}秒");
        table.AddRow("压力测试速率", $"{_config.StressTest.RatePerSecond}/秒");
        table.AddRow("重量范围", $"{_config.DataGeneration.WeightMin}-{_config.DataGeneration.WeightMax}克");
        table.AddRow("尺寸范围(长)", $"{_config.DataGeneration.LengthMin}-{_config.DataGeneration.LengthMax}毫米");
        table.AddRow("尺寸范围(宽)", $"{_config.DataGeneration.WidthMin}-{_config.DataGeneration.WidthMax}毫米");
        table.AddRow("尺寸范围(高)", $"{_config.DataGeneration.HeightMin}-{_config.DataGeneration.HeightMax}毫米");

        AnsiConsole.Write(table);

        WaitForKeyPress();
    }

    static void DisplayBatchResult(BatchResult result)
    {
        AnsiConsole.WriteLine();
        
        var table = new Table();
        table.BorderStyle(Style.Parse("green"));
        table.Title("[bold green]批量测试结果[/]");
        table.AddColumn("指标");
        table.AddColumn("值");

        table.AddRow("总数", result.TotalCount.ToString());
        table.AddRow("成功", $"[green]{result.SuccessCount}[/]");
        table.AddRow("失败", result.FailureCount > 0 ? $"[red]{result.FailureCount}[/]" : "0");
        table.AddRow("总耗时", $"{result.TotalTimeMs}ms");
        table.AddRow("平均延迟", $"{result.AverageLatencyMs:F2}ms");
        table.AddRow("最小延迟", $"{result.MinLatencyMs}ms");
        table.AddRow("最大延迟", $"{result.MaxLatencyMs}ms");
        table.AddRow("成功率", $"{(result.SuccessCount * 100.0 / result.TotalCount):F2}%");

        AnsiConsole.Write(table);
    }

    static void DisplayStressTestResult(StressTestResult result)
    {
        AnsiConsole.WriteLine();
        
        var innerTable = new Table()
                .AddColumn("指标")
                .AddColumn("值")
                .AddRow("持续时间", $"{result.DurationSeconds:F2}秒")
                .AddRow("目标速率", $"{result.TargetRate}/秒")
                .AddRow("实际速率", $"[yellow]{result.ActualRate:F2}/秒[/]")
                .AddRow("总发送数", result.TotalSent.ToString())
                .AddRow("成功", $"[green]{result.SuccessCount}[/]")
                .AddRow("失败", result.FailureCount > 0 ? $"[red]{result.FailureCount}[/]" : "0")
                .AddRow("成功率", $"{(result.SuccessCount * 100.0 / result.TotalSent):F2}%")
                .AddRow("平均延迟", $"{result.AverageLatencyMs:F2}ms")
                .AddRow("P50延迟", $"{result.P50LatencyMs:F2}ms")
                .AddRow("P95延迟", $"{result.P95LatencyMs:F2}ms")
                .AddRow("P99延迟", $"{result.P99LatencyMs:F2}ms");
        
        var panel = new Panel(innerTable);
        panel.Header = new PanelHeader("[bold green]压力测试结果[/]");
        panel.Border = BoxBorder.Double;

        AnsiConsole.Write(panel);

        // Display recommendations
        AnsiConsole.WriteLine();
        if (result.SuccessCount * 100.0 / result.TotalSent < 95)
        {
            AnsiConsole.MarkupLine("[yellow]⚠ 成功率低于95%，建议检查系统性能[/]");
        }
        if (result.P99LatencyMs > 1000)
        {
            AnsiConsole.MarkupLine("[yellow]⚠ P99延迟超过1秒，建议优化系统响应[/]");
        }
        if (Math.Abs(result.ActualRate - result.TargetRate) > result.TargetRate * 0.1)
        {
            AnsiConsole.MarkupLine("[yellow]⚠ 实际速率与目标速率偏差超过10%[/]");
        }
    }

    static void WaitForKeyPress()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]按任意键继续...[/]");
        Console.ReadKey();
        AnsiConsole.Clear();
        DisplayWelcomeBanner();
    }
}
