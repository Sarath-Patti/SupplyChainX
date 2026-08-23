using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SupplyChainX.Api.Middleware;
using Xunit;

namespace SupplyChainX.UnitTests.Middleware;

public class GlobalExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenExceptionOccurs_ShouldReturnProblemDetailsJson()
    {
        // Arrange
        var logger = Substitute.For<ILogger<GlobalExceptionHandlingMiddleware>>();
        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns("Production");

        RequestDelegate next = _ => throw new InvalidOperationException("Test database failure");
        var middleware = new GlobalExceptionHandlingMiddleware(next, logger, env);

        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        httpContext.Request.Path = "/api/v1/test";
        httpContext.Request.Method = "GET";

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        httpContext.Response.ContentType.Should().Be("application/problem+json");

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(httpContext.Response.Body);
        var responseText = await reader.ReadToEndAsync();

        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseText, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be(500);
        problemDetails.Title.Should().Be("An unexpected error occurred while processing your request.");
        problemDetails.Detail.Should().Be("A server error occurred. Please contact support if the issue persists.");
    }
}
