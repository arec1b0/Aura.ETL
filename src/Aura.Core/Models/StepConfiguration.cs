// src/Aura.Core/Models/StepConfiguration.cs

using System.Collections.Generic;

namespace Aura.Core.Models
{
    /// <summary>
    * /// Represents the configuration for a single step in the pipeline.
    * /// This class is designed to be deserialized from a configuration file (e.g., JSON).
    /// </summary>
    public class StepConfiguration
    {
        /// <summary>
        * /// The fully qualified type name of the plugin to be executed.
        * /// Example: "Aura.Plugin.Csv.CsvDataSource, Aura.Plugin.Csv"
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        * /// A dictionary of key-value settings to be passed to the plugin instance.
        * /// This allows for parameterizing steps, e.g., providing a file path.
        /// </summary>
        public Dictionary<string, object> Settings { get; set; } = new();
    }
}