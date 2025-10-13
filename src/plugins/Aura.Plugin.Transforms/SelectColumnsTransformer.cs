// src/plugins/Aura.Plugin.Transforms/SelectColumnsTransformer.cs

using Aura.Abstractions;
using System.Text.Json;

namespace Aura.Plugin.Transforms;

/// <summary>
/// A transformer that selects a subset of columns from the input data.
/// It expects the input to be an IEnumerable of string arrays.
/// </summary>
public class SelectColumnsTransformer : ITransformer<IEnumerable<string[]>, IEnumerable<string[]>>, IConfigurableStep
{
    private int[] _columnIndices = Array.Empty<int>();

    /// <inheritdoc/>
    public void Initialize(IDictionary<string, object> settings)
    {
        if (!settings.TryGetValue("columnIndices", out var indicesObj))
        {
            throw new ArgumentException("SelectColumnsTransformer requires a 'columnIndices' setting.");
        }

        // The setting is deserialized from JSON as a JsonElement representing an array.
        _columnIndices = ((JsonElement)indicesObj).EnumerateArray().Select(e => e.GetInt32()).ToArray();
    }

    /// <inheritdoc/>
    public Task<DataContext<IEnumerable<string[]>>> ExecuteAsync(DataContext<IEnumerable<string[]>> context, CancellationToken cancellationToken)
    {
        var transformedData = context.Payload
            .Select(row => _columnIndices.Select(index => row.Length > index ? row[index] : string.Empty).ToArray())
            .ToList();

        return Task.FromResult(new DataContext<IEnumerable<string[]>>(transformedData));
    }
}