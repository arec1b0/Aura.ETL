using Aura.Abstractions;
using Aura.Plugin.Transforms;
using FluentAssertions;
using Xunit;

namespace Aura.Core.Tests;

public class ConsoleDataSinkTests
{
    [Fact]
    public async Task ExecuteAsync_WithValidData_ShouldOutputToConsole()
    {
        // Arrange
        var sink = new ConsoleDataSink();
        var inputData = new List<string[]>
        {
            new[] { "Name", "Age", "City" },
            new[] { "John", "30", "New York" },
            new[] { "Jane", "25", "London" }
        };
        var context = new DataContext<IEnumerable<string[]>>(inputData);

        // Capture console output
        using var stringWriter = new StringWriter();
        var originalOutput = Console.Out;
        Console.SetOut(stringWriter);

        try
        {
            // Act
            var result = await sink.ExecuteAsync(context, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Payload.Should().NotBeNull();

            var output = stringWriter.ToString();
            output.Should().Contain("--- Pipeline Result ---");
            output.Should().Contain("Name | Age | City");
            output.Should().Contain("John | 30 | New York");
            output.Should().Contain("Jane | 25 | London");
            output.Should().Contain("-----------------------");
        }
        finally
        {
            // Restore original console output
            Console.SetOut(originalOutput);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyData_ShouldOutputHeadersOnly()
    {
        // Arrange
        var sink = new ConsoleDataSink();
        var inputData = new List<string[]>
        {
            new[] { "ID", "Name", "Value" }
        };
        var context = new DataContext<IEnumerable<string[]>>(inputData);

        // Capture console output
        using var stringWriter = new StringWriter();
        var originalOutput = Console.Out;
        Console.SetOut(stringWriter);

        try
        {
            // Act
            var result = await sink.ExecuteAsync(context, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();

            var output = stringWriter.ToString();
            output.Should().Contain("--- Pipeline Result ---");
            output.Should().Contain("ID | Name | Value");
            output.Should().Contain("-----------------------");
        }
        finally
        {
            // Restore original console output
            Console.SetOut(originalOutput);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithSingleRow_ShouldOutputCorrectly()
    {
        // Arrange
        var sink = new ConsoleDataSink();
        var inputData = new List<string[]>
        {
            new[] { "Message" },
            new[] { "Hello World" }
        };
        var context = new DataContext<IEnumerable<string[]>>(inputData);

        // Capture console output
        using var stringWriter = new StringWriter();
        var originalOutput = Console.Out;
        Console.SetOut(stringWriter);

        try
        {
            // Act
            var result = await sink.ExecuteAsync(context, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();

            var output = stringWriter.ToString();
            output.Should().Contain("Message");
            output.Should().Contain("Hello World");
        }
        finally
        {
            // Restore original console output
            Console.SetOut(originalOutput);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellation_ShouldStillComplete()
    {
        // Arrange
        var sink = new ConsoleDataSink();
        var inputData = new List<string[]>
        {
            new[] { "Name" },
            new[] { "Test" }
        };
        var context = new DataContext<IEnumerable<string[]>>(inputData);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Capture console output
        using var stringWriter = new StringWriter();
        var originalOutput = Console.Out;
        Console.SetOut(stringWriter);

        try
        {
            // Act
            var result = await sink.ExecuteAsync(context, cts.Token);

            // Assert
            result.Should().NotBeNull();

            var output = stringWriter.ToString();
            output.Should().Contain("Test");
        }
        finally
        {
            // Restore original console output
            Console.SetOut(originalOutput);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var sink = new ConsoleDataSink();
        var inputData = new List<string[]>
        {
            new[] { "Name", "Description" },
            new[] { "Test@#$%", "Value with spaces & symbols!" }
        };
        var context = new DataContext<IEnumerable<string[]>>(inputData);

        // Capture console output
        using var stringWriter = new StringWriter();
        var originalOutput = Console.Out;
        Console.SetOut(stringWriter);

        try
        {
            // Act
            var result = await sink.ExecuteAsync(context, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();

            var output = stringWriter.ToString();
            output.Should().Contain("Test@#$%");
            output.Should().Contain("Value with spaces & symbols!");
        }
        finally
        {
            // Restore original console output
            Console.SetOut(originalOutput);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyStrings_ShouldHandleCorrectly()
    {
        // Arrange
        var sink = new ConsoleDataSink();
        var inputData = new List<string[]>
        {
            new[] { "A", "B", "C" },
            new[] { "", "value", "" }
        };
        var context = new DataContext<IEnumerable<string[]>>(inputData);

        // Capture console output
        using var stringWriter = new StringWriter();
        var originalOutput = Console.Out;
        Console.SetOut(stringWriter);

        try
        {
            // Act
            var result = await sink.ExecuteAsync(context, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();

            var output = stringWriter.ToString();
            output.Should().Contain(" | value | ");
        }
        finally
        {
            // Restore original console output
            Console.SetOut(originalOutput);
        }
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsEmptyContext()
    {
        // Arrange
        var sink = new ConsoleDataSink();
        var inputData = new List<string[]>
        {
            new[] { "Test" }
        };
        var context = new DataContext<IEnumerable<string[]>>(inputData);

        // Act
        var result = await sink.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Payload.Should().BeOfType<object>();
    }
}
