// src/Aura.Core/Interfaces/IStepFactory.cs

using Aura.Abstractions;
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
        /// Creates a type-safe executor for the specified step configuration.
        /// </summary>
        /// <param name="stepConfig">The configuration for the step to be created.</param>
        /// <returns>A type-safe pipeline step executor.</returns>
        IPipelineStepExecutor CreateStep(StepConfiguration stepConfig);
    }
}