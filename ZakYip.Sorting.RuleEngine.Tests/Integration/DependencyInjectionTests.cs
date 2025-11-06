using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
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
        
        // Register repositories
        services.AddScoped<IChuteRepository, LiteDbChuteRepository>();
        services.AddScoped<IPerformanceMetricRepository, LiteDbPerformanceMetricRepository>();
        
        // Register service (merged service that includes ChuteStatistics, GanttChart functionality)
        services.AddScoped<IDataAnalysisService, DataAnalysisService>();
        
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
}
