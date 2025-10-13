// src/Aura.Core/Models/PipelineConfiguration.cs

using System.Collections.Generic;

namespace Aura.Core.Models
{
    /// <summary>
    /// Represents the complete configuration for an ETL pipeline.
    /// </summary>
    public class PipelineConfiguration
    {
        /// <summary>
        /// The sequential list of steps that make up this pipeline.
        /// </summary>
        public List<StepConfiguration> Steps { get; set; } = new();
    }
}