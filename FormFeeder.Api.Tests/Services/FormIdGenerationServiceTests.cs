using FormFeeder.Api.Services;

namespace FormFeeder.Api.Tests.Services;

public sealed class FormIdGenerationServiceTests
{
    [Fact]
    public void GeneratePrivateFormId_ReturnsCorrectPrefix()
    {
        // Arrange
        var service = new FormIdGenerationService();

        // Act
        var result = service.GeneratePrivateFormId();

        // Assert
        result.Should().StartWith("prv-");
    }

    [Fact]
    public void GeneratePrivateFormId_ReturnsMinimumLength()
    {
        // Arrange
        var service = new FormIdGenerationService();

        // Act
        var result = service.GeneratePrivateFormId();

        // Assert
        result.Length.Should().BeGreaterOrEqualTo(10, "minimum 10 characters as per requirement");
        result.Should().HaveLength(11); // prv- (4) + 7 random chars = 11, meets minimum requirement
    }

    [Fact]
    public void GeneratePrivateFormId_GeneratesUniqueIds()
    {
        // Arrange
        var service = new FormIdGenerationService();
        var generatedIds = new HashSet<string>();

        // Act - Generate 1000 IDs to test uniqueness
        for (int i = 0; i < 1000; i++)
        {
            var id = service.GeneratePrivateFormId();
            generatedIds.Add(id);
        }

        // Assert
        generatedIds.Should().HaveCount(1000, "all generated IDs should be unique");
    }

    [Fact]
    public void GeneratePrivateFormId_OnlyContainsValidCharacters()
    {
        // Arrange
        var service = new FormIdGenerationService();
        var validChars = "abcdefghijklmnopqrstuvwxyz0123456789".ToHashSet();

        // Act
        var result = service.GeneratePrivateFormId();

        // Assert
        var hashPart = result[4..]; // Skip "prv-" prefix
        hashPart.Should().NotBeEmpty();
        hashPart.All(c => validChars.Contains(c)).Should().BeTrue();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public void GeneratePrivateFormId_ConsistentLength(int iterations)
    {
        // Arrange
        var service = new FormIdGenerationService();
        var expectedLength = 11; // Based on current implementation: prv- (4) + 7 random chars

        // Act & Assert
        for (int i = 0; i < iterations; i++)
        {
            var result = service.GeneratePrivateFormId();
            result.Should().HaveLength(expectedLength);
        }
    }
}
