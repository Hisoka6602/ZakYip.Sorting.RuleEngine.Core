namespace ShadowCloneDetector;

/// <summary>
/// 检测报告 / Detection Report
/// </summary>
public class DetectionReport
{
    public int FilesScanned { get; set; }
    public double SimilarityThreshold { get; set; }
    public int TotalDuplicates => EnumDuplicates.Count + InterfaceDuplicates.Count + 
                                   DtoDuplicates.Count + OptionsDuplicates.Count + 
                                   ExtensionMethodDuplicates.Count + StaticClassDuplicates.Count + 
                                   ConstantDuplicates.Count;
    
    public List<DuplicateInfo> EnumDuplicates { get; set; } = new();
    public List<DuplicateInfo> InterfaceDuplicates { get; set; } = new();
    public List<DuplicateInfo> DtoDuplicates { get; set; } = new();
    public List<DuplicateInfo> OptionsDuplicates { get; set; } = new();
    public List<DuplicateInfo> ExtensionMethodDuplicates { get; set; } = new();
    public List<DuplicateInfo> StaticClassDuplicates { get; set; } = new();
    public List<DuplicateInfo> ConstantDuplicates { get; set; } = new();
}

/// <summary>
/// 重复信息 / Duplicate Information
/// </summary>
public class DuplicateInfo
{
    public required string Name { get; init; }
    public required string Location1 { get; init; }
    public required string Location2 { get; init; }
    public double Similarity { get; init; }
    public string? Reason { get; init; }
}

/// <summary>
/// 枚举信息 / Enum Information
/// </summary>
internal class EnumInfo
{
    public required string Name { get; init; }
    public required string FilePath { get; init; }
    public required List<string> Members { get; init; }
    public required string Namespace { get; init; }
}

/// <summary>
/// 接口信息 / Interface Information
/// </summary>
internal class InterfaceInfo
{
    public required string Name { get; init; }
    public required string FilePath { get; init; }
    public required List<string> MethodSignatures { get; init; }
    public required string Namespace { get; init; }
}

/// <summary>
/// DTO 信息 / DTO Information
/// </summary>
internal class DtoInfo
{
    public required string Name { get; init; }
    public required string FilePath { get; init; }
    public required List<PropertyInfo> Properties { get; init; }
    public required string Namespace { get; init; }
}

/// <summary>
/// 属性信息 / Property Information
/// </summary>
internal class PropertyInfo
{
    public required string Name { get; init; }
    public required string Type { get; init; }
}

/// <summary>
/// Options/配置类信息 / Options/Config Class Information
/// </summary>
internal class OptionsInfo
{
    public required string Name { get; init; }
    public required string FilePath { get; init; }
    public required List<PropertyInfo> Properties { get; init; }
    public required string Namespace { get; init; }
}

/// <summary>
/// 扩展方法信息 / Extension Method Information
/// </summary>
internal class ExtensionMethodInfo
{
    public required string Name { get; init; }
    public required string FilePath { get; init; }
    public required string Signature { get; init; }
    public required string ClassName { get; init; }
}

/// <summary>
/// 静态类信息 / Static Class Information
/// </summary>
internal class StaticClassInfo
{
    public required string Name { get; init; }
    public required string FilePath { get; init; }
    public required List<string> MethodSignatures { get; init; }
    public required string Namespace { get; init; }
}

/// <summary>
/// 常量信息 / Constant Information
/// </summary>
internal class ConstantInfo
{
    public required string Name { get; init; }
    public required string FilePath { get; init; }
    public required string Value { get; init; }
    public required string Type { get; init; }
    public required string ClassName { get; init; }
}
