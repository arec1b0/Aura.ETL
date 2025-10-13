# Aura.ETL

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

A lightweight, extensible ETL (Extract, Transform, Load) pipeline framework built with C# and .NET 8.0. Aura.ETL provides a clean, type-safe architecture for building data processing pipelines with plugin-based extensibility.

## 🌟 Features

- **Type-Safe Pipeline Steps**: Strongly-typed interfaces ensure compile-time safety
- **Plugin Architecture**: Extensible through .NET assemblies loaded at runtime
- **Configuration-Driven**: JSON-based pipeline configuration
- **Asynchronous Processing**: Built-in support for async operations and cancellation
- **Modular Design**: Clean separation between abstractions, core engine, and plugins

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

3. Run the example pipeline:

```bash
cd src/Aura.Core
dotnet run
```

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

- **CsvDataSource**: Reads data from CSV files
  - Settings: `filePath` (string)

#### Transformers

- **SelectColumnsTransformer**: Selects specific columns by index
  - Settings: `columnIndices` (array of integers)

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
└── plugins/                    # Plugin implementations
    ├── Aura.Plugin.Csv/        # CSV data source
    └── Aura.Plugin.Transforms/ # Data transformations and sinks
```

### Building from Source

```bash
# Build all projects
dotnet build

# Run tests (if any)
dotnet test

# Create packages
dotnet pack
```

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
2. Add unit tests for new functionality
3. Update documentation as needed
4. Ensure all tests pass before submitting

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👤 Author

**Daniil Krizhanovskyi** - [arec1b0](https://github.com/arec1b0)

## 🙏 Acknowledgments

- Built with .NET 8.0
- Inspired by modern ETL frameworks and plugin architectures
- Designed for extensibility and maintainability
