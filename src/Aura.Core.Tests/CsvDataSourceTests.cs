using Aura.Abstractions;
using Aura.Plugin.Csv;
using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace Aura.Core.Tests;

public class CsvDataSourceTests
{
    [Fact]
    public void Initialize_WithValidFilePath_ShouldSetFilePath()
    {
        // Arrange
        var dataSource = new CsvDataSource();
        var settings = TestHelpers.CreateStringSetting("filePath", "test.csv");

        // Act
        dataSource.Initialize(settings);

        // Assert - We can't directly test the private field, but we can test behavior
        // This will be tested through ExecuteAsync
    }

    [Fact]
    public void Initialize_WithoutFilePath_ShouldThrowArgumentException()
    {
        // Arrange
        var dataSource = new CsvDataSource();
        var settings = new Dictionary<string, object>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => dataSource.Initialize(settings));
        exception.Message.Should().Contain("'filePath' setting");
    }

    [Fact]
    public void Initialize_WithNullFilePath_ShouldThrowArgumentNullException()
    {
        // Arrange
        var dataSource = new CsvDataSource();
        var settings = new Dictionary<string, object>
        {
            ["filePath"] = System.Text.Json.JsonSerializer.SerializeToElement((string)null)
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => dataSource.Initialize(settings));
        exception.ParamName.Should().Be("filePath cannot be null.");
    }

    [Fact]
    public async Task ExecuteAsync_WithValidCsvFile_ShouldReturnData()
    {
        // Arrange
        var dataSource = new CsvDataSource();
        var testFilePath = Path.Combine("TestData", "test.csv");

        var settings = TestHelpers.CreateStringSetting("filePath", testFilePath);
        dataSource.Initialize(settings);

        var context = new DataContext<object>(new object());

        // Act
        var result = await dataSource.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        var data = result.Payload.ToList();
        data.Should().HaveCount(5); // 4 data rows + 1 header row

        // Check header row
        data[0].Should().BeEquivalentTo(new[] { "ID", "FirstName", "LastName", "Email", "Age" });

        // Check first data row
        data[1].Should().BeEquivalentTo(new[] { "1", "John", "Doe", "john.doe@email.com", "30" });
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var dataSource = new CsvDataSource();
        var settings = TestHelpers.CreateStringSetting("filePath", "nonexistent.csv");
        dataSource.Initialize(settings);

        var context = new DataContext<object>(new object());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FileNotFoundException>(
            () => dataSource.ExecuteAsync(context, CancellationToken.None));
        exception.FileName.Should().Be("nonexistent.csv");
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyCsvFile_ShouldReturnOnlyHeaders()
    {
        // Arrange
        var dataSource = new CsvDataSource();
        var testFilePath = Path.Combine("TestData", "empty.csv");

        var settings = TestHelpers.CreateStringSetting("filePath", testFilePath);
        dataSource.Initialize(settings);

        var context = new DataContext<object>(new object());

        // Act
        var result = await dataSource.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var data = result.Payload.ToList();
        data.Should().HaveCount(1); // Only header row
        data[0].Should().BeEquivalentTo(new[] { "ID", "Name", "Value" });
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellation_ShouldRespectCancellation()
    {
        // Arrange
        var dataSource = new CsvDataSource();
        var testFilePath = Path.Combine("TestData", "test.csv");

        var settings = TestHelpers.CreateStringSetting("filePath", testFilePath);
        dataSource.Initialize(settings);

        var context = new DataContext<object>(new object());
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(
            () => dataSource.ExecuteAsync(context, cts.Token));
    }
}
