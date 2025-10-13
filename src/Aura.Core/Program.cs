// src/Aura.Core/Program.cs

using Aura.Core.Models;
using Aura.Core.Services;
using System.Text.Json;

namespace Aura.Core;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("Aura ETL Engine Initialized.");
        try
        {
            // 1. Load configuration
            var configPath = "pipeline.json";
            if (!File.Exists(configPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: Configuration file not found at '{configPath}'");
                return 1; // Failure
            }
            var configJson = await File.ReadAllTextAsync(configPath);
            var config = JsonSerializer.Deserialize<PipelineConfiguration>(configJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (config == null || !config.Steps.Any())
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Warning: Pipeline configuration is empty or invalid.");
                return 0; // Success (did nothing)
            }

            // 2. Instantiate services
            var stepFactory = new StepFactory();
            var orchestrator = new PipelineOrchestrator(stepFactory);

            // 3. Execute the pipeline
            await orchestrator.ExecuteAsync(config, CancellationToken.None);

            Console.WriteLine("Execution finished.");
            return 0; // Success
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nAn unhandled exception occurred: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return 1; // Failure
        }
        finally
        {
            Console.ResetColor();
        }
    }
}