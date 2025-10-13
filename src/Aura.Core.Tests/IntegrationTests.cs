using Aura.Core.Models;
using Aura.Core.Services;
using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace Aura.Core.Tests;

public class IntegrationTests
{
    [Fact]
    public async Task FullPipeline_WithCsvDataSourceAndConsoleSink_ShouldExecuteSuccessfully()
    {
        // Arrange
        var testCsvPath = Path.Combine("TestData", "test.csv");

        var config = new PipelineConfiguration
        {
            Steps = new List<StepConfiguration>
            {
                new StepConfiguration
                {
                    Type = "Aura.Plugin.Csv.CsvDataSource, Aura.Plugin.Csv",
                    Settings = TestHelpers.CreateStringSetting("filePath", testCsvPath)
                },
                new StepConfiguration
                {
                    Type = "Aura.Plugin.Transforms.ConsoleDataSink, Aura.Plugin.Transforms",
                    Settings = new Dictionary<string, object>()
                }
            }
        };

        var stepFactory = new StepFactory();
        var orchestrator = new PipelineOrchestrator(stepFactory);

        // Capture console output to verify the pipeline executed correctly
        using var stringWriter = new StringWriter();
        var originalOutput = Console.Out;
        Console.SetOut(stringWriter);

        try
        {
            // Act
            await orchestrator.ExecuteAsync(config, CancellationToken.None);

            // Assert
            var output = stringWriter.ToString();
            output.Should().Contain("Executing step: Aura.Plugin.Csv.CsvDataSource, Aura.Plugin.Csv");
            output.Should().Contain("Step Aura.Plugin.Csv.CsvDataSource, Aura.Plugin.Csv completed.");
            output.Should().Contain("Executing step: Aura.Plugin.Transforms.ConsoleDataSink, Aura.Plugin.Transforms");
            output.Should().Contain("Step Aura.Plugin.Transforms.ConsoleDataSink, Aura.Plugin.Transforms completed.");
            output.Should().Contain("Pipeline execution finished successfully.");
            output.Should().Contain("--- Pipeline Result ---");
            output.Should().Contain("ID | FirstName | LastName | Email | Age");
            output.Should().Contain("1 | John | Doe | john.doe@email.com | 30");
            output.Should().Contain("2 | Jane | Smith | jane.smith@email.com | 25");
            output.Should().Contain("-----------------------");
        }
        finally
        {
            // Restore original console output
            Console.SetOut(originalOutput);
        }
    }

    [Fact]
    public async Task FullPipeline_WithColumnSelection_ShouldTransformDataCorrectly()
    {
        // Arrange
        var testCsvPath = Path.Combine("TestData", "test.csv");

        var config = new PipelineConfiguration
        {
            Steps = new List<StepConfiguration>
            {
                new StepConfiguration
                {
                    Type = "Aura.Plugin.Csv.CsvDataSource, Aura.Plugin.Csv",
                    Settings = TestHelpers.CreateStringSetting("filePath", testCsvPath)
                },
                new StepConfiguration
                {
                    Type = "Aura.Plugin.Transforms.SelectColumnsTransformer, Aura.Plugin.Transforms",
                    Settings = TestHelpers.CreateIntArraySetting("columnIndices", new[] { 1, 3 }) // FirstName and Email columns
                },
                new StepConfiguration
                {
                    Type = "Aura.Plugin.Transforms.ConsoleDataSink, Aura.Plugin.Transforms",
                    Settings = new Dictionary<string, object>()
                }
            }
        };

        var stepFactory = new StepFactory();
        var orchestrator = new PipelineOrchestrator(stepFactory);

        // Capture console output
        using var stringWriter = new StringWriter();
        var originalOutput = Console.Out;
        Console.SetOut(stringWriter);

        try
        {
            // Act
            await orchestrator.ExecuteAsync(config, CancellationToken.None);

            // Assert
            var output = stringWriter.ToString();
            output.Should().Contain("Pipeline execution finished successfully.");
            output.Should().Contain("--- Pipeline Result ---");
            output.Should().Contain("FirstName | Email");
            output.Should().Contain("John | john.doe@email.com");
            output.Should().Contain("Jane | jane.smith@email.com");
            output.Should().Contain("Peter | peter.jones@email.com");
            output.Should().Contain("Alice | alice.brown@email.com");
            output.Should().NotContain("LastName");
            output.Should().NotContain("Age");
        }
        finally
        {
            // Restore original console output
            Console.SetOut(originalOutput);
        }
    }

    [Fact]
    public async Task Pipeline_WithInvalidCsvPath_ShouldThrowException()
    {
        // Arrange
        var config = new PipelineConfiguration
        {
            Steps = new List<StepConfiguration>
            {
                new StepConfiguration
                {
                    Type = "Aura.Plugin.Csv.CsvDataSource, Aura.Plugin.Csv",
                    Settings = TestHelpers.CreateStringSetting("filePath", "nonexistent.csv")
                }
            }
        };

        var stepFactory = new StepFactory();
        var orchestrator = new PipelineOrchestrator(stepFactory);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FileNotFoundException>(
            () => orchestrator.ExecuteAsync(config, CancellationToken.None));
        exception.FileName.Should().Be("nonexistent.csv");
    }

    [Fact]
    public async Task Pipeline_WithEmptySteps_ShouldCompleteSuccessfully()
    {
        // Arrange
        var config = new PipelineConfiguration
        {
            Steps = new List<StepConfiguration>()
        };

        var stepFactory = new StepFactory();
        var orchestrator = new PipelineOrchestrator(stepFactory);

        // Act
        await orchestrator.ExecuteAsync(config, CancellationToken.None);

        // Assert - Should complete without throwing
    }

    [Fact]
    public async Task Pipeline_WithCancellation_ShouldRespectCancellation()
    {
        // Arrange
        var testCsvPath = Path.Combine("TestData", "test.csv");

        var config = new PipelineConfiguration
        {
            Steps = new List<StepConfiguration>
            {
                new StepConfiguration
                {
                    Type = "Aura.Plugin.Csv.CsvDataSource, Aura.Plugin.Csv",
                    Settings = TestHelpers.CreateStringSetting("filePath", testCsvPath)
                },
                new StepConfiguration
                {
                    Type = "Aura.Plugin.Transforms.ConsoleDataSink, Aura.Plugin.Transforms",
                    Settings = new Dictionary<string, object>()
                }
            }
        };

        var stepFactory = new StepFactory();
        var orchestrator = new PipelineOrchestrator(stepFactory);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => orchestrator.ExecuteAsync(config, cts.Token));
    }

    [Fact]
    public async Task Pipeline_ConfigurationRoundTrip_ShouldPreserveData()
    {
        // Arrange - Create a configuration, serialize it, then deserialize and execute
        var originalConfig = new PipelineConfiguration
        {
            Steps = new List<StepConfiguration>
            {
                new StepConfiguration
                {
                    Type = "Aura.Plugin.Csv.CsvDataSource, Aura.Plugin.Csv",
                    Settings = TestHelpers.CreateStringSetting("filePath", "TestData/test.csv")
                },
                new StepConfiguration
                {
                    Type = "Aura.Plugin.Transforms.ConsoleDataSink, Aura.Plugin.Transforms",
                    Settings = new Dictionary<string, object>()
                }
            }
        };

        // Serialize to JSON
        var json = JsonSerializer.Serialize(originalConfig);

        // Deserialize back
        var deserializedConfig = JsonSerializer.Deserialize<PipelineConfiguration>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Act - Execute with deserialized config
        var stepFactory = new StepFactory();
        var orchestrator = new PipelineOrchestrator(stepFactory);

        using var stringWriter = new StringWriter();
        var originalOutput = Console.Out;
        Console.SetOut(stringWriter);

        try
        {
            await orchestrator.ExecuteAsync(deserializedConfig!, CancellationToken.None);

            // Assert
            var output = stringWriter.ToString();
            output.Should().Contain("Pipeline execution finished successfully.");
        }
        finally
        {
            Console.SetOut(originalOutput);
        }
    }

    [Fact]
    public async Task Pipeline_MultipleRuns_ShouldBeIndependent()
    {
        // Arrange
        var testCsvPath = Path.Combine("TestData", "test.csv");

        var config = new PipelineConfiguration
        {
            Steps = new List<StepConfiguration>
            {
                new StepConfiguration
                {
                    Type = "Aura.Plugin.Csv.CsvDataSource, Aura.Plugin.Csv",
                    Settings = TestHelpers.CreateStringSetting("filePath", testCsvPath)
                },
                new StepConfiguration
                {
                    Type = "Aura.Plugin.Transforms.ConsoleDataSink, Aura.Plugin.Transforms",
                    Settings = new Dictionary<string, object>()
                }
            }
        };

        var stepFactory = new StepFactory();
        var orchestrator = new PipelineOrchestrator(stepFactory);

        // Act - Run pipeline twice
        using var stringWriter1 = new StringWriter();
        var originalOutput = Console.Out;

        try
        {
            Console.SetOut(stringWriter1);
            await orchestrator.ExecuteAsync(config, CancellationToken.None);
        }
        finally
        {
            Console.SetOut(originalOutput);
        }

        using var stringWriter2 = new StringWriter();

        try
        {
            Console.SetOut(stringWriter2);
            await orchestrator.ExecuteAsync(config, CancellationToken.None);
        }
        finally
        {
            Console.SetOut(originalOutput);
        }

        // Assert - Both runs should produce identical results
        var output1 = stringWriter1.ToString();
        var output2 = stringWriter2.ToString();

        output1.Should().Contain("Pipeline execution finished successfully.");
        output2.Should().Contain("Pipeline execution finished successfully.");

        // Extract the result sections for comparison
        var result1 = ExtractResultSection(output1);
        var result2 = ExtractResultSection(output2);

        result1.Should().Be(result2);
    }

    private static string ExtractResultSection(string output)
    {
        var startMarker = "--- Pipeline Result ---";
        var endMarker = "-----------------------";

        var startIndex = output.IndexOf(startMarker);
        var endIndex = output.IndexOf(endMarker, startIndex) + endMarker.Length;

        if (startIndex >= 0 && endIndex > startIndex)
        {
            return output[startIndex..endIndex];
        }

        return string.Empty;
    }
}
