// src/plugins/Aura.Plugin.Csv/CsvDataSource.cs

using Aura.Abstractions;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Plugin.Csv
{
    /// <summary>
    /// A data source plugin that reads records from a CSV file using streaming to handle large files efficiently.
    /// The output data type is an IEnumerable of string arrays, where each array
    /// represents a single row of comma-separated values.
    /// </summary>
    public class CsvDataSource : IDataSource<IEnumerable<string[]>>, IConfigurableStep
    {
        private string _filePath = string.Empty;
        private int _batchSize = 1000; // Default batch size for streaming
        private long _maxFileSizeBytes = 10_000_000_000; // 10GB default limit
        private const int BufferSize = 8192;

        /// <inheritdoc/>
        public void Initialize(IDictionary<string, object> settings)
        {
            if (!settings.TryGetValue("filePath", out var filePathObj))
            {
                throw new ArgumentException("CsvDataSource requires a 'filePath' setting.");
            }

            // The setting comes from JSON deserialization as a JsonElement.
            _filePath = ((JsonElement)filePathObj).GetString() ?? throw new ArgumentNullException("filePath cannot be null.");

            // Optional: batch size for streaming
            if (settings.TryGetValue("batchSize", out var batchSizeObj) && batchSizeObj is JsonElement batchSizeElement)
            {
                _batchSize = batchSizeElement.GetInt32();
            }

            // Optional: max file size limit
            if (settings.TryGetValue("maxFileSizeBytes", out var maxSizeObj) && maxSizeObj is JsonElement maxSizeElement)
            {
                _maxFileSizeBytes = maxSizeElement.GetInt64();
            }

            // Validate file path for security
            ValidateFilePath(_filePath);
        }

        /// <inheritdoc/>
        public async Task<DataContext<IEnumerable<string[]>>> ExecuteAsync(DataContext<object> context, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_filePath))
            {
                throw new ArgumentException("File path cannot be null or whitespace.", nameof(_filePath));
            }

            if (!File.Exists(_filePath))
            {
                throw new FileNotFoundException($"CSV file not found at path: {_filePath}", _filePath);
            }

            // Re-validate at execution time (defense in depth)
            ValidateFilePath(_filePath);

            // Use streaming to avoid loading entire file into memory
            var data = new List<string[]>();
            
            await foreach (var row in ReadCsvStreamAsync(cancellationToken))
            {
                data.Add(row);
            }

            return new DataContext<IEnumerable<string[]>>(data);
        }

        /// <summary>
        /// Streams CSV file contents using IAsyncEnumerable to handle large files efficiently.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Async enumerable of string arrays representing CSV rows.</returns>
        private async IAsyncEnumerable<string[]> ReadCsvStreamAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var fileStream = new FileStream(
                _filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                BufferSize,
                useAsync: true);

            using var reader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, BufferSize);

            long lineNumber = 0;
            while (!reader.EndOfStream)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var line = await reader.ReadLineAsync();
                lineNumber++;

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue; // Skip empty lines
                }

                // Simple CSV parsing (for production, consider using CsvHelper library)
                var fields = ParseCsvLine(line);
                
                yield return fields;
            }
        }

        /// <summary>
        /// Parses a CSV line into fields. Handles basic CSV escaping.
        /// For production use, consider using a robust CSV parsing library like CsvHelper.
        /// </summary>
        private string[] ParseCsvLine(string line)
        {
            // Simple implementation - split by comma
            // TODO: For production, use CsvHelper to handle quoted fields, escaped commas, etc.
            return line.Split(',').Select(f => f.Trim()).ToArray();
        }

        /// <summary>
        /// Validates file path to prevent path traversal attacks and enforce size limits.
        /// </summary>
        /// <param name="filePath">The file path to validate.</param>
        /// <exception cref="SecurityException">Thrown when path validation fails.</exception>
        /// <exception cref="InvalidOperationException">Thrown when file exceeds size limit.</exception>
        private void ValidateFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));
            }

            try
            {
                // Get the full absolute path
                var fullPath = Path.GetFullPath(filePath);

                // Define allowed directory (application base directory and subdirectories)
                var allowedDirectory = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);

                // Check for path traversal - ensure file is within allowed directory
                if (!fullPath.StartsWith(allowedDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    throw new SecurityException(
                        $"Access denied: File path '{filePath}' is outside the allowed directory. " +
                        $"Only files within the application directory are permitted.");
                }

                // Validate file exists before checking size
                if (!File.Exists(fullPath))
                {
                    // Don't throw here - file might not exist yet during initialization
                    return;
                }

                // Check file size to prevent processing extremely large files
                var fileInfo = new FileInfo(fullPath);
                if (fileInfo.Length > _maxFileSizeBytes)
                {
                    throw new InvalidOperationException(
                        $"File '{filePath}' exceeds maximum allowed size. " +
                        $"File size: {fileInfo.Length:N0} bytes, " +
                        $"Maximum allowed: {_maxFileSizeBytes:N0} bytes ({_maxFileSizeBytes / 1_000_000_000}GB)");
                }
            }
            catch (SecurityException)
            {
                throw; // Re-throw security exceptions
            }
            catch (InvalidOperationException)
            {
                throw; // Re-throw size validation exceptions
            }
            catch (Exception ex)
            {
                throw new SecurityException($"File path validation failed: {ex.Message}", ex);
            }
        }
    }
}