// src/Aura.Core/Services/StepFactory.cs

using Aura.Abstractions; // Added for IConfigurableStep
using Aura.Core.Interfaces;
using Aura.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Aura.Core.Services
{
    public class StepFactory : IStepFactory
    {
        private readonly Dictionary<string, Assembly> _loadedAssemblies = new();

        public StepFactory()
        {
            LoadPluginAssemblies();
        }

        public object CreateStep(StepConfiguration stepConfig)
        {
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

            return stepInstance;
        }

        private void LoadPluginAssemblies()
        {
            // ... (rest of the method is unchanged)
            var executionPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            var pluginsPath = Path.Combine(executionPath, "plugins");

            if (!Directory.Exists(pluginsPath)) return;

            var pluginAssemblies = Directory.GetFiles(pluginsPath, "*.dll");
            foreach (var assemblyPath in pluginAssemblies)
            {
                var assembly = Assembly.LoadFrom(assemblyPath);
                _loadedAssemblies[assembly.GetName().Name!] = assembly;
                Console.WriteLine($"Loaded plugin assembly: {assembly.GetName().Name}");
            }
        }
    }
}