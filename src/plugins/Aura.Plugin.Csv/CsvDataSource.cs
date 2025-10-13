// src/plugins/Aura.Plugin.Csv/CsvDataSource.cs

using Aura.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Plugin.Csv
{
    /// <summary>
    /// A data source plugin that reads records from a CSV file.
    /// The output data type is an IEnumerable of string arrays, where each array
    /// represents a single row of comma-separated values.
    /// </summary>
    public class CsvDataSource : IDataSource<IEnumerable<string[]>>, IConfigurableStep
    {
        private string _filePath = string.Empty;

        /// <inheritdoc/>
        public void Initialize(IDictionary<string, object> settings)
        {
            if (!settings.TryGetValue("filePath", out var filePathObj))
            {
                throw new ArgumentException("CsvDataSource requires a 'filePath' setting.");
            }

            // The setting comes from JSON deserialization as a JsonElement.
            _filePath = ((JsonElement)filePathObj).GetString() ?? throw new ArgumentNullException("filePath cannot be null.");
        }

        /// <inheritdoc/>
        public async Task<DataContext<IEnumerable<string[]>>> ExecuteAsync(DataContext<object> context, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_filePath) || !File.Exists(_filePath))
            {
                throw new FileNotFoundException("CSV file not found.", _filePath);
            }

            var lines = await File.ReadAllLinesAsync(_filePath, cancellationToken);

            var data = lines
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Split(','))
                .ToList();

            return new DataContext<IEnumerable<string[]>>(data);
        }
    }
}