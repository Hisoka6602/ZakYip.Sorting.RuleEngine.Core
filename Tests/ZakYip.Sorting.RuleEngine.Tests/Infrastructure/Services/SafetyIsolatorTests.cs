using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ZakYip.Sorting.RuleEngine.Infrastructure.Services;

namespace ZakYip.Sorting.RuleEngine.Tests.Infrastructure.Services
{
    /// <summary>
    /// SafetyIsolator单元测试
    /// Unit tests for SafetyIsolator
    /// </summary>
    public class SafetyIsolatorTests
    {
        private readonly Mock<ILogger> _mockLogger;
        private readonly SafetyIsolator _isolator;

        public SafetyIsolatorTests()
        {
            _mockLogger = new Mock<ILogger>();
            _isolator = new SafetyIsolator(_mockLogger.Object);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SafetyIsolator(null));
        }

        [Fact]
        public void Execute_Action_SuccessfulExecution_ReturnsTrue()
        {
            // Arrange
            var executed = false;
            Action action = () => executed = true;

            // Act
            var result = _isolator.Execute(action, "test operation");

            // Assert
            Assert.True(result);
            Assert.True(executed);
        }

        [Fact]
        public void Execute_Action_ThrowsException_ReturnsFalse()
        {
            // Arrange
            Action action = () => throw new InvalidOperationException("Test exception");

            // Act
            var result = _isolator.Execute(action, "test operation");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Execute_Func_SuccessfulExecution_ReturnsResult()
        {
            // Arrange
            Func<int> func = () => 42;

            // Act
            var result = _isolator.Execute(func, "test operation", 0);

            // Assert
            Assert.Equal(42, result);
        }

        [Fact]
        public void Execute_Func_ThrowsException_ReturnsDefaultValue()
        {
            // Arrange
            Func<int> func = () => throw new InvalidOperationException("Test exception");

            // Act
            var result = _isolator.Execute(func, "test operation", -1);

            // Assert
            Assert.Equal(-1, result);
        }

        [Fact]
        public void TryExecute_SuccessfulExecution_ReturnsSuccessAndResult()
        {
            // Arrange
            Func<string> func = () => "success";

            // Act
            var (success, result) = _isolator.TryExecute(func, "test operation", "default");

            // Assert
            Assert.True(success);
            Assert.Equal("success", result);
        }

        [Fact]
        public void TryExecute_ThrowsException_ReturnsFailureAndDefaultValue()
        {
            // Arrange
            Func<string> func = () => throw new InvalidOperationException("Test exception");

            // Act
            var (success, result) = _isolator.TryExecute(func, "test operation", "default");

            // Assert
            Assert.False(success);
            Assert.Equal("default", result);
        }

        [Fact]
        public void ExecuteSilent_Action_SuccessfulExecution_ReturnsTrue()
        {
            // Arrange
            var executed = false;
            Action action = () => executed = true;

            // Act
            var result = _isolator.ExecuteSilent(action);

            // Assert
            Assert.True(result);
            Assert.True(executed);
        }

        [Fact]
        public void ExecuteSilent_Action_ThrowsException_ReturnsFalseWithoutLogging()
        {
            // Arrange
            Action action = () => throw new InvalidOperationException("Test exception");

            // Act
            var result = _isolator.ExecuteSilent(action);

            // Assert
            Assert.False(result);
            // Verify no logging occurred
            _mockLogger.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Never);
        }

        [Fact]
        public void ExecuteSilent_Func_SuccessfulExecution_ReturnsResult()
        {
            // Arrange
            Func<int> func = () => 100;

            // Act
            var result = _isolator.ExecuteSilent(func, 0);

            // Assert
            Assert.Equal(100, result);
        }

        [Fact]
        public void ExecuteSilent_Func_ThrowsException_ReturnsDefaultValueWithoutLogging()
        {
            // Arrange
            Func<int> func = () => throw new InvalidOperationException("Test exception");

            // Act
            var result = _isolator.ExecuteSilent(func, -999);

            // Assert
            Assert.Equal(-999, result);
            // Verify no logging occurred
            _mockLogger.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Never);
        }

        [Fact]
        public void Execute_WithNullAction_ReturnsTrue()
        {
            // Act
            var result = _isolator.Execute((Action)null, "test operation");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Execute_WithNullFunc_ReturnsDefaultValue()
        {
            // Act
            var result = _isolator.Execute((Func<int>)null, "test operation", 42);

            // Assert
            Assert.Equal(42, result);
        }

        #region Async Tests

        [Fact]
        public async Task ExecuteAsync_Action_SuccessfulExecution_ReturnsTrue()
        {
            // Arrange
            var executed = false;
            Func<Task> action = async () =>
            {
                await Task.Delay(10);
                executed = true;
            };

            // Act
            var result = await _isolator.ExecuteAsync(action, "test async operation");

            // Assert
            Assert.True(result);
            Assert.True(executed);
        }

        [Fact]
        public async Task ExecuteAsync_Action_ThrowsException_ReturnsFalse()
        {
            // Arrange
            Func<Task> action = async () =>
            {
                await Task.Delay(10);
                throw new InvalidOperationException("Test async exception");
            };

            // Act
            var result = await _isolator.ExecuteAsync(action, "test async operation");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ExecuteAsync_Func_SuccessfulExecution_ReturnsResult()
        {
            // Arrange
            Func<Task<int>> func = async () =>
            {
                await Task.Delay(10);
                return 42;
            };

            // Act
            var result = await _isolator.ExecuteAsync(func, "test async operation", 0);

            // Assert
            Assert.Equal(42, result);
        }

        [Fact]
        public async Task ExecuteAsync_Func_ThrowsException_ReturnsDefaultValue()
        {
            // Arrange
            Func<Task<int>> func = async () =>
            {
                await Task.Delay(10);
                throw new InvalidOperationException("Test async exception");
            };

            // Act
            var result = await _isolator.ExecuteAsync(func, "test async operation", -1);

            // Assert
            Assert.Equal(-1, result);
        }

        [Fact]
        public async Task TryExecuteAsync_SuccessfulExecution_ReturnsSuccessAndResult()
        {
            // Arrange
            Func<Task<string>> func = async () =>
            {
                await Task.Delay(10);
                return "success";
            };

            // Act
            var (success, result) = await _isolator.TryExecuteAsync(func, "test async operation", "default");

            // Assert
            Assert.True(success);
            Assert.Equal("success", result);
        }

        [Fact]
        public async Task TryExecuteAsync_ThrowsException_ReturnsFailureAndDefaultValue()
        {
            // Arrange
            Func<Task<string>> func = async () =>
            {
                await Task.Delay(10);
                throw new InvalidOperationException("Test async exception");
            };

            // Act
            var (success, result) = await _isolator.TryExecuteAsync(func, "test async operation", "default");

            // Assert
            Assert.False(success);
            Assert.Equal("default", result);
        }

        [Fact]
        public async Task ExecuteSilentAsync_Action_SuccessfulExecution_ReturnsTrue()
        {
            // Arrange
            var executed = false;
            Func<Task> action = async () =>
            {
                await Task.Delay(10);
                executed = true;
            };

            // Act
            var result = await _isolator.ExecuteSilentAsync(action);

            // Assert
            Assert.True(result);
            Assert.True(executed);
        }

        [Fact]
        public async Task ExecuteSilentAsync_Action_ThrowsException_ReturnsFalseWithoutLogging()
        {
            // Arrange
            Func<Task> action = async () =>
            {
                await Task.Delay(10);
                throw new InvalidOperationException("Test async exception");
            };

            // Act
            var result = await _isolator.ExecuteSilentAsync(action);

            // Assert
            Assert.False(result);
            
            // Verify no error was logged (silent execution)
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
        }

        [Fact]
        public async Task ExecuteSilentAsync_Func_SuccessfulExecution_ReturnsResult()
        {
            // Arrange
            Func<Task<int>> func = async () =>
            {
                await Task.Delay(10);
                return 42;
            };

            // Act
            var result = await _isolator.ExecuteSilentAsync(func, 0);

            // Assert
            Assert.Equal(42, result);
        }

        [Fact]
        public async Task ExecuteSilentAsync_Func_ThrowsException_ReturnsDefaultValueWithoutLogging()
        {
            // Arrange
            Func<Task<int>> func = async () =>
            {
                await Task.Delay(10);
                throw new InvalidOperationException("Test async exception");
            };

            // Act
            var result = await _isolator.ExecuteSilentAsync(func, -999);

            // Assert
            Assert.Equal(-999, result);
            
            // Verify no error was logged (silent execution)
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
        }

        #endregion
    }
}
