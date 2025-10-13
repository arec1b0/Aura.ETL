// src/Aura.Core/Interfaces/IStepFactory.cs

using Aura.Core.Models;

namespace Aura.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for a factory responsible for creating pipeline step instances.
    /// This abstraction decouples the orchestrator from the complexities of plugin
    /// assembly loading and type instantiation.
    /// </summary>
    public interface IStepFactory
    {
        /// <summary>
        /// Creates an instance of a pipeline step based on its configuration.
        /// </summary>
        /// <param name="stepConfig">The configuration for the step to be created.</param>
        /// <returns>An instance of the configured pipeline step as a non-generic object.</returns>
        object CreateStep(StepConfiguration stepConfig);
    }
}