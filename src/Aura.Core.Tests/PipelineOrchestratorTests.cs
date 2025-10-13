using Aura.Core;
using Aura.Core.Interfaces;
using Aura.Core.Models;
using Aura.Abstractions;
using FluentAssertions;
using Microsoft.CSharp.RuntimeBinder;
using Moq;
using Xunit;

namespace Aura.Core.Tests;

public class PipelineOrchestratorTests
{
    private readonly Mock<IStepFactory> _mockStepFactory;
    private readonly PipelineOrchestrator _orchestrator;

    public PipelineOrchestratorTests()
    {
        _mockStepFactory = new Mock<IStepFactory>();
        _orchestrator = new PipelineOrchestrator(_mockStepFactory.Object);
    }

    [Fact]
    public void Constructor_WithNullStepFactory_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PipelineOrchestrator(null!));
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptySteps_ShouldCompleteSuccessfully()
    {
        // Arrange
        var config = new PipelineConfiguration
        {
            Steps = new List<StepConfiguration>()
        };

        // Act
        await _orchestrator.ExecuteAsync(config, CancellationToken.None);

        // Assert - Should complete without throwing
        _mockStepFactory.Verify(f => f.CreateStep(It.IsAny<StepConfiguration>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithSingleStep_ShouldExecuteStepOnce()
    {
        // Arrange
        var mockStep = new Mock<IPipelineStep<object, object>>();
        mockStep.Setup(s => s.ExecuteAsync(It.IsAny<DataContext<object>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DataContext<object>(new object()));

        _mockStepFactory.Setup(f => f.CreateStep(It.IsAny<StepConfiguration>()))
                        .Returns(mockStep.Object);

        var config = new PipelineConfiguration
        {
            Steps = new List<StepConfiguration>
            {
                new StepConfiguration
                {
                    Type = "Test.Step, TestAssembly",
                    Settings = new Dictionary<string, object>()
                }
            }
        };

        // Act
        await _orchestrator.ExecuteAsync(config, CancellationToken.None);

        // Assert
        _mockStepFactory.Verify(f => f.CreateStep(It.IsAny<StepConfiguration>()), Times.Once);
        mockStep.Verify(s => s.ExecuteAsync(It.IsAny<DataContext<object>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleSteps_ShouldExecuteStepsInOrder()
    {
        // Arrange
        var mockStep1 = new Mock<IPipelineStep<object, object>>();
        var mockStep2 = new Mock<IPipelineStep<object, object>>();
        var mockStep3 = new Mock<IPipelineStep<object, object>>();

        var step1Result = new DataContext<object>("step1_result");
        var step2Result = new DataContext<object>("step2_result");
        var step3Result = new DataContext<object>("step3_result");

        mockStep1.Setup(s => s.ExecuteAsync(It.IsAny<DataContext<object>>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(step1Result);
        mockStep2.Setup(s => s.ExecuteAsync(It.IsAny<DataContext<object>>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(step2Result);
        mockStep3.Setup(s => s.ExecuteAsync(It.IsAny<DataContext<object>>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(step3Result);

        _mockStepFactory.SetupSequence(f => f.CreateStep(It.IsAny<StepConfiguration>()))
                        .Returns(mockStep1.Object)
                        .Returns(mockStep2.Object)
                        .Returns(mockStep3.Object);

        var config = new PipelineConfiguration
        {
            Steps = new List<StepConfiguration>
            {
                new StepConfiguration { Type = "Step1", Settings = new Dictionary<string, object>() },
                new StepConfiguration { Type = "Step2", Settings = new Dictionary<string, object>() },
                new StepConfiguration { Type = "Step3", Settings = new Dictionary<string, object>() }
            }
        };

        // Act
        await _orchestrator.ExecuteAsync(config, CancellationToken.None);

        // Assert
        _mockStepFactory.Verify(f => f.CreateStep(It.IsAny<StepConfiguration>()), Times.Exactly(3));

        // Verify the steps were called in order with correct inputs
        mockStep1.Verify(s => s.ExecuteAsync(It.Is<DataContext<object>>(ctx => ctx.Payload is object), It.IsAny<CancellationToken>()), Times.Once);
        mockStep2.Verify(s => s.ExecuteAsync(It.Is<DataContext<object>>(ctx => ctx == step1Result), It.IsAny<CancellationToken>()), Times.Once);
        mockStep3.Verify(s => s.ExecuteAsync(It.Is<DataContext<object>>(ctx => ctx == step2Result), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellationRequested_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var config = new PipelineConfiguration
        {
            Steps = new List<StepConfiguration>
            {
                new StepConfiguration { Type = "Test.Step", Settings = new Dictionary<string, object>() }
            }
        };

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _orchestrator.ExecuteAsync(config, cts.Token));
    }

    [Fact]
    public async Task ExecuteAsync_WhenStepThrowsException_ShouldPropagateException()
    {
        // Arrange
        var mockStep = new Mock<IPipelineStep<object, object>>();
        mockStep.Setup(s => s.ExecuteAsync(It.IsAny<DataContext<object>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Test exception"));

        _mockStepFactory.Setup(f => f.CreateStep(It.IsAny<StepConfiguration>()))
                        .Returns(mockStep.Object);

        var config = new PipelineConfiguration
        {
            Steps = new List<StepConfiguration>
            {
                new StepConfiguration { Type = "Test.Step", Settings = new Dictionary<string, object>() }
            }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _orchestrator.ExecuteAsync(config, CancellationToken.None));
        exception.Message.Should().Be("Test exception");
    }

    [Fact]
    public async Task ExecuteAsync_WithStepThatDoesNotImplementIPipelineStep_ShouldThrowInvalidCastException()
    {
        // Arrange
        var invalidStep = new object(); // Not implementing IPipelineStep

        _mockStepFactory.Setup(f => f.CreateStep(It.IsAny<StepConfiguration>()))
                        .Returns(invalidStep);

        var config = new PipelineConfiguration
        {
            Steps = new List<StepConfiguration>
            {
                new StepConfiguration { Type = "Invalid.Step", Settings = new Dictionary<string, object>() }
            }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<RuntimeBinderException>(
            () => _orchestrator.ExecuteAsync(config, CancellationToken.None));
        exception.Message.Should().Contain("'object' does not contain a definition for 'ExecuteAsync'");
    }

    [Fact]
    public async Task ExecuteAsync_WithComplexDataFlow_ShouldPassDataCorrectly()
    {
        // Arrange - Test with actual data types that would flow in a real pipeline
        var csvData = new List<string[]>
        {
            new[] { "ID", "Name" },
            new[] { "1", "John" },
            new[] { "2", "Jane" }
        };

        var transformedData = new List<string[]>
        {
            new[] { "Name" },
            new[] { "John" },
            new[] { "Jane" }
        };

        var mockCsvSource = new Mock<IPipelineStep<object, object>>();
        mockCsvSource.Setup(s => s.ExecuteAsync(It.IsAny<DataContext<object>>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new DataContext<object>(csvData));

        var mockTransformer = new Mock<IPipelineStep<object, object>>();
        mockTransformer.Setup(s => s.ExecuteAsync(It.IsAny<DataContext<object>>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new DataContext<object>(transformedData));

        var mockSink = new Mock<IPipelineStep<object, object>>();
        mockSink.Setup(s => s.ExecuteAsync(It.IsAny<DataContext<object>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DataContext<object>(new object()));

        _mockStepFactory.SetupSequence(f => f.CreateStep(It.IsAny<StepConfiguration>()))
                        .Returns(mockCsvSource.Object)
                        .Returns(mockTransformer.Object)
                        .Returns(mockSink.Object);

        var config = new PipelineConfiguration
        {
            Steps = new List<StepConfiguration>
            {
                new StepConfiguration { Type = "CsvSource", Settings = new Dictionary<string, object>() },
                new StepConfiguration { Type = "Transformer", Settings = new Dictionary<string, object>() },
                new StepConfiguration { Type = "Sink", Settings = new Dictionary<string, object>() }
            }
        };

        // Act
        await _orchestrator.ExecuteAsync(config, CancellationToken.None);

        // Assert
        mockCsvSource.Verify(s => s.ExecuteAsync(It.IsAny<DataContext<object>>(), It.IsAny<CancellationToken>()), Times.Once);
        mockTransformer.Verify(s => s.ExecuteAsync(It.IsAny<DataContext<object>>(), It.IsAny<CancellationToken>()), Times.Once);
        mockSink.Verify(s => s.ExecuteAsync(It.IsAny<DataContext<object>>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
