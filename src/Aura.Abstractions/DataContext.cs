// src/Aura.Abstractions/DataContext.cs

namespace Aura.Abstractions
{
    /// <summary>
    /// Represents the data context that is passed between pipeline steps.
    /// Using a generic wrapper class ensures type safety throughout the pipeline
    /// and provides a consistent data-carrying mechanism.
    /// </summary>
    /// <typeparam name="T">The type of the data payload.</typeparam>
    public class DataContext<T>
    {
        /// <summary>
        /// Gets the payload of this data context.
        /// </summary>
        public T Payload { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataContext{T}"/> class.
        /// </summary>
        /// <param name="payload">The data to be carried by this context.</param>
        public DataContext(T payload)
        {
            Payload = payload;
        }
    }
}