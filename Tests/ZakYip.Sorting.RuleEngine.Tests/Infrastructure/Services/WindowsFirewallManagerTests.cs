using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ZakYip.Sorting.RuleEngine.Infrastructure.Services;

namespace ZakYip.Sorting.RuleEngine.Tests.Infrastructure.Services
{
    /// <summary>
    /// WindowsFirewallManager单元测试
    /// Unit tests for WindowsFirewallManager
    /// </summary>
    public class WindowsFirewallManagerTests
    {
        private readonly Mock<ILogger<WindowsFirewallManager>> _mockLogger;
        private readonly WindowsFirewallManager _manager;

        public WindowsFirewallManagerTests()
        {
            _mockLogger = new Mock<ILogger<WindowsFirewallManager>>();
            _manager = new WindowsFirewallManager(_mockLogger.Object);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new WindowsFirewallManager(null));
        }

        [Theory]
        [InlineData(new string[] { "http://localhost:5000" }, new int[] { 5000 })]
        [InlineData(new string[] { "https://localhost:5001" }, new int[] { 5001 })]
        [InlineData(new string[] { "http://0.0.0.0:8080" }, new int[] { 8080 })]
        [InlineData(new string[] { "http://localhost:5000", "https://localhost:5001" }, new int[] { 5000, 5001 })]
        public void ExtractPortsFromUrls_ValidUrls_ReturnsCorrectPorts(string[] urls, int[] expectedPorts)
        {
            // Act
            var result = WindowsFirewallManager.ExtractPortsFromUrls(urls);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedPorts.Length, result.Count());
            Assert.True(expectedPorts.All(p => result.Contains(p)));
        }

        [Fact]
        public void ExtractPortsFromUrls_NullUrls_ReturnsEmptyCollection()
        {
            // Act
            var result = WindowsFirewallManager.ExtractPortsFromUrls(null);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void ExtractPortsFromUrls_EmptyUrls_ReturnsEmptyCollection()
        {
            // Act
            var result = WindowsFirewallManager.ExtractPortsFromUrls(Array.Empty<string>());

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Theory]
        [InlineData(new string[] { "http://localhost:80" }, new int[] { 80 })]
        [InlineData(new string[] { "https://localhost:443" }, new int[] { 443 })]
        [InlineData(new string[] { "http://localhost" }, new int[] { 80 })]
        [InlineData(new string[] { "https://localhost" }, new int[] { 443 })]
        public void ExtractPortsFromUrls_DefaultPorts_HandlesCorrectly(string[] urls, int[] expectedPorts)
        {
            // Act
            var result = WindowsFirewallManager.ExtractPortsFromUrls(urls);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedPorts.Length, result.Count());
            Assert.True(expectedPorts.All(p => result.Contains(p)));
        }

        [Fact]
        public void ExtractPortsFromUrls_InvalidUrls_IgnoresInvalidOnes()
        {
            // Arrange
            var urls = new[]
            {
                "http://localhost:5000",  // Valid
                "not-a-url",              // Invalid
                "https://localhost:5001"  // Valid
            };

            // Act
            var result = WindowsFirewallManager.ExtractPortsFromUrls(urls);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Contains(5000, result);
            Assert.Contains(5001, result);
        }

        [Fact]
        public void ExtractPortsFromUrls_DuplicatePorts_ReturnsUniquePorts()
        {
            // Arrange
            var urls = new[]
            {
                "http://localhost:5000",
                "http://0.0.0.0:5000",
                "http://127.0.0.1:5000"
            };

            // Act
            var result = WindowsFirewallManager.ExtractPortsFromUrls(urls);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Contains(5000, result);
        }

        [Fact]
        public void ExtractPortsFromUrls_MultiplePortsInDifferentUrls_ReturnsAllUniquePorts()
        {
            // Arrange
            var urls = new[]
            {
                "http://localhost:5000",
                "https://localhost:5001",
                "http://0.0.0.0:8080",
                "https://example.com:443"
            };

            // Act
            var result = WindowsFirewallManager.ExtractPortsFromUrls(urls);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result.Count());
            Assert.Contains(5000, result);
            Assert.Contains(5001, result);
            Assert.Contains(8080, result);
            Assert.Contains(443, result);
        }
    }
}
