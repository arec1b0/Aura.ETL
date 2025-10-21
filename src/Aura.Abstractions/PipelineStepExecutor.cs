// src/Aura.Abstractions/PipelineStepExecutor.cs

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Abstractions
{
    /// <summary>
    /// Type-safe wrapper for executing pipeline steps without using dynamic dispatch.
    /// This class maintains compile-time type safety while allowing runtime polymorphism.
    /// </summary>
    /// <typeparam name="TIn">The input type of the pipeline step.</typeparam>
    /// <typeparam name="TOut">The output type of the pipeline step.</typeparam>
    public class PipelineStepExecutor<TIn, TOut> : IPipelineStepExecutor
    {
        private readonly IPipelineStep<TIn, TOut> _step;

        public Type InputType => typeof(TIn);
        public Type OutputType => typeof(TOut);
        public string StepName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PipelineStepExecutor{TIn, TOut}"/> class.
        /// </summary>
        /// <param name="step">The strongly-typed pipeline step to wrap.</param>
        /// <param name="stepName">The name of the step for diagnostics.</param>
        public PipelineStepExecutor(IPipelineStep<TIn, TOut> step, string stepName)
        {
            _step = step ?? throw new ArgumentNullException(nameof(step));
            StepName = stepName ?? throw new ArgumentNullException(nameof(stepName));
        }

        public async Task<object> ExecuteAsync(object context, CancellationToken cancellationToken)
        {
            // Type-safe casting with clear error messages
            if (context is not DataContext<TIn> typedContext)
            {
                var actualType = context?.GetType().Name ?? "null";
                throw new InvalidOperationException(
                    $"Step '{StepName}' expects input type 'DataContext<{typeof(TIn).Name}>' but received '{actualType}'. " +
                    $"This indicates a pipeline configuration error where adjacent steps have incompatible types.");
            }

            // Execute with full type safety
            var result = await _step.ExecuteAsync(typedContext, cancellationToken);
            
            return result;
        }

        public bool CanAcceptInput(Type? previousOutputType)
        {
            if (previousOutputType == null)
            {
                // First step in pipeline - accepts object as a convention
                return typeof(TIn) == typeof(object);
            }

            // Check if the previous output type matches our input type
            // We need to unwrap the DataContext<T> to get T
            if (previousOutputType.IsGenericType && 
                previousOutputType.GetGenericTypeDefinition() == typeof(DataContext<>))
            {
                var previousPayloadType = previousOutputType.GetGenericArguments()[0];
                return previousPayloadType == typeof(TIn);
            }

            return false;
        }
    }
}
