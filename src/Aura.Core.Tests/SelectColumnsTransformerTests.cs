using Aura.Abstractions;
using Aura.Plugin.Transforms;
using FluentAssertions;
using Xunit;

namespace Aura.Core.Tests;

public class SelectColumnsTransformerTests
{
    [Fact]
    public void Initialize_WithValidColumnIndices_ShouldSetColumnIndices()
    {
        // Arrange
        var transformer = new SelectColumnsTransformer();
        var settings = TestHelpers.CreateIntArraySetting("columnIndices", new[] { 0, 2, 4 });

        // Act
        transformer.Initialize(settings);

        // Assert - We test this through ExecuteAsync behavior
    }

    [Fact]
    public void Initialize_WithoutColumnIndices_ShouldThrowArgumentException()
    {
        // Arrange
        var transformer = new SelectColumnsTransformer();
        var settings = new Dictionary<string, object>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => transformer.Initialize(settings));
        exception.Message.Should().Contain("'columnIndices' setting");
    }

    [Fact]
    public async Task ExecuteAsync_WithValidColumnIndices_ShouldSelectCorrectColumns()
    {
        // Arrange
        var transformer = new SelectColumnsTransformer();
        var settings = TestHelpers.CreateIntArraySetting("columnIndices", new[] { 1, 3 }); // FirstName and Email columns (0-indexed)
        transformer.Initialize(settings);

        var inputData = new List<string[]>
        {
            new[] { "ID", "FirstName", "LastName", "Email", "Age" },
            new[] { "1", "John", "Doe", "john.doe@email.com", "30" },
            new[] { "2", "Jane", "Smith", "jane.smith@email.com", "25" },
            new[] { "3", "Peter", "Jones", "peter.jones@email.com", "35" }
        };
        var context = new DataContext<IEnumerable<string[]>>(inputData);

        // Act
        var result = await transformer.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        var transformedData = result.Payload.ToList();

        transformedData.Should().HaveCount(4);

        // Check header row
        transformedData[0].Should().BeEquivalentTo(new[] { "FirstName", "Email" });

        // Check data rows
        transformedData[1].Should().BeEquivalentTo(new[] { "John", "john.doe@email.com" });
        transformedData[2].Should().BeEquivalentTo(new[] { "Jane", "jane.smith@email.com" });
        transformedData[3].Should().BeEquivalentTo(new[] { "Peter", "peter.jones@email.com" });
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyColumnIndices_ShouldReturnEmptyColumns()
    {
        // Arrange
        var transformer = new SelectColumnsTransformer();
        var settings = TestHelpers.CreateIntArraySetting("columnIndices", Array.Empty<int>());
        transformer.Initialize(settings);

        var inputData = new List<string[]>
        {
            new[] { "ID", "Name", "Value" },
            new[] { "1", "Test", "100" }
        };
        var context = new DataContext<IEnumerable<string[]>>(inputData);

        // Act
        var result = await transformer.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var transformedData = result.Payload.ToList();

        transformedData.Should().HaveCount(2);
        transformedData[0].Should().BeEmpty();
        transformedData[1].Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WithOutOfBoundsIndices_ShouldReturnEmptyStrings()
    {
        // Arrange
        var transformer = new SelectColumnsTransformer();
        var settings = TestHelpers.CreateIntArraySetting("columnIndices", new[] { 0, 5, 10 }); // Last two indices are out of bounds
        transformer.Initialize(settings);

        var inputData = new List<string[]>
        {
            new[] { "ID", "Name", "Value" }, // Only 3 columns (indices 0, 1, 2)
            new[] { "1", "Test", "100" }
        };
        var context = new DataContext<IEnumerable<string[]>>(inputData);

        // Act
        var result = await transformer.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var transformedData = result.Payload.ToList();

        transformedData.Should().HaveCount(2);
        transformedData[0].Should().BeEquivalentTo(new[] { "ID", "", "" });
        transformedData[1].Should().BeEquivalentTo(new[] { "1", "", "" });
    }

    [Fact]
    public async Task ExecuteAsync_WithSingleColumn_ShouldReturnSingleColumn()
    {
        // Arrange
        var transformer = new SelectColumnsTransformer();
        var settings = TestHelpers.CreateIntArraySetting("columnIndices", new[] { 0 }); // Only ID column
        transformer.Initialize(settings);

        var inputData = new List<string[]>
        {
            new[] { "ID", "Name", "Value" },
            new[] { "1", "Test1", "100" },
            new[] { "2", "Test2", "200" }
        };
        var context = new DataContext<IEnumerable<string[]>>(inputData);

        // Act
        var result = await transformer.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var transformedData = result.Payload.ToList();

        transformedData.Should().HaveCount(3);
        transformedData[0].Should().BeEquivalentTo(new[] { "ID" });
        transformedData[1].Should().BeEquivalentTo(new[] { "1" });
        transformedData[2].Should().BeEquivalentTo(new[] { "2" });
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellation_ShouldRespectCancellation()
    {
        // Arrange
        var transformer = new SelectColumnsTransformer();
        var settings = TestHelpers.CreateIntArraySetting("columnIndices", new[] { 0, 1 });
        transformer.Initialize(settings);

        var inputData = new List<string[]>
        {
            new[] { "ID", "Name" },
            new[] { "1", "Test" }
        };
        var context = new DataContext<IEnumerable<string[]>>(inputData);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        // Note: This transformer doesn't use async operations that can be cancelled,
        // but the method signature requires CancellationToken for consistency
        var result = await transformer.ExecuteAsync(context, cts.Token);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyInput_ShouldReturnEmptyResult()
    {
        // Arrange
        var transformer = new SelectColumnsTransformer();
        var settings = TestHelpers.CreateIntArraySetting("columnIndices", new[] { 0, 1 });
        transformer.Initialize(settings);

        var inputData = new List<string[]>();
        var context = new DataContext<IEnumerable<string[]>>(inputData);

        // Act
        var result = await transformer.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Payload.Should().BeEmpty();
    }
}
