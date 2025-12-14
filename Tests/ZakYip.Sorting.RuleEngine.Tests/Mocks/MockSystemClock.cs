using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Tests.Mocks;

public class MockSystemClock : ISystemClock
{
    private readonly DateTime _fixedTime;
    public MockSystemClock() : this(new DateTime(2025, 1, 1, 12, 0, 0)) { }
    public MockSystemClock(DateTime fixedTime) { _fixedTime = fixedTime; }
    public DateTime LocalNow => _fixedTime;
    public DateTime UtcNow => _fixedTime.ToUniversalTime();
}
