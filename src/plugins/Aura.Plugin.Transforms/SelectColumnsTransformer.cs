// src/plugins/Aura.Plugin.Transforms/SelectColumnsTransformer.cs

using Aura.Abstractions;
using System.Buffers;
using System.Text.Json;

namespace Aura.Plugin.Transforms;

/// <summary>
/// A transformer that selects a subset of columns from the input data using memory pooling for performance.
/// It expects the input to be an IEnumerable of string arrays.
/// </summary>
public class SelectColumnsTransformer : ITransformer<IEnumerable<string[]>, IEnumerable<string[]>>, IConfigurableStep
{
    private int[] _columnIndices = Array.Empty<int>();
    private static readonly ArrayPool<string> _stringPool = ArrayPool<string>.Shared;

    /// <inheritdoc/>
    public void Initialize(IDictionary<string, object> settings)
    {
        if (!settings.TryGetValue("columnIndices", out var indicesObj))
        {
            throw new ArgumentException("SelectColumnsTransformer requires a 'columnIndices' setting.");
        }

        // The setting is deserialized from JSON as a JsonElement representing an array.
        _columnIndices = ((JsonElement)indicesObj).EnumerateArray().Select(e => e.GetInt32()).ToArray();

        if (_columnIndices.Length == 0)
        {
            throw new ArgumentException("SelectColumnsTransformer requires at least one column index.");
        }
    }

    /// <inheritdoc/>
    public Task<DataContext<IEnumerable<string[]>>> ExecuteAsync(
        DataContext<IEnumerable<string[]>> context, 
        CancellationToken cancellationToken)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        // Use memory pooling for better performance with large datasets
        var transformedData = new List<string[]>();

        foreach (var row in context.Payload)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Rent array from pool (potentially larger than needed)
            var pooledArray = _stringPool.Rent(_columnIndices.Length);
            
            try
            {
                // Populate the pooled array
                for (int i = 0; i < _columnIndices.Length; i++)
                {
                    var columnIndex = _columnIndices[i];
                    pooledArray[i] = row.Length > columnIndex ? row[columnIndex] : string.Empty;
                }

                // Create exact-size array from pooled array
                // We need to copy because the pooled array gets returned
                var resultRow = new string[_columnIndices.Length];
                Array.Copy(pooledArray, resultRow, _columnIndices.Length);
                transformedData.Add(resultRow);
            }
            finally
            {
                // Return array to pool for reuse
                _stringPool.Return(pooledArray, clearArray: true);
            }
        }

        return Task.FromResult(new DataContext<IEnumerable<string[]>>(transformedData));
    }
}
