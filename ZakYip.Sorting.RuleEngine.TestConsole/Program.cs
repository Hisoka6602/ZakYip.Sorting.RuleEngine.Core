using System.Text;
using System.Text.Json;
using TouchSocket.Core;
using TouchSocket.Sockets;

namespace ZakYip.Sorting.RuleEngine.TestConsole;

/// <summary>
/// 分拣机信号模拟测试控制台
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== 分拣机信号模拟测试程序 ===");
        Console.WriteLine("此程序用于模拟分拣机发送信号到主系统");
        Console.WriteLine();

        var mode = GetOperationMode();

        if (mode == 1)
        {
            await RunSorterSimulatorAsync();
        }
        else
        {
            await RunDwsSimulatorAsync();
        }
    }

    static int GetOperationMode()
    {
        Console.WriteLine("请选择运行模式:");
        Console.WriteLine("1. 模拟分拣机信号（创建包裹）");
        Console.WriteLine("2. 模拟DWS数据发送");
        Console.Write("请输入选项（1或2）: ");
        
        if (int.TryParse(Console.ReadLine(), out int mode) && (mode == 1 || mode == 2))
        {
            return mode;
        }
        
        Console.WriteLine("无效输入，默认使用模式1（分拣机信号）");
        return 1;
    }

    /// <summary>
    /// 运行分拣机模拟器 - 通过HTTP API发送信号
    /// </summary>
    static async Task RunSorterSimulatorAsync()
    {
        Console.WriteLine("\n=== 分拣机信号模拟器 ===");
        Console.Write("请输入API地址（默认: http://localhost:5000）: ");
        var apiUrl = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(apiUrl))
        {
            apiUrl = "http://localhost:5000";
        }

        using var httpClient = new HttpClient { BaseAddress = new Uri(apiUrl) };

        while (true)
        {
            Console.WriteLine("\n--- 新包裹信息 ---");
            Console.Write("包裹ID（输入 'exit' 退出）: ");
            var parcelId = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(parcelId) || parcelId.ToLower() == "exit")
            {
                break;
            }

            Console.Write("小车号: ");
            var cartNumber = Console.ReadLine() ?? "CART001";

            Console.Write("条码（可选）: ");
            var barcode = Console.ReadLine();

            try
            {
                var request = new
                {
                    parcelId,
                    cartNumber,
                    barcode
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("/api/sortingmachine/create-parcel", content);
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"✓ 包裹信号发送成功: {result}");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"✗ 包裹信号发送失败: {response.StatusCode} - {result}");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ 发送错误: {ex.Message}");
                Console.ResetColor();
            }
        }

        Console.WriteLine("\n程序已退出。");
    }

    /// <summary>
    /// 运行DWS数据模拟器 - 通过TCP发送DWS数据
    /// </summary>
    static async Task RunDwsSimulatorAsync()
    {
        Console.WriteLine("\n=== DWS数据模拟器（TCP） ===");
        Console.Write("请输入DWS服务器地址（默认: 127.0.0.1）: ");
        var host = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(host))
        {
            host = "127.0.0.1";
        }

        Console.Write("请输入DWS服务器端口（默认: 8001）: ");
        if (!int.TryParse(Console.ReadLine(), out int port))
        {
            port = 8001;
        }

        var tcpClient = new TcpClient();
        
        try
        {
            await tcpClient.SetupAsync(new TouchSocketConfig()
                .SetRemoteIPHost(new IPHost($"{host}:{port}"))
                .SetTcpDataHandlingAdapter(() => new TerminatorPackageAdapter("\n")));

            await tcpClient.ConnectAsync();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ 已连接到DWS服务器: {host}:{port}");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ 连接DWS服务器失败: {ex.Message}");
            Console.ResetColor();
            return;
        }

        while (true)
        {
            Console.WriteLine("\n--- DWS数据 ---");
            Console.Write("包裹ID（输入 'exit' 退出）: ");
            var parcelId = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(parcelId) || parcelId.ToLower() == "exit")
            {
                break;
            }

            Console.Write("条码: ");
            var barcode = Console.ReadLine() ?? "";

            Console.Write("重量（克）: ");
            if (!double.TryParse(Console.ReadLine(), out double weight))
            {
                weight = 1000;
            }

            Console.Write("长度（毫米）: ");
            if (!double.TryParse(Console.ReadLine(), out double length))
            {
                length = 300;
            }

            Console.Write("宽度（毫米）: ");
            if (!double.TryParse(Console.ReadLine(), out double width))
            {
                width = 200;
            }

            Console.Write("高度（毫米）: ");
            if (!double.TryParse(Console.ReadLine(), out double height))
            {
                height = 150;
            }

            var volume = length * width * height;

            try
            {
                var dwsData = new
                {
                    parcelId,
                    barcode,
                    weight,
                    length,
                    width,
                    height,
                    volume
                };

                var json = JsonSerializer.Serialize(dwsData) + "\n";
                var data = Encoding.UTF8.GetBytes(json);

                await tcpClient.SendAsync(data);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ DWS数据发送成功");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ 发送错误: {ex.Message}");
                Console.ResetColor();
            }
        }

        tcpClient.Close();
        Console.WriteLine("\n程序已退出。");
    }
}

