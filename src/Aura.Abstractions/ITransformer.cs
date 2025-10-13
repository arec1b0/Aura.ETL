// src/Aura.Abstractions/ITransformer.cs

namespace Aura.Abstractions
{
    /// <summary>
    /// Represents a transformation step in an ETL pipeline (Transform).
    /// A transformer receives data from a previous step, performs some transformation
    /// on it, and produces modified data for the next step.
    /// </summary>
    /// <typeparam name="TIn">The type of data this transformer accepts.</typeparam>
    /// <typeparam name="TOut">The type of data this transformer produces.</typeparam>
    public interface ITransformer<TIn, TOut> : IPipelineStep<TIn, TOut>
    {
        // This interface inherits the ExecuteAsync method.
    }
}
