using Aura.Core.Models;
using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace Aura.Core.Tests;

public class ConfigurationModelTests
{
    [Fact]
    public void PipelineConfiguration_DefaultConstructor_ShouldInitializeEmptyStepsList()
    {
        // Act
        var config = new PipelineConfiguration();

        // Assert
        config.Steps.Should().NotBeNull();
        config.Steps.Should().BeEmpty();
    }

    [Fact]
    public void PipelineConfiguration_CanAddSteps()
    {
        // Arrange
        var config = new PipelineConfiguration();
        var step = new StepConfiguration
        {
            Type = "Test.Step, TestAssembly",
            Settings = new Dictionary<string, object> { ["key"] = "value" }
        };

        // Act
        config.Steps.Add(step);

        // Assert
        config.Steps.Should().HaveCount(1);
        config.Steps[0].Should().Be(step);
    }

    [Fact]
    public void StepConfiguration_DefaultConstructor_ShouldInitializeProperties()
    {
        // Act
        var step = new StepConfiguration();

        // Assert
        step.Type.Should().BeEmpty();
        step.Settings.Should().NotBeNull();
        step.Settings.Should().BeEmpty();
    }

    [Fact]
    public void StepConfiguration_CanSetProperties()
    {
        // Arrange
        var step = new StepConfiguration();
        var settings = new Dictionary<string, object>
        {
            ["filePath"] = "test.csv",
            ["columnIndices"] = new[] { 1, 2, 3 }
        };

        // Act
        step.Type = "Aura.Plugin.Csv.CsvDataSource, Aura.Plugin.Csv";
        step.Settings = settings;

        // Assert
        step.Type.Should().Be("Aura.Plugin.Csv.CsvDataSource, Aura.Plugin.Csv");
        step.Settings.Should().BeEquivalentTo(settings);
    }

    [Fact]
    public void PipelineConfiguration_CanDeserializeFromJson()
    {
        // Arrange
        var json = @"
        {
            ""Steps"": [
                {
                    ""Type"": ""Aura.Plugin.Csv.CsvDataSource, Aura.Plugin.Csv"",
                    ""Settings"": {
                        ""filePath"": ""data.csv""
                    }
                },
                {
                    ""Type"": ""Aura.Plugin.Transforms.ConsoleDataSink, Aura.Plugin.Transforms"",
                    ""Settings"": {}
                }
            ]
        }";

        // Act
        var config = JsonSerializer.Deserialize<PipelineConfiguration>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        config.Should().NotBeNull();
        config!.Steps.Should().HaveCount(2);

        config.Steps[0].Type.Should().Be("Aura.Plugin.Csv.CsvDataSource, Aura.Plugin.Csv");
        config.Steps[0].Settings.Should().ContainKey("filePath");

        config.Steps[1].Type.Should().Be("Aura.Plugin.Transforms.ConsoleDataSink, Aura.Plugin.Transforms");
        config.Steps[1].Settings.Should().BeEmpty();
    }

    [Fact]
    public void StepConfiguration_CanDeserializeFromJson()
    {
        // Arrange
        var json = @"
        {
            ""Type"": ""Aura.Plugin.Transforms.SelectColumnsTransformer, Aura.Plugin.Transforms"",
            ""Settings"": {
                ""columnIndices"": [0, 2, 4]
            }
        }";

        // Act
        var step = JsonSerializer.Deserialize<StepConfiguration>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        step.Should().NotBeNull();
        step!.Type.Should().Be("Aura.Plugin.Transforms.SelectColumnsTransformer, Aura.Plugin.Transforms");
        step.Settings.Should().ContainKey("columnIndices");
    }

    [Fact]
    public void StepConfiguration_SettingsCanContainComplexObjects()
    {
        // Arrange
        var step = new StepConfiguration();
        var complexSettings = new Dictionary<string, object>
        {
            ["stringValue"] = "test",
            ["intValue"] = 42,
            ["arrayValue"] = new[] { 1, 2, 3 },
            ["nestedDict"] = new Dictionary<string, object> { ["inner"] = "value" }
        };

        // Act
        step.Settings = complexSettings;

        // Assert
        step.Settings.Should().BeEquivalentTo(complexSettings);
        step.Settings["stringValue"].Should().Be("test");
        step.Settings["intValue"].Should().Be(42);
        step.Settings["arrayValue"].Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void PipelineConfiguration_CanSerializeToJson()
    {
        // Arrange
        var config = new PipelineConfiguration
        {
            Steps = new List<StepConfiguration>
            {
                new StepConfiguration
                {
                    Type = "Test.Step, TestAssembly",
                    Settings = new Dictionary<string, object> { ["key"] = "value" }
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(config);

        // Assert
        json.Should().Contain("Steps");
        json.Should().Contain("Test.Step, TestAssembly");
        json.Should().Contain("key");
        json.Should().Contain("value");
    }

    [Fact]
    public void StepConfiguration_CanSerializeToJson()
    {
        // Arrange
        var step = new StepConfiguration
        {
            Type = "Test.Step, TestAssembly",
            Settings = new Dictionary<string, object>
            {
                ["stringSetting"] = "value",
                ["numberSetting"] = 123
            }
        };

        // Act
        var json = JsonSerializer.Serialize(step);

        // Assert
        json.Should().Contain("Type");
        json.Should().Contain("Test.Step, TestAssembly");
        json.Should().Contain("Settings");
        json.Should().Contain("stringSetting");
        json.Should().Contain("value");
        json.Should().Contain("numberSetting");
        json.Should().Contain("123");
    }

    [Fact]
    public void PipelineConfiguration_EmptyConfiguration_IsValid()
    {
        // Arrange
        var config = new PipelineConfiguration();

        // Act & Assert
        config.Steps.Should().NotBeNull();
        config.Steps.Should().BeEmpty();
    }

    [Fact]
    public void StepConfiguration_EmptySettings_IsValid()
    {
        // Arrange
        var step = new StepConfiguration
        {
            Type = "Test.Step",
            Settings = new Dictionary<string, object>()
        };

        // Act & Assert
        step.Type.Should().Be("Test.Step");
        step.Settings.Should().BeEmpty();
    }
}
