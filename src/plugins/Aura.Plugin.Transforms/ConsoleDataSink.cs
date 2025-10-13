// src/plugins/Aura.Plugin.Transforms/ConsoleDataSink.cs

using Aura.Abstractions;

namespace Aura.Plugin.Transforms;

/// <summary>
/// A data sink that prints the incoming data rows to the standard console output.
/// This serves as a simple and effective way to view the final result of a pipeline.
/// </summary>
public class ConsoleDataSink : IDataSink<IEnumerable<string[]>>
{
    /// <inheritdoc/>
    public Task<DataContext<object>> ExecuteAsync(DataContext<IEnumerable<string[]>> context, CancellationToken cancellationToken)
    {
        Console.WriteLine("\n--- Pipeline Result ---");
        foreach (var row in context.Payload)
        {
            Console.WriteLine(string.Join(" | ", row));
        }
        Console.WriteLine("-----------------------\n");

        // A sink returns a final, empty context.
        return Task.FromResult(new DataContext<object>(new object()));
    }
}