var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Interface Simulator API",
        Version = "v1",
        Description = "接口模拟器 - 随机返回接口ID（1-50）\nInterface Simulator - Returns random interface ID (1-50)"
    });
});

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Interface Simulator API V1");
    c.RoutePrefix = string.Empty; // Set Swagger UI at root
});

app.UseCors();

/// <summary>
/// 获取随机接口ID
/// Get random interface ID
/// </summary>
app.MapGet("/api/interface/random", () =>
{
    try
    {
        var interfaceId = Random.Shared.Next(1, 51); // 1-50
        return Results.Ok(new InterfaceResponse
        {
            InterfaceId = interfaceId,
            Timestamp = DateTime.Now,
            Success = true,
            Message = $"接口ID: {interfaceId}"
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Error generating interface ID",
            detail: ex.Message,
            statusCode: 500
        );
    }
})
.WithName("GetRandomInterfaceId")
.WithOpenApi()
.Produces<InterfaceResponse>(200)
.Produces(500)
.WithTags("Interface")
.WithSummary("获取随机接口ID (Get random interface ID)")
.WithDescription("返回1-50之间的随机接口ID (Returns a random interface ID between 1 and 50)");

/// <summary>
/// 批量获取随机接口ID
/// Get batch of random interface IDs
/// </summary>
app.MapGet("/api/interface/random/batch", (int count = 10) =>
{
    try
    {
        if (count < 1 || count > 100)
        {
            return Results.BadRequest(new
            {
                Success = false,
                Message = "数量必须在1-100之间 (Count must be between 1 and 100)"
            });
        }

        var interfaceIds = Enumerable.Range(0, count)
            .Select(_ => Random.Shared.Next(1, 51))
            .ToList();

        return Results.Ok(new BatchInterfaceResponse
        {
            InterfaceIds = interfaceIds,
            Count = count,
            Timestamp = DateTime.Now,
            Success = true,
            Message = $"生成了 {count} 个接口ID"
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Error generating interface IDs",
            detail: ex.Message,
            statusCode: 500
        );
    }
})
.WithName("GetBatchRandomInterfaceIds")
.WithOpenApi()
.Produces<BatchInterfaceResponse>(200)
.Produces(400)
.Produces(500)
.WithTags("Interface")
.WithSummary("批量获取随机接口ID (Get batch of random interface IDs)")
.WithDescription("批量返回1-50之间的随机接口ID，最多100个 (Returns up to 100 random interface IDs between 1 and 50)");

/// <summary>
/// 健康检查
/// Health check
/// </summary>
app.MapGet("/api/health", () =>
{
    return Results.Ok(new
    {
        Status = "Healthy",
        Service = "Interface Simulator",
        Timestamp = DateTime.Now,
        Version = "1.0.0"
    });
})
.WithName("HealthCheck")
.WithOpenApi()
.WithTags("Health")
.WithSummary("健康检查 (Health Check)")
.WithDescription("检查服务是否正常运行 (Check if the service is running properly)");

app.Run();

/// <summary>
/// 接口响应
/// Interface response
/// </summary>
record InterfaceResponse
{
    /// <summary>
    /// 接口ID (1-50)
    /// Interface ID (1-50)
    /// </summary>
    public int InterfaceId { get; set; }

    /// <summary>
    /// 时间戳
    /// Timestamp
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// 是否成功
    /// Success status
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 消息
    /// Message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 批量接口响应
/// Batch interface response
/// </summary>
record BatchInterfaceResponse
{
    /// <summary>
    /// 接口ID列表
    /// List of interface IDs
    /// </summary>
    public List<int> InterfaceIds { get; set; } = new();

    /// <summary>
    /// 数量
    /// Count
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// 时间戳
    /// Timestamp
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// 是否成功
    /// Success status
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 消息
    /// Message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

