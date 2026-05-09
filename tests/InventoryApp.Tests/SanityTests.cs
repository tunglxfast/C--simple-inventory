using FluentAssertions;
using InventoryApp.Core.Enums;
using Xunit;

namespace InventoryApp.Tests;

public class SanityTests
{
    [Fact]
    public void DocumentStatus_ShouldContainDraft()
    {
        Enum.IsDefined(typeof(DocumentStatus), DocumentStatus.Draft).Should().BeTrue();
    }
}
