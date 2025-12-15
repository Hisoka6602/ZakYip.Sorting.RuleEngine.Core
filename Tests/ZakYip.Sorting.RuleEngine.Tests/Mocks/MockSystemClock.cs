using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Tests.Mocks;

public class MockSystemClock : ISystemClock
{
    private readonly DateTime _fixedTime;
    public MockSystemClock() : this(new DateTime(2025, 1, 1, 12, 0, 0)) { }
    public MockSystemClock(DateTime fixedTime) { _fixedTime = fixedTime; }
    public DateTime LocalNow => _fixedTime;
#pragma warning disable RS0030 // Banned API - This is a mock for testing purposes
    public DateTime UtcNow => _fixedTime.ToUniversalTime();
#pragma warning restore RS0030
}
