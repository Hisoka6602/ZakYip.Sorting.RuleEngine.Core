using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ShadowCloneDetector;

/// <summary>
/// 影分身分析器 / Shadow Clone Analyzer
/// </summary>
internal class ShadowCloneAnalyzer
{
    private readonly string _directoryPath;
    private readonly double _similarityThreshold;
    
    private readonly List<EnumInfo> _enums = new();
    private readonly List<InterfaceInfo> _interfaces = new();
    private readonly List<DtoInfo> _dtos = new();
    private readonly List<OptionsInfo> _options = new();
    private readonly List<ExtensionMethodInfo> _extensionMethods = new();
    private readonly List<StaticClassInfo> _staticClasses = new();
    private readonly List<ConstantInfo> _constants = new();

    public ShadowCloneAnalyzer(string directoryPath, double similarityThreshold)
    {
        _directoryPath = directoryPath;
        _similarityThreshold = similarityThreshold;
    }

    public async Task<DetectionReport> AnalyzeAsync()
    {
        var csFiles = Directory.GetFiles(_directoryPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("/bin/") && !f.Contains("/obj/") && 
                       !f.Contains("/Migrations/") && !f.Contains("/Tests/"))
            .ToList();

        foreach (var file in csFiles)
        {
            await AnalyzeFileAsync(file);
        }

        var report = new DetectionReport
        {
            FilesScanned = csFiles.Count,
            SimilarityThreshold = _similarityThreshold
        };

        // 检测各类重复 / Detect various duplicates
        report.EnumDuplicates = DetectEnumDuplicates();
        report.InterfaceDuplicates = DetectInterfaceDuplicates();
        report.DtoDuplicates = DetectDtoDuplicates();
        report.OptionsDuplicates = DetectOptionsDuplicates();
        report.ExtensionMethodDuplicates = DetectExtensionMethodDuplicates();
        report.StaticClassDuplicates = DetectStaticClassDuplicates();
        report.ConstantDuplicates = DetectConstantDuplicates();

        return report;
    }

    private async Task AnalyzeFileAsync(string filePath)
    {
        var code = await File.ReadAllTextAsync(filePath);
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        ExtractEnums(root, filePath);
        ExtractInterfaces(root, filePath);
        ExtractDtos(root, filePath);
        ExtractOptions(root, filePath);
        ExtractExtensionMethods(root, filePath);
        ExtractStaticClasses(root, filePath);
        ExtractConstants(root, filePath);
    }

    private void ExtractEnums(SyntaxNode root, string filePath)
    {
        var enumDeclarations = root.DescendantNodes().OfType<EnumDeclarationSyntax>();
        
        foreach (var enumDecl in enumDeclarations)
        {
            var members = enumDecl.Members.Select(m => m.Identifier.Text).ToList();
            var namespaceName = GetNamespace(enumDecl);
            
            _enums.Add(new EnumInfo
            {
                Name = enumDecl.Identifier.Text,
                FilePath = filePath,
                Members = members,
                Namespace = namespaceName
            });
        }
    }

    private void ExtractInterfaces(SyntaxNode root, string filePath)
    {
        var interfaceDeclarations = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>();
        
        foreach (var interfaceDecl in interfaceDeclarations)
        {
            var methods = interfaceDecl.Members
                .OfType<MethodDeclarationSyntax>()
                .Select(m => GetMethodSignature(m))
                .ToList();
            
            var namespaceName = GetNamespace(interfaceDecl);
            
            _interfaces.Add(new InterfaceInfo
            {
                Name = interfaceDecl.Identifier.Text,
                FilePath = filePath,
                MethodSignatures = methods,
                Namespace = namespaceName
            });
        }
    }

    private void ExtractDtos(SyntaxNode root, string filePath)
    {
        var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
        
        foreach (var classDecl in classDeclarations)
        {
            var className = classDecl.Identifier.Text;
            
            // 识别 DTO: 名称包含 Dto/DTO/Request/Response
            // Identify DTOs: name contains Dto/DTO/Request/Response
            if (className.EndsWith("Dto", StringComparison.OrdinalIgnoreCase) ||
                className.EndsWith("Request", StringComparison.OrdinalIgnoreCase) ||
                className.EndsWith("Response", StringComparison.OrdinalIgnoreCase) ||
                className.Contains("DTO"))
            {
                var properties = ExtractProperties(classDecl);
                var namespaceName = GetNamespace(classDecl);
                
                _dtos.Add(new DtoInfo
                {
                    Name = className,
                    FilePath = filePath,
                    Properties = properties,
                    Namespace = namespaceName
                });
            }
        }
    }

    private void ExtractOptions(SyntaxNode root, string filePath)
    {
        var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
        
        foreach (var classDecl in classDeclarations)
        {
            var className = classDecl.Identifier.Text;
            
            // 识别 Options/Config: 名称包含 Options/Config/Settings
            // Identify Options/Config: name contains Options/Config/Settings
            if (className.EndsWith("Options", StringComparison.OrdinalIgnoreCase) ||
                className.EndsWith("Config", StringComparison.OrdinalIgnoreCase) ||
                className.EndsWith("Configuration", StringComparison.OrdinalIgnoreCase) ||
                className.EndsWith("Settings", StringComparison.OrdinalIgnoreCase))
            {
                var properties = ExtractProperties(classDecl);
                var namespaceName = GetNamespace(classDecl);
                
                _options.Add(new OptionsInfo
                {
                    Name = className,
                    FilePath = filePath,
                    Properties = properties,
                    Namespace = namespaceName
                });
            }
        }
    }

    private void ExtractExtensionMethods(SyntaxNode root, string filePath)
    {
        var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
        
        foreach (var classDecl in classDeclarations)
        {
            if (classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)))
            {
                var methods = classDecl.Members
                    .OfType<MethodDeclarationSyntax>()
                    .Where(m => m.ParameterList.Parameters.Any(p => 
                        p.Modifiers.Any(mod => mod.IsKind(SyntaxKind.ThisKeyword))));
                
                foreach (var method in methods)
                {
                    _extensionMethods.Add(new ExtensionMethodInfo
                    {
                        Name = method.Identifier.Text,
                        FilePath = filePath,
                        Signature = GetMethodSignature(method),
                        ClassName = classDecl.Identifier.Text
                    });
                }
            }
        }
    }

    private void ExtractStaticClasses(SyntaxNode root, string filePath)
    {
        var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
        
        foreach (var classDecl in classDeclarations)
        {
            if (classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)))
            {
                var className = classDecl.Identifier.Text;
                
                // 排除扩展方法类 / Exclude extension method classes
                if (className.EndsWith("Extensions", StringComparison.OrdinalIgnoreCase))
                    continue;
                
                var methods = classDecl.Members
                    .OfType<MethodDeclarationSyntax>()
                    .Select(m => GetMethodSignature(m))
                    .ToList();
                
                var namespaceName = GetNamespace(classDecl);
                
                _staticClasses.Add(new StaticClassInfo
                {
                    Name = className,
                    FilePath = filePath,
                    MethodSignatures = methods,
                    Namespace = namespaceName
                });
            }
        }
    }

    private void ExtractConstants(SyntaxNode root, string filePath)
    {
        var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
        
        foreach (var classDecl in classDeclarations)
        {
            var fields = classDecl.Members
                .OfType<FieldDeclarationSyntax>()
                .Where(f => f.Modifiers.Any(m => m.IsKind(SyntaxKind.ConstKeyword)));
            
            foreach (var field in fields)
            {
                var variable = field.Declaration.Variables.FirstOrDefault();
                if (variable is not null)
                {
                    var value = variable.Initializer?.Value?.ToString() ?? string.Empty;
                    
                    _constants.Add(new ConstantInfo
                    {
                        Name = variable.Identifier.Text,
                        FilePath = filePath,
                        Value = value,
                        Type = field.Declaration.Type.ToString(),
                        ClassName = classDecl.Identifier.Text
                    });
                }
            }
        }
    }

    private List<PropertyInfo> ExtractProperties(ClassDeclarationSyntax classDecl)
    {
        return classDecl.Members
            .OfType<PropertyDeclarationSyntax>()
            .Select(p => new PropertyInfo
            {
                Name = p.Identifier.Text,
                Type = p.Type.ToString()
            })
            .ToList();
    }

    private string GetMethodSignature(MethodDeclarationSyntax method)
    {
        var parameters = string.Join(", ", method.ParameterList.Parameters
            .Select(p => $"{p.Type} {p.Identifier}"));
        return $"{method.ReturnType} {method.Identifier}({parameters})";
    }

    private string GetNamespace(SyntaxNode node)
    {
        var namespaceDecl = node.Ancestors()
            .OfType<BaseNamespaceDeclarationSyntax>()
            .FirstOrDefault();
        
        return namespaceDecl?.Name.ToString() ?? "Global";
    }

    // 检测方法实现在下一个文件 / Detection methods implemented in next part
    private List<DuplicateInfo> DetectEnumDuplicates()
    {
        var duplicates = new List<DuplicateInfo>();
        
        for (int i = 0; i < _enums.Count; i++)
        {
            for (int j = i + 1; j < _enums.Count; j++)
            {
                var similarity = CalculateEnumSimilarity(_enums[i], _enums[j]);
                if (similarity >= _similarityThreshold)
                {
                    duplicates.Add(new DuplicateInfo
                    {
                        Name = $"{_enums[i].Name} ↔ {_enums[j].Name}",
                        Location1 = GetRelativePath(_enums[i].FilePath),
                        Location2 = GetRelativePath(_enums[j].FilePath),
                        Similarity = similarity,
                        Reason = $"枚举成员相似度高 / High similarity in enum members"
                    });
                }
            }
        }
        
        return duplicates;
    }

    private List<DuplicateInfo> DetectInterfaceDuplicates()
    {
        var duplicates = new List<DuplicateInfo>();
        
        for (int i = 0; i < _interfaces.Count; i++)
        {
            for (int j = i + 1; j < _interfaces.Count; j++)
            {
                var similarity = CalculateInterfaceSimilarity(_interfaces[i], _interfaces[j]);
                if (similarity >= _similarityThreshold)
                {
                    duplicates.Add(new DuplicateInfo
                    {
                        Name = $"{_interfaces[i].Name} ↔ {_interfaces[j].Name}",
                        Location1 = GetRelativePath(_interfaces[i].FilePath),
                        Location2 = GetRelativePath(_interfaces[j].FilePath),
                        Similarity = similarity,
                        Reason = $"接口方法签名重叠 / Interface method signatures overlap"
                    });
                }
            }
        }
        
        return duplicates;
    }

    private List<DuplicateInfo> DetectDtoDuplicates()
    {
        var duplicates = new List<DuplicateInfo>();
        
        for (int i = 0; i < _dtos.Count; i++)
        {
            for (int j = i + 1; j < _dtos.Count; j++)
            {
                var similarity = CalculateDtoSimilarity(_dtos[i], _dtos[j]);
                if (similarity >= _similarityThreshold)
                {
                    duplicates.Add(new DuplicateInfo
                    {
                        Name = $"{_dtos[i].Name} ↔ {_dtos[j].Name}",
                        Location1 = GetRelativePath(_dtos[i].FilePath),
                        Location2 = GetRelativePath(_dtos[j].FilePath),
                        Similarity = similarity,
                        Reason = $"DTO 字段结构相同 / DTO field structures are identical"
                    });
                }
            }
        }
        
        return duplicates;
    }

    private List<DuplicateInfo> DetectOptionsDuplicates()
    {
        var duplicates = new List<DuplicateInfo>();
        
        for (int i = 0; i < _options.Count; i++)
        {
            for (int j = i + 1; j < _options.Count; j++)
            {
                // 检查是否在不同命名空间但结构相似
                // Check if in different namespaces but structurally similar
                if (_options[i].Namespace != _options[j].Namespace)
                {
                    var similarity = CalculateOptionsSimilarity(_options[i], _options[j]);
                    if (similarity >= _similarityThreshold)
                    {
                        duplicates.Add(new DuplicateInfo
                        {
                            Name = $"{_options[i].Name} ↔ {_options[j].Name}",
                            Location1 = GetRelativePath(_options[i].FilePath),
                            Location2 = GetRelativePath(_options[j].FilePath),
                            Similarity = similarity,
                            Reason = $"配置类在多个命名空间重复 / Config class duplicated across namespaces"
                        });
                    }
                }
            }
        }
        
        return duplicates;
    }

    private List<DuplicateInfo> DetectExtensionMethodDuplicates()
    {
        var duplicates = new List<DuplicateInfo>();
        
        for (int i = 0; i < _extensionMethods.Count; i++)
        {
            for (int j = i + 1; j < _extensionMethods.Count; j++)
            {
                if (_extensionMethods[i].Signature == _extensionMethods[j].Signature)
                {
                    duplicates.Add(new DuplicateInfo
                    {
                        Name = $"{_extensionMethods[i].Name} ↔ {_extensionMethods[j].Name}",
                        Location1 = GetRelativePath(_extensionMethods[i].FilePath),
                        Location2 = GetRelativePath(_extensionMethods[j].FilePath),
                        Similarity = 1.0,
                        Reason = $"扩展方法签名相同 / Extension method signatures are identical"
                    });
                }
            }
        }
        
        return duplicates;
    }

    private List<DuplicateInfo> DetectStaticClassDuplicates()
    {
        var duplicates = new List<DuplicateInfo>();
        
        for (int i = 0; i < _staticClasses.Count; i++)
        {
            for (int j = i + 1; j < _staticClasses.Count; j++)
            {
                var similarity = CalculateStaticClassSimilarity(_staticClasses[i], _staticClasses[j]);
                if (similarity >= _similarityThreshold)
                {
                    duplicates.Add(new DuplicateInfo
                    {
                        Name = $"{_staticClasses[i].Name} ↔ {_staticClasses[j].Name}",
                        Location1 = GetRelativePath(_staticClasses[i].FilePath),
                        Location2 = GetRelativePath(_staticClasses[j].FilePath),
                        Similarity = similarity,
                        Reason = $"静态类功能重复 / Static class functionality is duplicated"
                    });
                }
            }
        }
        
        return duplicates;
    }

    private List<DuplicateInfo> DetectConstantDuplicates()
    {
        var duplicates = new List<DuplicateInfo>();
        
        for (int i = 0; i < _constants.Count; i++)
        {
            for (int j = i + 1; j < _constants.Count; j++)
            {
                // 相同值的常量
                // Constants with same value
                if (_constants[i].Value == _constants[j].Value && 
                    _constants[i].Type == _constants[j].Type &&
                    _constants[i].ClassName != _constants[j].ClassName)
                {
                    duplicates.Add(new DuplicateInfo
                    {
                        Name = $"{_constants[i].Name} ↔ {_constants[j].Name}",
                        Location1 = GetRelativePath(_constants[i].FilePath),
                        Location2 = GetRelativePath(_constants[j].FilePath),
                        Similarity = 1.0,
                        Reason = $"常量值相同: {_constants[i].Value} / Constant values are identical: {_constants[i].Value}"
                    });
                }
            }
        }
        
        return duplicates;
    }

    // 相似度计算方法 / Similarity calculation methods
    private double CalculateEnumSimilarity(EnumInfo enum1, EnumInfo enum2)
    {
        if (enum1.Members.Count == 0 && enum2.Members.Count == 0) return 0;
        
        var intersection = enum1.Members.Intersect(enum2.Members).Count();
        var union = enum1.Members.Union(enum2.Members).Count();
        
        return (double)intersection / union;
    }

    private double CalculateInterfaceSimilarity(InterfaceInfo interface1, InterfaceInfo interface2)
    {
        if (interface1.MethodSignatures.Count == 0 && interface2.MethodSignatures.Count == 0) return 0;
        
        var intersection = interface1.MethodSignatures.Intersect(interface2.MethodSignatures).Count();
        var union = interface1.MethodSignatures.Union(interface2.MethodSignatures).Count();
        
        return (double)intersection / union;
    }

    private double CalculateDtoSimilarity(DtoInfo dto1, DtoInfo dto2)
    {
        if (dto1.Properties.Count == 0 && dto2.Properties.Count == 0) return 0;
        
        var props1 = dto1.Properties.Select(p => $"{p.Type}:{p.Name}").ToList();
        var props2 = dto2.Properties.Select(p => $"{p.Type}:{p.Name}").ToList();
        
        var intersection = props1.Intersect(props2).Count();
        var union = props1.Union(props2).Count();
        
        return (double)intersection / union;
    }

    private double CalculateOptionsSimilarity(OptionsInfo options1, OptionsInfo options2)
    {
        return CalculateDtoSimilarity(
            new DtoInfo 
            { 
                Name = options1.Name, 
                FilePath = options1.FilePath, 
                Properties = options1.Properties, 
                Namespace = options1.Namespace 
            },
            new DtoInfo 
            { 
                Name = options2.Name, 
                FilePath = options2.FilePath, 
                Properties = options2.Properties, 
                Namespace = options2.Namespace 
            });
    }

    private double CalculateStaticClassSimilarity(StaticClassInfo class1, StaticClassInfo class2)
    {
        if (class1.MethodSignatures.Count == 0 && class2.MethodSignatures.Count == 0) return 0;
        
        var intersection = class1.MethodSignatures.Intersect(class2.MethodSignatures).Count();
        var union = class1.MethodSignatures.Union(class2.MethodSignatures).Count();
        
        return (double)intersection / union;
    }

    private string GetRelativePath(string fullPath)
    {
        return fullPath.Replace(_directoryPath, "").TrimStart('/', '\\');
    }
}
