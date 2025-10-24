using BenchmarkDotNet.Running;

namespace ZakYip.Sorting.RuleEngine.Benchmarks;

/// <summary>
/// 性能基准测试程序入口
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        // 运行所有基准测试
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
