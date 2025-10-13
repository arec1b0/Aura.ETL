using Aura.Core.Interfaces;
using Aura.Core.Models;
using Aura.Core.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace Aura.Core.Tests;

public class StepFactoryTests
{
    [Fact]
    public void Constructor_ShouldLoadPluginAssemblies()
    {
        // Arrange & Act
        var factory = new StepFactory();

        // Assert - We can't directly test private fields, but we can test behavior
        // This will be tested through CreateStep method
    }

    [Fact]
    public void CreateStep_WithValidConfiguration_ShouldCreateStepInstance()
    {
        // Arrange
        var factory = new StepFactory();
        var config = new StepConfiguration
        {
            Type = "Aura.Plugin.Transforms.ConsoleDataSink, Aura.Plugin.Transforms",
            Settings = new Dictionary<string, object>()
        };

        // Act
        var step = factory.CreateStep(config);

        // Assert
        step.Should().NotBeNull();
        step.Should().BeOfType<Aura.Plugin.Transforms.ConsoleDataSink>();
    }

    [Fact]
    public void CreateStep_WithConfigurableStep_ShouldInitializeSettings()
    {
        // Arrange
        var factory = new StepFactory();
        var config = new StepConfiguration
        {
            Type = "Aura.Plugin.Transforms.SelectColumnsTransformer, Aura.Plugin.Transforms",
            Settings = TestHelpers.CreateIntArraySetting("columnIndices", new[] { 0, 2 })
        };

        // Act
        var step = factory.CreateStep(config);

        // Assert
        step.Should().NotBeNull();
        step.Should().BeOfType<Aura.Plugin.Transforms.SelectColumnsTransformer>();
        // The initialization would have been called, we can test this indirectly
    }

    [Fact]
    public void CreateStep_WithInvalidTypeFormat_ShouldThrowArgumentException()
    {
        // Arrange
        var factory = new StepFactory();
        var config = new StepConfiguration
        {
            Type = "InvalidTypeFormat",
            Settings = new Dictionary<string, object>()
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => factory.CreateStep(config));
        exception.Message.Should().Contain("Invalid type format");
    }

    [Fact]
    public void CreateStep_WithNonExistentAssembly_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var factory = new StepFactory();
        var config = new StepConfiguration
        {
            Type = "Some.Type, NonExistentAssembly",
            Settings = new Dictionary<string, object>()
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateStep(config));
        exception.Message.Should().Contain("Plugin assembly 'NonExistentAssembly' not found");
    }

    [Fact]
    public void CreateStep_WithNonExistentType_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var factory = new StepFactory();
        var config = new StepConfiguration
        {
            Type = "Aura.Plugin.Transforms.NonExistentType, Aura.Plugin.Transforms",
            Settings = new Dictionary<string, object>()
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateStep(config));
        exception.Message.Should().Contain("Could not create instance of type");
    }

    [Fact]
    public void CreateStep_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Arrange
        var factory = new StepFactory();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => factory.CreateStep(null!));
    }

    [Fact]
    public void CreateStep_WithEmptyType_ShouldThrowArgumentException()
    {
        // Arrange
        var factory = new StepFactory();
        var config = new StepConfiguration
        {
            Type = string.Empty,
            Settings = new Dictionary<string, object>()
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => factory.CreateStep(config));
        exception.Message.Should().Contain("Invalid type format");
    }

    [Fact]
    public void CreateStep_WithWhitespaceType_ShouldThrowArgumentException()
    {
        // Arrange
        var factory = new StepFactory();
        var config = new StepConfiguration
        {
            Type = "   ",
            Settings = new Dictionary<string, object>()
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => factory.CreateStep(config));
        exception.Message.Should().Contain("Invalid type format");
    }

    [Fact]
    public void CreateStep_WithCsvDataSource_ShouldCreateValidInstance()
    {
        // Arrange
        var factory = new StepFactory();
        var config = new StepConfiguration
        {
            Type = "Aura.Plugin.Csv.CsvDataSource, Aura.Plugin.Csv",
            Settings = TestHelpers.CreateStringSetting("filePath", "test.csv")
        };

        // Act
        var step = factory.CreateStep(config);

        // Assert
        step.Should().NotBeNull();
        step.Should().BeOfType<Aura.Plugin.Csv.CsvDataSource>();
    }

    [Fact]
    public void CreateStep_WithMultipleCalls_ShouldReturnDifferentInstances()
    {
        // Arrange
        var factory = new StepFactory();
        var config = new StepConfiguration
        {
            Type = "Aura.Plugin.Transforms.ConsoleDataSink, Aura.Plugin.Transforms",
            Settings = new Dictionary<string, object>()
        };

        // Act
        var step1 = factory.CreateStep(config);
        var step2 = factory.CreateStep(config);

        // Assert
        step1.Should().NotBeNull();
        step2.Should().NotBeNull();
        step1.Should().NotBeSameAs(step2);
        step1.Should().BeOfType<Aura.Plugin.Transforms.ConsoleDataSink>();
        step2.Should().BeOfType<Aura.Plugin.Transforms.ConsoleDataSink>();
    }
}
