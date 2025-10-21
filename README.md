# Aura.ETL

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![CI](https://github.com/arec1b0/Aura.ETL/workflows/CI/badge.svg)](https://github.com/arec1b0/Aura.ETL/actions)
[![CodeQL](https://github.com/arec1b0/Aura.ETL/workflows/CodeQL/badge.svg)](https://github.com/arec1b0/Aura.ETL/actions)

A lightweight, extensible ETL (Extract, Transform, Load) pipeline framework built with C# and .NET 8.0. Aura.ETL provides a clean, type-safe architecture for building data processing pipelines with plugin-based extensibility.

## 🌟 Features

### Core Capabilities
- **Type-Safe Pipeline Execution**: Compile-time type checking with no dynamic dispatch
- **Plugin Architecture**: Extensible through .NET assemblies loaded at runtime
- **Configuration-Driven**: JSON-based pipeline configuration with FluentValidation
- **Asynchronous Processing**: Built-in support for async operations and cancellation
- **Modular Design**: Clean separation between abstractions, core engine, and plugins
- **Comprehensive Testing**: 60+ unit and integration tests ensuring reliability

### Production-Ready Features
- **Structured Logging**: Serilog integration with correlation IDs and metrics tracking
- **Streaming Data Processing**: Handle multi-GB CSV files with constant memory usage
- **Dependency Injection**: Microsoft.Extensions.Hosting for modern .NET architecture
- **Resilience Policies**: Polly-based retry, circuit breaker, and timeout patterns
- **Security Hardening**: Path traversal validation and file size limits
- **Graceful Shutdown**: Proper signal handling for Ctrl+C and SIGTERM
- **Memory Pooling**: ArrayPool optimizations for high-performance transformations
- **Cross-Platform CI/CD**: PowerShell-based builds for Windows/Linux/macOS
- **Configuration Validation**: Pre-execution validation with clear error messages
- **Observability**: Pipeline metrics with execution time and row counts

## 🏗️ Architecture

### Core Components

- **Aura.Abstractions**: Defines the core interfaces and contracts
- **Aura.Core**: Contains the pipeline orchestrator and step factory
- **Plugins**: Modular components for data sources, transformations, and sinks

### Pipeline Flow

```
Data Source → Transformer(s) → Data Sink
    ↓            ↓            ↓
 Extract    Transform    Load
```

## 🚀 Quick Start

### Prerequisites

- .NET 8.0 SDK or later
- Windows/Linux/macOS
- 2GB+ RAM recommended for large file processing

### Installation

1. Clone the repository:

```bash
git clone https://github.com/arec1b0/Aura.ETL.git
cd Aura.ETL
```

2. Build the solution:

```bash
dotnet build
```

3. Configure logging (optional):

```bash
# Edit src/Aura.Core/appsettings.json to customize log levels and outputs
```

4. Run the example pipeline:

```bash
cd src/Aura.Core
dotnet run
```

Logs will be written to `logs/aura-{Date}.log` and the console.

## 📖 Usage

### Creating a Pipeline

Pipelines are defined using JSON configuration files. Here's an example:

```json
{
  "Steps": [
    {
      "Type": "Aura.Plugin.Csv.CsvDataSource, Aura.Plugin.Csv",
      "Settings": {
        "filePath": "data.csv"
      }
    },
    {
      "Type": "Aura.Plugin.Transforms.SelectColumnsTransformer, Aura.Plugin.Transforms",
      "Settings": {
        "columnIndices": [1, 3]
      }
    },
    {
      "Type": "Aura.Plugin.Transforms.ConsoleDataSink, Aura.Plugin.Transforms",
      "Settings": {}
    }
  ]
}
```

### Built-in Plugins

#### Data Sources

- **CsvDataSource**: Streams data from CSV files (supports multi-GB files)
  - Settings: 
    - `filePath` (string, required): Path to CSV file
    - `batchSize` (int, optional): Batch size for streaming (default: 1000)
    - `maxFileSizeBytes` (long, optional): Maximum file size in bytes (default: 10GB)

#### Transformers

- **SelectColumnsTransformer**: Selects specific columns by index (uses memory pooling)
  - Settings: `columnIndices` (array of integers, required)

#### Data Sinks

- **ConsoleDataSink**: Outputs data to console
  - Settings: none

### Creating Custom Plugins

1. Create a new class library project in `src/plugins/`
2. Implement the appropriate interface:
   - `IDataSource<TOut>` for data sources
   - `ITransformer<TIn, TOut>` for transformations
   - `IDataSink<TIn>` for data sinks
3. Implement `IConfigurableStep` if your plugin needs configuration
4. Add a post-build event to copy DLLs to the plugins directory

Example plugin structure:

```csharp
public class MyCustomTransformer : ITransformer<IEnumerable<string[]>, IEnumerable<string[]>>, IConfigurableStep
{
    public void Initialize(IDictionary<string, object> settings)
    {
        // Initialize with settings
    }

    public Task<DataContext<IEnumerable<string[]>>> ExecuteAsync(
        DataContext<IEnumerable<string[]>> context,
        CancellationToken cancellationToken)
    {
        // Transform logic here
        return Task.FromResult(new DataContext<IEnumerable<string[]>>(transformedData));
    }
}
```

## 🛠️ Development

### Project Structure

```
src/
├── Aura.Abstractions/          # Core interfaces and contracts
├── Aura.Core/                  # Pipeline orchestrator and main executable
│   ├── Interfaces/             # Service interfaces
│   ├── Models/                 # Configuration models
│   └── Services/               # Implementation services
├── Aura.Core.Tests/            # Comprehensive test suite (60+ tests)
└── plugins/                    # Plugin implementations
    ├── Aura.Plugin.Csv/        # CSV data source
    └── Aura.Plugin.Transforms/ # Data transformations and sinks
```

### Building from Source

```bash
# Build all projects
dotnet build

# Run comprehensive test suite (60+ tests)
dotnet test

# Create packages
dotnet pack
```

### CI/CD Pipeline

This project uses GitHub Actions for continuous integration and deployment:

- **Continuous Integration**: Automated builds and tests on push/PR to main branch
- **Multi-platform Testing**: Tests run on Windows, Linux, and macOS
- **Code Quality**: CodeQL security analysis and dependency updates via Dependabot
- **Automated Releases**: Manual workflow for publishing to NuGet

Workflows include:

- `ci.yml`: Main CI pipeline with build, test, and packaging
- `codeql-analysis.yml`: Security vulnerability scanning
- `publish.yml`: Manual NuGet package publishing

### Adding New Plugins

1. Create a new class library in `src/plugins/`
2. Reference `Aura.Abstractions`
3. Implement the required interfaces
4. Update the post-build event to copy to plugins directory

## 📊 Example Output

Running the included example pipeline produces:

```
Aura ETL Engine Initialized.
Loaded plugin assembly: Aura.Plugin.Csv
Loaded plugin assembly: Aura.Plugin.Transforms
Executing step: Aura.Plugin.Csv.CsvDataSource, Aura.Plugin.Csv
Step Aura.Plugin.Csv.CsvDataSource, Aura.Plugin.Csv completed.
Executing step: Aura.Plugin.Transforms.SelectColumnsTransformer, Aura.Plugin.Transforms
Step Aura.Plugin.Transforms.SelectColumnsTransformer, Aura.Plugin.Transforms completed.
Executing step: Aura.Plugin.Transforms.ConsoleDataSink, Aura.Plugin.Transforms
Step Aura.Plugin.Transforms.ConsoleDataSink, Aura.Plugin.Transforms completed.

--- Pipeline Result ---
John | john.doe@email.com
Jane | jane.smith@email.com
Peter | peter.jones@email.com
-----------------------

Pipeline execution finished successfully.
Execution finished.
```

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

### Development Guidelines

1. Follow the existing code style and architecture patterns
2. Add comprehensive unit and integration tests for new functionality
3. Update documentation as needed
4. Ensure all tests pass before submitting (60+ test suite)

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👤 Author

**Daniil Krizhanovskyi** - [arec1b0](https://github.com/arec1b0)

## 🙏 Acknowledgments

- Built with .NET 8.0
- Inspired by modern ETL frameworks and plugin architectures
- Designed for extensibility and maintainability
- Comprehensive testing with xUnit, FluentAssertions, and Moq
