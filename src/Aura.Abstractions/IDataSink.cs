// src/Aura.Abstractions/IDataSink.cs

namespace Aura.Abstractions
{
    /// <summary>
    /// Represents the final step of an ETL pipeline (Load).
    /// A data sink receives data and performs a terminal operation, such as writing
    /// to a database or a file. It does not produce an output for subsequent steps.
    /// </summary>
    /// <typeparam name="TIn">The type of data this sink accepts.</typeparam>
    public interface IDataSink<TIn> : IPipelineStep<TIn, object>
    {
        // This interface inherits the ExecuteAsync method.
        // TOut is 'object' as a conventional placeholder for "no output".
    }
}