// src/Aura.Abstractions/IPipelineStep.cs

using System.Threading;
using System.Threading.Tasks;

namespace Aura.Abstractions
{
    /// <summary>
    /// Defines the fundamental contract for a single, type-safe step within an ETL pipeline.
    /// This generic interface ensures that the input and output types of connected
    /// steps are compatible at compile time.
    /// </summary>
    /// <typeparam name="TIn">The type of the input data context's payload.</typeparam>
    /// <typeparam name="TOut">The type of the output data context's payload.</typeparam>
    public interface IPipelineStep<TIn, TOut>
    {
        /// <summary>
        /// Asynchronously executes the logic for this pipeline step.
        /// </summary>
        /// <param name="context">
        /// The strongly-typed data context passed from the previous step.
        /// </param>
        /// <param name="cancellationToken">
        /// A token to signal that the operation should be cancelled.
        /// </param>
        /// <returns>
        /// A task representing the asynchronous operation. The resulting, strongly-typed
        /// DataContext will be passed to the subsequent pipeline step.
        /// </returns>
        Task<DataContext<TOut>> ExecuteAsync(DataContext<TIn> context, CancellationToken cancellationToken);
    }
}