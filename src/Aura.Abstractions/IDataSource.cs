// src/Aura.Abstractions/IDataSource.cs

namespace Aura.Abstractions
{
    /// <summary>
    /// Represents the starting point of an ETL pipeline (Extract).
    /// A data source does not receive input from a previous step. It is responsible
    /// for generating the initial data payload.
    /// </summary>
    /// <typeparam name="TOut">The type of data this source produces.</typeparam>
    public interface IDataSource<TOut> : IPipelineStep<object, TOut>
    {
        // This interface inherits the ExecuteAsync method but specializes its type constraints.
        // TIn is 'object' as a conventional placeholder for "no input".
    }
}