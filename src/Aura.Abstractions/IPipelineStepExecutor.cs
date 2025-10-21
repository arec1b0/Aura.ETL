// src/Aura.Abstractions/IPipelineStepExecutor.cs

using System.Threading;
using System.Threading.Tasks;

namespace Aura.Abstractions
{
    /// <summary>
    /// Non-generic interface for pipeline step execution.
    /// This allows runtime polymorphism while maintaining type safety through wrapped execution.
    /// </summary>
    public interface IPipelineStepExecutor
    {
        /// <summary>
        /// Gets the type of input data this step expects.
        /// </summary>
        Type InputType { get; }

        /// <summary>
        /// Gets the type of output data this step produces.
        /// </summary>
        Type OutputType { get; }

        /// <summary>
        /// Gets the name of the step for logging and diagnostics.
        /// </summary>
        string StepName { get; }

        /// <summary>
        /// Executes the pipeline step with type-safe boxing.
        /// </summary>
        /// <param name="context">The input data context (boxed).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The output data context (boxed).</returns>
        /// <exception cref="ArgumentException">Thrown when the input context type doesn't match InputType.</exception>
        Task<object> ExecuteAsync(object context, CancellationToken cancellationToken);

        /// <summary>
        /// Validates that this step can accept output from the previous step.
        /// </summary>
        /// <param name="previousOutputType">The output type of the previous step.</param>
        /// <returns>True if compatible, false otherwise.</returns>
        bool CanAcceptInput(Type? previousOutputType);
    }
}

