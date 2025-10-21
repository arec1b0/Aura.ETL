// src/Aura.Core/Services/ResiliencePipeline.cs

using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Core.Services
{
    /// <summary>
    /// Provides resilience policies for pipeline operations including retry and circuit breaker.
    /// </summary>
    public class ResiliencePipeline
    {
        private readonly ILogger<ResiliencePipeline> _logger;
        private readonly ResiliencePolicyOptions _options;

        public ResiliencePipeline(
            ILogger<ResiliencePipeline> logger,
            ResiliencePolicyOptions options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Creates a retry pipeline for transient failures.
        /// </summary>
        public ResiliencePipeline<T> CreateRetryPipeline<T>(string operationName)
        {
            var retryStrategy = new RetryStrategyOptions<T>
            {
                MaxRetryAttempts = _options.MaxRetryAttempts,
                Delay = TimeSpan.FromSeconds(_options.RetryDelaySeconds),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "Retry {AttemptNumber}/{MaxAttempts} for operation '{Operation}' after {Delay}ms due to: {Exception}",
                        args.AttemptNumber,
                        _options.MaxRetryAttempts,
                        operationName,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.Message ?? "Unknown error");

                    return default;
                }
            };

            return new ResiliencePipelineBuilder<T>()
                .AddRetry(retryStrategy)
                .Build();
        }

        /// <summary>
        /// Creates a circuit breaker pipeline to prevent cascading failures.
        /// </summary>
        public ResiliencePipeline<T> CreateCircuitBreakerPipeline<T>(string operationName)
        {
            var circuitBreakerStrategy = new CircuitBreakerStrategyOptions<T>
            {
                FailureRatio = 0.5,
                MinimumThroughput = _options.CircuitBreakerThreshold,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = _options.CircuitBreakerDuration,
                OnOpened = args =>
                {
                    _logger.LogError(
                        "Circuit breaker opened for operation '{Operation}'. Will retry after {Duration}",
                        operationName,
                        _options.CircuitBreakerDuration);
                    return default;
                },
                OnClosed = args =>
                {
                    _logger.LogInformation(
                        "Circuit breaker closed for operation '{Operation}'. Normal operation resumed.",
                        operationName);
                    return default;
                },
                OnHalfOpened = args =>
                {
                    _logger.LogInformation(
                        "Circuit breaker half-open for operation '{Operation}'. Testing if service recovered.",
                        operationName);
                    return default;
                }
            };

            return new ResiliencePipelineBuilder<T>()
                .AddCircuitBreaker(circuitBreakerStrategy)
                .Build();
        }

        /// <summary>
        /// Creates a combined resilience pipeline with retry and circuit breaker.
        /// </summary>
        public ResiliencePipeline<T> CreateCombinedPipeline<T>(string operationName)
        {
            var retryStrategy = new RetryStrategyOptions<T>
            {
                MaxRetryAttempts = _options.MaxRetryAttempts,
                Delay = TimeSpan.FromSeconds(_options.RetryDelaySeconds),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "Retry {AttemptNumber}/{MaxAttempts} for '{Operation}': {Exception}",
                        args.AttemptNumber,
                        _options.MaxRetryAttempts,
                        operationName,
                        args.Outcome.Exception?.Message);
                    return default;
                }
            };

            var circuitBreakerStrategy = new CircuitBreakerStrategyOptions<T>
            {
                FailureRatio = 0.5,
                MinimumThroughput = _options.CircuitBreakerThreshold,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = _options.CircuitBreakerDuration
            };

            var timeoutStrategy = new Polly.Timeout.TimeoutStrategyOptions
            {
                Timeout = _options.OperationTimeout,
                OnTimeout = args =>
                {
                    _logger.LogError(
                        "Operation '{Operation}' timed out after {Timeout}",
                        operationName,
                        _options.OperationTimeout);
                    return default;
                }
            };

            return new ResiliencePipelineBuilder<T>()
                .AddRetry(retryStrategy)
                .AddCircuitBreaker(circuitBreakerStrategy)
                .AddTimeout(timeoutStrategy)
                .Build();
        }
    }

    /// <summary>
    /// Configuration options for resilience policies.
    /// </summary>
    public class ResiliencePolicyOptions
    {
        public int MaxRetryAttempts { get; set; } = 3;
        public int RetryDelaySeconds { get; set; } = 2;
        public int CircuitBreakerThreshold { get; set; } = 5;
        public TimeSpan CircuitBreakerDuration { get; set; } = TimeSpan.FromMinutes(1);
        public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromMinutes(5);
    }
}

