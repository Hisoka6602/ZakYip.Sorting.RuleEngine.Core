using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.Json;

namespace ShadowCloneDetector;

/// <summary>
/// 影分身检测工具主程序
/// Shadow Clone Detector Main Program
/// </summary>
file class Program
{
    private const int MaxDisplayedDuplicates = 5; // 最多显示的重复项数量 / Maximum duplicates to display
    
    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("🔍 影分身检测工具 / Shadow Clone Detector");
        Console.WriteLine("==========================================\n");

        if (args.Length == 0)
        {
            Console.WriteLine("用法 / Usage: ShadowCloneDetector <directory-path> [--json] [--threshold <value>]");
            Console.WriteLine("示例 / Example: ShadowCloneDetector /path/to/project --threshold 0.85");
            return 1;
        }

        string directoryPath = args[0];
        bool jsonOutput = args.Contains("--json");
        double similarityThreshold = 0.80; // 默认相似度阈值 80%

        var thresholdIndex = Array.IndexOf(args, "--threshold");
        if (thresholdIndex >= 0 && thresholdIndex + 1 < args.Length &&
            double.TryParse(args[thresholdIndex + 1], out double threshold))
        {
            similarityThreshold = threshold;
        }

        if (!Directory.Exists(directoryPath))
        {
            Console.WriteLine($"❌ 错误：目录不存在 / Error: Directory not found: {directoryPath}");
            return 1;
        }

        var detector = new ShadowCloneAnalyzer(directoryPath, similarityThreshold);
        var report = await detector.AnalyzeAsync();

        if (jsonOutput)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            Console.WriteLine(JsonSerializer.Serialize(report, jsonOptions));
        }
        else
        {
            PrintReport(report);
        }

        // 返回码：如果发现影分身则返回 1，否则返回 0
        // Return code: 1 if shadow clones found, 0 otherwise
        return report.TotalDuplicates > 0 ? 1 : 0;
    }

    static void PrintReport(DetectionReport report)
    {
        Console.WriteLine($"\n📊 检测结果摘要 / Detection Results Summary");
        Console.WriteLine("==========================================");
        Console.WriteLine($"扫描文件数 / Files Scanned: {report.FilesScanned}");
        Console.WriteLine($"相似度阈值 / Similarity Threshold: {report.SimilarityThreshold:P0}");
        Console.WriteLine($"发现影分身总数 / Total Duplicates Found: {report.TotalDuplicates}");
        Console.WriteLine();

        PrintCategoryResults("枚举 / Enums", report.EnumDuplicates);
        PrintCategoryResults("接口 / Interfaces", report.InterfaceDuplicates);
        PrintCategoryResults("DTO", report.DtoDuplicates);
        PrintCategoryResults("Options/配置类", report.OptionsDuplicates);
        PrintCategoryResults("扩展方法 / Extension Methods", report.ExtensionMethodDuplicates);
        PrintCategoryResults("静态类 / Static Classes", report.StaticClassDuplicates);
        PrintCategoryResults("常量 / Constants", report.ConstantDuplicates);

        if (report.TotalDuplicates > 0)
        {
            Console.WriteLine("\n⚠️  发现影分身代码！请查看上面的详细信息。");
            Console.WriteLine("⚠️  Shadow clone code detected! Please review the details above.");
        }
        else
        {
            Console.WriteLine("\n✅ 未发现影分身代码。");
            Console.WriteLine("✅ No shadow clone code detected.");
        }
    }

    static void PrintCategoryResults(string category, List<DuplicateInfo> duplicates)
    {
        Console.WriteLine($"\n📦 {category}");
        Console.WriteLine($"   发现 / Found: {duplicates.Count} 组重复");

        foreach (var dup in duplicates.Take(MaxDisplayedDuplicates))
        {
            Console.WriteLine($"   ⚠️  相似度 {dup.Similarity:P0}: {dup.Name}");
            Console.WriteLine($"      📄 {dup.Location1}");
            Console.WriteLine($"      📄 {dup.Location2}");
            if (!string.IsNullOrEmpty(dup.Reason))
            {
                Console.WriteLine($"      💡 {dup.Reason}");
            }
        }

        if (duplicates.Count > MaxDisplayedDuplicates)
        {
            Console.WriteLine($"   ... 还有 {duplicates.Count - MaxDisplayedDuplicates} 组重复");
        }
    }
}
