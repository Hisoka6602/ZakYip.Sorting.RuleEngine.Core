using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.ApiCommunicationLogs;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.LiteDb;
using ZakYip.Sorting.RuleEngine.Infrastructure.Services;

namespace ZakYip.Sorting.RuleEngine.Tests.Integration;

/// <summary>
/// Integration tests to verify dependency injection configuration
/// </summary>
public class DependencyInjectionTests
{
    [Fact]
    public void ServiceProvider_CanResolve_IPerformanceMetricRepository()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Add LiteDB (in-memory for testing)
        services.AddSingleton<LiteDB.ILiteDatabase>(sp => 
            new LiteDB.LiteDatabase(new System.IO.MemoryStream()));
        
        // Add required dependencies
        services.AddSingleton<ISystemClock, SystemClock>();
        
        // Register repository
        services.AddScoped<IPerformanceMetricRepository, LiteDbPerformanceMetricRepository>();
        
        var serviceProvider = services.BuildServiceProvider();
        
        // Act
        var repository = serviceProvider.GetService<IPerformanceMetricRepository>();
        
        // Assert
        Assert.NotNull(repository);
    }
    
    [Fact]
    public void ServiceProvider_CanResolve_IDataAnalysisService()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Add LiteDB (in-memory for testing)
        services.AddSingleton<LiteDB.ILiteDatabase>(sp => 
            new LiteDB.LiteDatabase(new System.IO.MemoryStream()));
        
        // Add logging
        services.AddLogging();
        
        services.AddSingleton<ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock, ZakYip.Sorting.RuleEngine.Infrastructure.Services.SystemClock>();
        
        // Register repositories
        services.AddScoped<IChuteRepository, LiteDbChuteRepository>();
        services.AddScoped<IPerformanceMetricRepository, LiteDbPerformanceMetricRepository>();
        
        // Register optional DbContext as null (for testing without database)
        // These are optional dependencies in DataAnalysisService constructor
        
        // Register service (merged service that includes ChuteStatistics, GanttChart functionality)
        services.AddScoped<IDataAnalysisService>(sp => 
            new DataAnalysisService(
                sp.GetRequiredService<IServiceScopeFactory>(),
                null, // MySqlLogDbContext (optional)
                null, // SqliteLogDbContext (optional)
                sp.GetRequiredService<ILogger<DataAnalysisService>>(),
                sp.GetRequiredService<ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock>()
            ));
        
        var serviceProvider = services.BuildServiceProvider();
        
        // Act
        var service = serviceProvider.GetService<IDataAnalysisService>();
        
        // Assert
        Assert.NotNull(service);
    }
    
    [Fact]
    public async Task PerformanceMetricRepository_CanRecordAndRetrieve_Metrics()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<LiteDB.ILiteDatabase>(sp => 
            new LiteDB.LiteDatabase(new System.IO.MemoryStream()));
        services.AddSingleton<ISystemClock, SystemClock>();
        services.AddScoped<IPerformanceMetricRepository, LiteDbPerformanceMetricRepository>();
        
        var serviceProvider = services.BuildServiceProvider();
        var repository = serviceProvider.GetRequiredService<IPerformanceMetricRepository>();
        
        var metric = new ZakYip.Sorting.RuleEngine.Domain.Entities.PerformanceMetric
        {
            OperationName = "Test_Operation",
            ParcelId = "TEST123",
            DurationMs = 100,
            Success = true,
            RecordedAt = DateTime.Now
        };
        
        // Act
        await repository.RecordMetricAsync(metric);
        
        var startTime = DateTime.Now.AddHours(-1);
        var endTime = DateTime.Now.AddHours(1);
        var metrics = await repository.GetMetricsAsync(startTime, endTime, "Test_Operation");
        
        // Assert
        Assert.NotNull(metrics);
        Assert.Contains(metrics, m => m.ParcelId == "TEST123");
    }
    
    [Fact]
    public void ServiceProvider_CanResolve_IApiCommunicationLogRepository()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging();
        
        // Register repository with optional DbContexts (null for testing)
        services.AddScoped<IApiCommunicationLogRepository>(sp => 
            new ApiCommunicationLogRepository(
                sp.GetRequiredService<ILogger<ApiCommunicationLogRepository>>(),
                null, // MySqlLogDbContext (optional)
                null  // SqliteLogDbContext (optional)
            ));
        
        var serviceProvider = services.BuildServiceProvider();
        
        // Act
        var repository = serviceProvider.GetService<IApiCommunicationLogRepository>();
        
        // Assert
        Assert.NotNull(repository);
    }
}
