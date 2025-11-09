namespace ZakYip.Sorting.RuleEngine.DataSimulator.Configuration;

/// <summary>
/// 模拟器配置
/// Simulator configuration
/// </summary>
public class SimulatorConfig
{
    /// <summary>
    /// HTTP API URL
    /// </summary>
    public string HttpApiUrl { get; set; } = "http://localhost:5000";

    /// <summary>
    /// DWS TCP主机地址
    /// DWS TCP host address
    /// </summary>
    public string DwsTcpHost { get; set; } = "127.0.0.1";

    /// <summary>
    /// DWS TCP端口
    /// DWS TCP port
    /// </summary>
    public int DwsTcpPort { get; set; } = 8001;

    /// <summary>
    /// 压力测试配置
    /// Stress test configuration
    /// </summary>
    public StressTestConfig StressTest { get; set; } = new();

    /// <summary>
    /// 数据生成配置
    /// Data generation configuration
    /// </summary>
    public DataGenerationConfig DataGeneration { get; set; } = new();
}

/// <summary>
/// 压力测试配置
/// Stress test configuration
/// </summary>
public class StressTestConfig
{
    /// <summary>
    /// 测试持续时间（秒）
    /// Test duration in seconds
    /// </summary>
    public int Duration { get; set; } = 60;

    /// <summary>
    /// 每秒请求数
    /// Requests per second
    /// </summary>
    public int RatePerSecond { get; set; } = 100;

    /// <summary>
    /// 预热时间（秒）
    /// Warmup time in seconds
    /// </summary>
    public int WarmupSeconds { get; set; } = 5;
}

/// <summary>
/// 数据生成配置
/// Data generation configuration
/// </summary>
public class DataGenerationConfig
{
    /// <summary>
    /// 最小重量（克）
    /// Minimum weight in grams
    /// </summary>
    public int WeightMin { get; set; } = 100;

    /// <summary>
    /// 最大重量（克）
    /// Maximum weight in grams
    /// </summary>
    public int WeightMax { get; set; } = 5000;

    /// <summary>
    /// 最小长度（毫米）
    /// Minimum length in millimeters
    /// </summary>
    public int LengthMin { get; set; } = 100;

    /// <summary>
    /// 最大长度（毫米）
    /// Maximum length in millimeters
    /// </summary>
    public int LengthMax { get; set; } = 500;

    /// <summary>
    /// 最小宽度（毫米）
    /// Minimum width in millimeters
    /// </summary>
    public int WidthMin { get; set; } = 100;

    /// <summary>
    /// 最大宽度（毫米）
    /// Maximum width in millimeters
    /// </summary>
    public int WidthMax { get; set; } = 500;

    /// <summary>
    /// 最小高度（毫米）
    /// Minimum height in millimeters
    /// </summary>
    public int HeightMin { get; set; } = 50;

    /// <summary>
    /// 最大高度（毫米）
    /// Maximum height in millimeters
    /// </summary>
    public int HeightMax { get; set; } = 300;
}
