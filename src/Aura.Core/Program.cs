// src/Aura.Core/Program.cs

using Aura.Core.Interfaces;
using Aura.Core.Models;
using Aura.Core.Services;
using Aura.Core.Validation;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Text.Json;

namespace Aura.Core;

public static class Program
{
    private static readonly CancellationTokenSource _shutdownTokenSource = new();

    public static async Task<int> Main(string[] args)
    {
        // Set up graceful shutdown handlers
        Console.CancelKeyPress += OnCancelKeyPress;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

        try
        {
            // Create and configure the host
            var host = CreateHostBuilder(args).Build();

            // Get logger
            var logger = host.Services.GetRequiredService<ILogger<PipelineOrchestrator>>();
            logger.LogInformation("Aura ETL Engine starting up...");

            // Load and validate pipeline configuration
            var configPath = "pipeline.json";
            if (!File.Exists(configPath))
            {
                logger.LogError("Configuration file not found at '{ConfigPath}'", configPath);
                return 1;
            }

            var configJson = await File.ReadAllTextAsync(configPath);
            var config = JsonSerializer.Deserialize<PipelineConfiguration>(
                configJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (config == null)
            {
                logger.LogError("Failed to deserialize pipeline configuration");
                return 1;
            }

            // Validate configuration using FluentValidation
            var validator = host.Services.GetRequiredService<IValidator<PipelineConfiguration>>();
            var validationResult = await validator.ValidateAsync(config, _shutdownTokenSource.Token);
            
            if (!validationResult.IsValid)
            {
                logger.LogError("Pipeline configuration validation failed:");
                foreach (var error in validationResult.Errors)
                {
                    logger.LogError("  - {PropertyName}: {ErrorMessage}", error.PropertyName, error.ErrorMessage);
                }
                return 1;
            }

            logger.LogInformation("Pipeline configuration validated successfully");

            // Get the orchestrator and execute the pipeline
            var orchestrator = host.Services.GetRequiredService<PipelineOrchestrator>();
            await orchestrator.ExecuteAsync(config, _shutdownTokenSource.Token);

            logger.LogInformation("Pipeline execution completed successfully");
            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\nPipeline execution was cancelled.");
            return 130; // Standard exit code for SIGINT
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nFatal error: {ex.Message}");
            Console.ResetColor();
            
            // Use Serilog if available
            Log.Fatal(ex, "Pipeline execution failed with unhandled exception");
            return 1;
        }
        finally
        {
            // Ensure Serilog flushes all logs
            Log.CloseAndFlush();
            
            // Clean up
            _shutdownTokenSource.Dispose();
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory());
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddEnvironmentVariables(prefix: "AURA_");
                config.AddCommandLine(args);
            })
            .UseSerilog((context, services, configuration) => configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .Enrich.WithProcessId())
            .ConfigureServices((context, services) =>
            {
                // Register core services
                services.AddSingleton<IStepFactory, StepFactory>();
                services.AddSingleton<PipelineOrchestrator>();

                // Register validators
                services.AddSingleton<IValidator<PipelineConfiguration>, PipelineConfigurationValidator>();
                services.AddSingleton<IValidator<StepConfiguration>, StepConfigurationValidator>();

                // Register configuration options
                services.Configure<PipelineOptions>(
                    context.Configuration.GetSection("Pipeline"));
                services.Configure<ResilienceOptions>(
                    context.Configuration.GetSection("Resilience"));
            });
    }

    private static void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        Console.WriteLine("\nReceived Ctrl+C, initiating graceful shutdown...");
        e.Cancel = true; // Prevent immediate termination
        _shutdownTokenSource.Cancel();
    }

    private static void OnProcessExit(object? sender, EventArgs e)
    {
        if (!_shutdownTokenSource.IsCancellationRequested)
        {
            Console.WriteLine("\nReceived termination signal, initiating graceful shutdown...");
            _shutdownTokenSource.Cancel();
            Thread.Sleep(1000); // Grace period for cleanup
        }
    }

    /// <summary>
    /// Configuration options for pipeline execution.
    /// </summary>
    public class PipelineOptions
    {
        public int MaxConcurrentSteps { get; set; } = 1;
        public bool EnableMetrics { get; set; } = true;
        public bool EnableTracing { get; set; } = true;
        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMinutes(30);
    }

    /// <summary>
    /// Configuration options for resilience policies.
    /// </summary>
    public class ResilienceOptions
    {
        public int MaxRetryAttempts { get; set; } = 3;
        public int RetryDelaySeconds { get; set; } = 2;
        public int CircuitBreakerThreshold { get; set; } = 5;
        public TimeSpan CircuitBreakerDuration { get; set; } = TimeSpan.FromMinutes(1);
    }
}
