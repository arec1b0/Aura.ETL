// src/Aura.Core/PipelineOrchestrator.cs

using Aura.Abstractions;
using Aura.Core.Interfaces;
using Aura.Core.Models;
using Aura.Core.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Core
{
    /// <summary>
    /// Orchestrates the execution of a type-safe ETL pipeline.
    /// Validates type compatibility between pipeline steps and executes them sequentially.
    /// </summary>
    public class PipelineOrchestrator
    {
        private readonly IStepFactory _stepFactory;
        private readonly ILogger<PipelineOrchestrator> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PipelineOrchestrator"/> class.
        /// </summary>
        /// <param name="stepFactory">The factory used to create pipeline step instances.</param>
        /// <param name="logger">Logger for structured logging.</param>
        public PipelineOrchestrator(
            IStepFactory stepFactory,
            ILogger<PipelineOrchestrator> logger)
        {
            _stepFactory = stepFactory ?? throw new ArgumentNullException(nameof(stepFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Executes the pipeline with type-safe validation and execution.
        /// </summary>
        /// <param name="config">The pipeline configuration.</param>
        /// <param name="cancellationToken">Cancellation token for graceful shutdown.</param>
        /// <exception cref="InvalidOperationException">Thrown when pipeline steps have incompatible types.</exception>
        public async Task ExecuteAsync(PipelineConfiguration config, CancellationToken cancellationToken)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (config.Steps == null || !config.Steps.Any())
            {
                throw new ArgumentException("Pipeline configuration must contain at least one step.", nameof(config));
            }

            var metrics = new PipelineMetrics();
            metrics.Start();

            _logger.LogInformation(
                "Starting pipeline execution. ExecutionId: {ExecutionId}, Steps: {StepCount}",
                metrics.PipelineExecutionId,
                config.Steps.Count);

            try
            {
                // Create all step executors upfront
                var stepExecutors = new List<IPipelineStepExecutor>();
                foreach (var stepConfig in config.Steps)
                {
                    try
                    {
                        var executor = _stepFactory.CreateStep(stepConfig);
                        stepExecutors.Add(executor);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create step '{StepType}'", stepConfig.Type);
                        throw new InvalidOperationException(
                            $"Failed to create step '{stepConfig.Type}': {ex.Message}", ex);
                    }
                }

                // Validate type compatibility BEFORE execution (fail fast)
                ValidatePipelineTypeCompatibility(stepExecutors);

                // Execute the pipeline with type-safe execution
                object currentContext = new DataContext<object>(new object());
                Type? currentOutputType = typeof(DataContext<object>);

                for (int i = 0; i < stepExecutors.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    var executor = stepExecutors[i];
                    var stepMetrics = metrics.StartStep(executor.StepName);

                    _logger.LogInformation(
                        "Executing step {StepNumber}/{TotalSteps}: {StepName} (Input: {InputType}, Output: {OutputType})",
                        i + 1,
                        stepExecutors.Count,
                        executor.StepName,
                        executor.InputType.Name,
                        executor.OutputType.Name);

                    try
                    {
                        // Type-safe execution (no dynamic dispatch!)
                        currentContext = await executor.ExecuteAsync(currentContext, cancellationToken);
                        currentOutputType = currentContext.GetType();
                        
                        // Try to get row count for metrics
                        long? rowCount = TryGetRowCount(currentContext);
                        metrics.StopStep(executor.StepName, true, rowCount);

                        _logger.LogInformation(
                            "Step completed successfully: {StepName} (Duration: {Duration}ms, Rows: {RowCount})",
                            executor.StepName,
                            stepMetrics.Duration.TotalMilliseconds,
                            rowCount ?? 0);
                    }
                    catch (Exception ex)
                    {
                        metrics.StopStep(executor.StepName, false);
                        
                        _logger.LogError(ex,
                            "Step failed: {StepName} at position {StepNumber} (Duration: {Duration}ms)",
                            executor.StepName,
                            i + 1,
                            stepMetrics.Duration.TotalMilliseconds);

                        throw new InvalidOperationException(
                            $"Pipeline failed at step {i + 1} ({executor.StepName}): {ex.Message}", ex);
                    }
                }

                metrics.Stop();

                _logger.LogInformation(
                    "Pipeline execution completed successfully. ExecutionId: {ExecutionId}, Duration: {Duration}ms, Steps: {StepCount}, Rows: {TotalRows}",
                    metrics.PipelineExecutionId,
                    metrics.Duration.TotalMilliseconds,
                    stepExecutors.Count,
                    metrics.GetSummary()["TotalRowsProcessed"]);
            }
            catch (Exception ex)
            {
                metrics.Stop();
                
                _logger.LogError(ex,
                    "Pipeline execution failed. ExecutionId: {ExecutionId}, Duration: {Duration}ms",
                    metrics.PipelineExecutionId,
                    metrics.Duration.TotalMilliseconds);

                throw;
            }
        }

        /// <summary>
        /// Attempts to extract row count from data context for metrics.
        /// </summary>
        private long? TryGetRowCount(object context)
        {
            try
            {
                var payloadProperty = context.GetType().GetProperty("Payload");
                if (payloadProperty == null)
                    return null;

                var payload = payloadProperty.GetValue(context);
                if (payload is System.Collections.IEnumerable enumerable)
                {
                    long count = 0;
                    foreach (var _ in enumerable)
                    {
                        count++;
                    }
                    return count;
                }
            }
            catch
            {
                // Ignore errors in metrics collection
            }

            return null;
        }

        /// <summary>
        /// Validates that all pipeline steps have compatible input/output types.
        /// This ensures type safety at pipeline configuration time, not runtime.
        /// </summary>
        /// <param name="stepExecutors">The list of step executors to validate.</param>
        /// <exception cref="InvalidOperationException">Thrown when adjacent steps have incompatible types.</exception>
        private void ValidatePipelineTypeCompatibility(List<IPipelineStepExecutor> stepExecutors)
        {
            _logger.LogDebug("Validating pipeline type compatibility for {StepCount} steps", stepExecutors.Count);

            Type? previousOutputType = null;

            for (int i = 0; i < stepExecutors.Count; i++)
            {
                var executor = stepExecutors[i];

                if (!executor.CanAcceptInput(previousOutputType))
                {
                    var previousTypeName = previousOutputType != null
                        ? GetPayloadTypeName(previousOutputType)
                        : "null";
                    var expectedTypeName = executor.InputType.Name;

                    var errorMessage = $"Type mismatch in pipeline at step {i + 1} ('{executor.StepName}'): " +
                        $"Step expects input type '{expectedTypeName}' but previous step outputs '{previousTypeName}'. " +
                        $"Pipeline steps must have compatible input/output types.";

                    _logger.LogError(errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }

                previousOutputType = typeof(DataContext<>).MakeGenericType(executor.OutputType);
            }

            _logger.LogInformation("Pipeline type validation successful: All {StepCount} steps have compatible types", stepExecutors.Count);
        }

        /// <summary>
        /// Extracts the payload type name from a DataContext type.
        /// </summary>
        private string GetPayloadTypeName(Type dataContextType)
        {
            if (dataContextType.IsGenericType &&
                dataContextType.GetGenericTypeDefinition() == typeof(DataContext<>))
            {
                return dataContextType.GetGenericArguments()[0].Name;
            }
            return dataContextType.Name;
        }
    }
}