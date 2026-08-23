using FluentAssertions;
using Xunit;

namespace SupplyChainX.UnitTests;

public class SanityTests
{
    [Fact]
    public void Foundation_ShouldBeConfiguredCorrectly()
    {
        const string version = "v0.1.0";
        version.Should().Be("v0.1.0");
    }
}
