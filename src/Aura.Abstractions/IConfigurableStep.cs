// src/Aura.Abstractions/IConfigurableStep.cs

using System.Collections.Generic;

namespace Aura.Abstractions
{
    /// <summary>
    /// Defines a contract for pipeline steps that require external configuration.
    /// The StepFactory will check if a created step implements this interface
    /// and, if so, will invoke Initialize to pass the required settings.
    /// This promotes a clean, explicit mechanism for dependency injection of configuration.
    /// </summary>
    public interface IConfigurableStep
    {
        /// <summary>
        /// Initializes the pipeline step with its specific configuration settings.
        /// </summary>
        /// <param name="settings">A dictionary of key-value settings from the pipeline configuration file.</param>
        void Initialize(IDictionary<string, object> settings);
    }
}