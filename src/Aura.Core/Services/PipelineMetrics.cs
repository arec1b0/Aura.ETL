// src/Aura.Core/Services/PipelineMetrics.cs

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Aura.Core.Services
{
    /// <summary>
    /// Collects and tracks metrics for pipeline execution.
    /// </summary>
    public class PipelineMetrics
    {
        private readonly Stopwatch _overallTimer = new();
        private readonly Dictionary<string, StepMetrics> _stepMetrics = new();

        public string PipelineExecutionId { get; } = Guid.NewGuid().ToString("N");
        public DateTime StartTime { get; private set; }
        public DateTime? EndTime { get; private set; }
        public TimeSpan Duration => _overallTimer.Elapsed;
        public bool IsRunning => _overallTimer.IsRunning;
        public IReadOnlyDictionary<string, StepMetrics> StepMetrics => _stepMetrics;

        public void Start()
        {
            StartTime = DateTime.UtcNow;
            _overallTimer.Start();
        }

        public void Stop()
        {
            _overallTimer.Stop();
            EndTime = DateTime.UtcNow;
        }

        public StepMetrics StartStep(string stepName)
        {
            var metrics = new StepMetrics(stepName);
            _stepMetrics[stepName] = metrics;
            metrics.Start();
            return metrics;
        }

        public void StopStep(string stepName, bool success, long? rowsProcessed = null)
        {
            if (_stepMetrics.TryGetValue(stepName, out var metrics))
            {
                metrics.Stop(success, rowsProcessed);
            }
        }

        public Dictionary<string, object> GetSummary()
        {
            return new Dictionary<string, object>
            {
                ["ExecutionId"] = PipelineExecutionId,
                ["StartTime"] = StartTime,
                ["EndTime"] = EndTime,
                ["Duration"] = Duration,
                ["TotalSteps"] = _stepMetrics.Count,
                ["SuccessfulSteps"] = GetSuccessfulStepCount(),
                ["FailedSteps"] = GetFailedStepCount(),
                ["TotalRowsProcessed"] = GetTotalRowsProcessed()
            };
        }

        private int GetSuccessfulStepCount()
        {
            int count = 0;
            foreach (var metric in _stepMetrics.Values)
            {
                if (metric.Success)
                    count++;
            }
            return count;
        }

        private int GetFailedStepCount()
        {
            int count = 0;
            foreach (var metric in _stepMetrics.Values)
            {
                if (!metric.Success)
                    count++;
            }
            return count;
        }

        private long GetTotalRowsProcessed()
        {
            long total = 0;
            foreach (var metric in _stepMetrics.Values)
            {
                total += metric.RowsProcessed ?? 0;
            }
            return total;
        }
    }

    /// <summary>
    /// Metrics for individual pipeline step execution.
    /// </summary>
    public class StepMetrics
    {
        private readonly Stopwatch _timer = new();

        public string StepName { get; }
        public DateTime StartTime { get; private set; }
        public DateTime? EndTime { get; private set; }
        public TimeSpan Duration => _timer.Elapsed;
        public bool Success { get; private set; }
        public long? RowsProcessed { get; private set; }

        public StepMetrics(string stepName)
        {
            StepName = stepName;
        }

        public void Start()
        {
            StartTime = DateTime.UtcNow;
            _timer.Start();
        }

        public void Stop(bool success, long? rowsProcessed = null)
        {
            _timer.Stop();
            EndTime = DateTime.UtcNow;
            Success = success;
            RowsProcessed = rowsProcessed;
        }
    }
}

