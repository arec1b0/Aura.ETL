// src/Aura.Core/Services/StepFactory.cs

using Aura.Abstractions; // Added for IConfigurableStep
using Aura.Core.Interfaces;
using Aura.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Aura.Core.Services
{
    /// <summary>
    /// Factory for creating type-safe pipeline step executors from plugin assemblies.
    /// </summary>
    public class StepFactory : IStepFactory
    {
        private readonly Dictionary<string, Assembly> _loadedAssemblies = new();
        private readonly ILogger<StepFactory> _logger;

        public StepFactory(ILogger<StepFactory> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            LoadPluginAssemblies();
        }

        public IPipelineStepExecutor CreateStep(StepConfiguration stepConfig)
        {
            if (stepConfig == null)
            {
                throw new ArgumentNullException(nameof(stepConfig));
            }

            var typeParts = stepConfig.Type.Split(',');
            if (typeParts.Length != 2)
            {
                throw new ArgumentException($"Invalid type format in configuration: '{stepConfig.Type}'");
            }
            
            var assemblyName = typeParts[1].Trim();
            var typeName = typeParts[0].Trim();

            if (!_loadedAssemblies.TryGetValue(assemblyName, out var assembly))
            {
                throw new InvalidOperationException($"Plugin assembly '{assemblyName}' not found or loaded.");
            }

            var stepInstance = assembly.CreateInstance(typeName);
            if (stepInstance == null)
            {
                throw new InvalidOperationException($"Could not create instance of type '{typeName}' from assembly '{assemblyName}'.");
            }
            
            // NEW: Check if the step is configurable and pass settings.
            if (stepInstance is IConfigurableStep configurableStep)
            {
                configurableStep.Initialize(stepConfig.Settings);
            }

            // Wrap the step in a type-safe executor
            return WrapStepInExecutor(stepInstance, stepConfig.Type);
        }

        private IPipelineStepExecutor WrapStepInExecutor(object stepInstance, string stepTypeName)
        {
            // Find all IPipelineStep<TIn, TOut> interfaces implemented by the step
            var pipelineStepInterfaces = stepInstance.GetType()
                .GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineStep<,>))
                .ToList();

            if (pipelineStepInterfaces.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Step type '{stepTypeName}' does not implement IPipelineStep<TIn, TOut>.");
            }

            if (pipelineStepInterfaces.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Step type '{stepTypeName}' implements multiple IPipelineStep interfaces. " +
                    $"Only one is supported.");
            }

            var pipelineStepInterface = pipelineStepInterfaces[0];
            var genericArgs = pipelineStepInterface.GetGenericArguments();
            var tIn = genericArgs[0];
            var tOut = genericArgs[1];

            // Create PipelineStepExecutor<TIn, TOut> using reflection
            var executorType = typeof(PipelineStepExecutor<,>).MakeGenericType(tIn, tOut);
            var executor = Activator.CreateInstance(executorType, stepInstance, stepTypeName);

            if (executor is not IPipelineStepExecutor result)
            {
                throw new InvalidOperationException(
                    $"Failed to create executor for step '{stepTypeName}'.");
            }

            return result;
        }

        private void LoadPluginAssemblies()
        {
            var executionPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            var pluginsPath = Path.Combine(executionPath, "plugins");

            _logger.LogInformation("Loading plugins from: {PluginsPath}", pluginsPath);

            if (!Directory.Exists(pluginsPath))
            {
                _logger.LogWarning("Plugins directory not found: {PluginsPath}", pluginsPath);
                return;
            }

            var pluginAssemblies = Directory.GetFiles(pluginsPath, "*.dll");
            _logger.LogInformation("Found {AssemblyCount} plugin assemblies", pluginAssemblies.Length);

            foreach (var assemblyPath in pluginAssemblies)
            {
                try
                {
                    var assembly = Assembly.LoadFrom(assemblyPath);
                    var assemblyName = assembly.GetName().Name!;
                    _loadedAssemblies[assemblyName] = assembly;
                    
                    _logger.LogInformation(
                        "Loaded plugin assembly: {AssemblyName} (Version: {Version})",
                        assemblyName,
                        assembly.GetName().Version);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load plugin assembly: {AssemblyPath}", assemblyPath);
                }
            }
        }
    }
}