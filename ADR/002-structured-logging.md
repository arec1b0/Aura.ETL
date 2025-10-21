# ADR 002: Implement Structured Logging with Serilog

## Status
Accepted

## Context
The original implementation used `Console.WriteLine()` for all logging:

```csharp
Console.WriteLine($"Executing step: {stepConfig.Type}");
Console.WriteLine($"Step {stepConfig.Type} completed.");
```

This approach has significant limitations:
1. **No Log Levels**: Cannot filter by severity (Info, Warning, Error)
2. **No Structured Data**: Logs are plain text, difficult to query or analyze
3. **No Correlation**: Cannot trace related operations across steps
4. **No Persistence**: Logs disappear when console closes
5. **Limited Observability**: No integration with monitoring tools
6. **Production Unsuitable**: Cannot send logs to centralized systems

## Decision
Implement structured logging using:
1. **Microsoft.Extensions.Logging.ILogger**: Standard .NET logging abstraction
2. **Serilog**: Powerful structured logging library with extensive sink support
3. **Configuration-Based**: Log settings in `appsettings.json`
4. **Correlation IDs**: Track individual pipeline executions
5. **Metrics Collection**: Record timing, row counts, and success/failure

## Implementation

### Logger Integration
```csharp
public class PipelineOrchestrator
{
    private readonly ILogger<PipelineOrchestrator> _logger;
    
    public PipelineOrchestrator(
        IStepFactory stepFactory,
        ILogger<PipelineOrchestrator> logger)
    {
        _logger = logger;
    }
    
    public async Task ExecuteAsync(...)
    {
        _logger.LogInformation(
            "Starting pipeline execution. ExecutionId: {ExecutionId}, Steps: {StepCount}",
            metrics.PipelineExecutionId,
            config.Steps.Count);
    }
}
```

### Configuration
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/aura-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  }
}
```

### Host Builder Integration
```csharp
.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName())
```

## Metrics Collection
```csharp
public class PipelineMetrics
{
    public string PipelineExecutionId { get; } = Guid.NewGuid().ToString("N");
    public DateTime StartTime { get; private set; }
    public TimeSpan Duration => _overallTimer.Elapsed;
    public IReadOnlyDictionary<string, StepMetrics> StepMetrics { get; }
}
```

## Consequences

### Positive
- **Queryable Logs**: Structured data enables powerful filtering and aggregation
- **Production Ready**: Logs to files with rotation and retention policies
- **Correlation**: ExecutionId tracks all operations in a pipeline run
- **Performance Insights**: Timing data identifies bottlenecks
- **Monitoring Integration**: Can send logs to ELK, Splunk, Application Insights, etc.
- **Configurable**: Change log levels and outputs without code changes
- **Type Safety**: Compile-time verification of log parameters

### Negative
- **Dependency**: Adds Serilog NuGet packages
- **Learning Curve**: Team must understand structured logging concepts
- **Disk Space**: File logging consumes storage (mitigated by retention policies)

### Neutral
- **Breaking Change**: Minimal - console output format changes slightly
- **Performance**: Negligible overhead with async logging

## Log Levels Strategy
- **Trace**: Detailed diagnostic information (disabled in production)
- **Debug**: Internal state information (development only)
- **Information**: General flow of the application (pipeline start/complete, step execution)
- **Warning**: Unexpected situations that don't prevent operation (retries, degraded performance)
- **Error**: Failures that prevent a step from completing (file not found, invalid data)
- **Critical**: Catastrophic failures requiring immediate attention (out of memory)

## Example Log Output
```
[14:32:15 INF] Starting pipeline execution. ExecutionId: a3f2d8b9, Steps: 3
[14:32:15 INF] Executing step 1/3: CsvDataSource (Input: Object, Output: IEnumerable`1)
[14:32:16 INF] Step completed successfully: CsvDataSource (Duration: 892ms, Rows: 10000)
[14:32:16 INF] Executing step 2/3: SelectColumnsTransformer (Input: IEnumerable`1, Output: IEnumerable`1)
[14:32:16 INF] Step completed successfully: SelectColumnsTransformer (Duration: 145ms, Rows: 10000)
[14:32:16 INF] Executing step 3/3: ConsoleDataSink (Input: IEnumerable`1, Output: Object)
[14:32:17 INF] Step completed successfully: ConsoleDataSink (Duration: 234ms, Rows: 10000)
[14:32:17 INF] Pipeline execution completed. ExecutionId: a3f2d8b9, Duration: 1271ms, Rows: 10000
```

## Future Enhancements
- OpenTelemetry integration for distributed tracing
- Metrics export to Prometheus
- Application Insights integration for Azure deployments
- Custom enrichers for environment-specific metadata

## Validation
- Logs are persisted to `logs/` directory with daily rotation
- Console output remains human-readable
- Log queries can filter by ExecutionId, StepName, or Duration
- Performance impact <2% compared to Console.WriteLine

## References
- Serilog Documentation: https://serilog.net/
- Microsoft.Extensions.Logging: https://docs.microsoft.com/en-us/dotnet/core/extensions/logging
- Structured Logging Best Practices: https://stackify.com/what-is-structured-logging/

