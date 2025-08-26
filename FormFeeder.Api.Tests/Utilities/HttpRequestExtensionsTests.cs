using System.Net;

using FormFeeder.Api.Utilities;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace FormFeeder.Api.Tests.Utilities;

public class HttpRequestExtensionsTests
{
    public class GetClientIpAddress : HttpRequestExtensionsTests
    {
        [Fact]
        public void GetClientIpAddress_WithXForwardedFor_ShouldReturnFirstIp()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["X-Forwarded-For"] = "192.168.1.1, 10.0.0.1, 172.16.0.1";

            // Act
            var result = httpContext.Request.GetClientIpAddress();

            // Assert
            result.Should().Be("192.168.1.1");
        }

        [Fact]
        public void GetClientIpAddress_WithXForwardedForSingleIp_ShouldReturnThatIp()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["X-Forwarded-For"] = "203.0.113.195";

            // Act
            var result = httpContext.Request.GetClientIpAddress();

            // Assert
            result.Should().Be("203.0.113.195");
        }

        [Fact]
        public void GetClientIpAddress_WithXForwardedForWithSpaces_ShouldTrimSpaces()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["X-Forwarded-For"] = " 192.168.1.1 , 10.0.0.1 ";

            // Act
            var result = httpContext.Request.GetClientIpAddress();

            // Assert
            result.Should().Be("192.168.1.1");
        }

        [Fact]
        public void GetClientIpAddress_WithEmptyXForwardedFor_ShouldFallbackToXRealIp()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["X-Forwarded-For"] = string.Empty;
            httpContext.Request.Headers["X-Real-IP"] = "192.168.1.100";

            // Act
            var result = httpContext.Request.GetClientIpAddress();

            // Assert
            result.Should().Be("192.168.1.100");
        }

        [Fact]
        public void GetClientIpAddress_WithXRealIp_ShouldReturnXRealIp()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["X-Real-IP"] = "198.51.100.1";

            // Act
            var result = httpContext.Request.GetClientIpAddress();

            // Assert
            result.Should().Be("198.51.100.1");
        }

        [Fact]
        public void GetClientIpAddress_WithRemoteIpAddress_ShouldReturnRemoteIp()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.50");

            // Act
            var result = httpContext.Request.GetClientIpAddress();

            // Assert
            result.Should().Be("10.0.0.50");
        }

        [Fact]
        public void GetClientIpAddress_WithIPv6Address_ShouldReturnIPv6String()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse("2001:db8::1");

            // Act
            var result = httpContext.Request.GetClientIpAddress();

            // Assert
            result.Should().Be("2001:db8::1");
        }

        [Fact]
        public void GetClientIpAddress_WithLocalhostIPv4_ShouldReturnLoopback()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = IPAddress.Loopback; // 127.0.0.1

            // Act
            var result = httpContext.Request.GetClientIpAddress();

            // Assert
            result.Should().Be("127.0.0.1");
        }

        [Fact]
        public void GetClientIpAddress_WithLocalhostIPv6_ShouldReturnIPv6Loopback()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = IPAddress.IPv6Loopback; // ::1

            // Act
            var result = httpContext.Request.GetClientIpAddress();

            // Assert
            result.Should().Be("::1");
        }

        [Fact]
        public void GetClientIpAddress_WithNullRemoteIpAddress_ShouldReturnNull()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = null;

            // Act
            var result = httpContext.Request.GetClientIpAddress();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetClientIpAddress_WithNoHeaders_ShouldFallbackToRemoteIp()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse("172.16.0.100");

            // Act
            var result = httpContext.Request.GetClientIpAddress();

            // Assert
            result.Should().Be("172.16.0.100");
        }

        [Fact]
        public void GetClientIpAddress_PriorityOrder_XForwardedForTakesPrecedence()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["X-Forwarded-For"] = "192.168.1.1";
            httpContext.Request.Headers["X-Real-IP"] = "10.0.0.1";
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse("172.16.0.1");

            // Act
            var result = httpContext.Request.GetClientIpAddress();

            // Assert
            result.Should().Be("192.168.1.1");
        }

        [Fact]
        public void GetClientIpAddress_PriorityOrder_XRealIpTakesPrecedenceOverRemoteIp()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["X-Real-IP"] = "10.0.0.1";
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse("172.16.0.1");

            // Act
            var result = httpContext.Request.GetClientIpAddress();

            // Assert
            result.Should().Be("10.0.0.1");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(",")]
        [InlineData(",,")]
        public void GetClientIpAddress_WithInvalidXForwardedFor_ShouldFallbackToXRealIp(string invalidForwardedFor)
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["X-Forwarded-For"] = invalidForwardedFor;
            httpContext.Request.Headers["X-Real-IP"] = "192.168.1.1";

            // Act
            var result = httpContext.Request.GetClientIpAddress();

            // Assert
            result.Should().Be("192.168.1.1");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void GetClientIpAddress_WithEmptyXRealIp_ShouldFallbackToRemoteIp(string invalidRealIp)
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["X-Real-IP"] = invalidRealIp;
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");

            // Act
            var result = httpContext.Request.GetClientIpAddress();

            // Assert
            result.Should().Be("10.0.0.1");
        }

        [Fact]
        public void GetClientIpAddress_WithMultipleXForwardedForHeaders_ShouldHandleStringValues()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["X-Forwarded-For"] = new StringValues(["192.168.1.1", "10.0.0.1"]);

            // Act
            var result = httpContext.Request.GetClientIpAddress();

            // Assert
            // StringValues.FirstOrDefault() should return the first header value
            result.Should().Be("192.168.1.1");
        }

        [Fact]
        public void GetClientIpAddress_WithRealWorldScenario_CloudflareHeaders()
        {
            // Arrange - Simulate Cloudflare headers
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["X-Forwarded-For"] = "203.0.113.1, 192.168.1.1";
            httpContext.Request.Headers["X-Real-IP"] = "203.0.113.1";
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse("104.16.124.96"); // Cloudflare IP

            // Act
            var result = httpContext.Request.GetClientIpAddress();

            // Assert
            result.Should().Be("203.0.113.1"); // Should get the real client IP, not the proxy IP
        }

        [Fact]
        public void GetClientIpAddress_WithRealWorldScenario_AWSLoadBalancer()
        {
            // Arrange - Simulate AWS Application Load Balancer headers
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["X-Forwarded-For"] = "198.51.100.1, 10.0.1.100";
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse("10.0.1.100"); // ALB internal IP

            // Act
            var result = httpContext.Request.GetClientIpAddress();

            // Assert
            result.Should().Be("198.51.100.1"); // Should get the original client IP
        }

        [Fact]
        public void GetClientIpAddress_WithMalformedXForwardedFor_ShouldHandleGracefully()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["X-Forwarded-For"] = "not-an-ip, 192.168.1.1";
            httpContext.Request.Headers["X-Real-IP"] = "10.0.0.1";

            // Act
            var result = httpContext.Request.GetClientIpAddress();

            // Assert
            result.Should().Be("not-an-ip"); // Returns first value even if malformed (doesn't validate IP format)
        }

        [Fact]
        public void GetClientIpAddress_IsExtensionMethod_ShouldWorkOnHttpRequest()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
            var request = httpContext.Request;

            // Act
            var result = request.GetClientIpAddress();

            // Assert
            result.Should().Be("127.0.0.1");
        }
    }
}
