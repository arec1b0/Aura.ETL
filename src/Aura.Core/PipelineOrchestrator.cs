// src/Aura.Core/PipelineOrchestrator.cs

using Aura.Abstractions;
using Aura.Core.Interfaces;
using Aura.Core.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Core
{
    public class PipelineOrchestrator
    {
        private readonly IStepFactory _stepFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="PipelineOrchestrator"/> class.
        /// </summary>
        /// <param name="stepFactory">The factory used to create pipeline step instances.</param>
        public PipelineOrchestrator(IStepFactory stepFactory)
        {
            _stepFactory = stepFactory ?? throw new ArgumentNullException(nameof(stepFactory));
        }

        public async Task ExecuteAsync(PipelineConfiguration config, CancellationToken cancellationToken)
        {
            // The initial context is a non-specific object payload.
            object currentContext = new DataContext<object>(new object());

            foreach (var stepConfig in config.Steps)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Console.WriteLine($"Executing step: {stepConfig.Type}");
                
                // 1. Delegate instantiation to the factory (DIP).
                var stepInstance = _stepFactory.CreateStep(stepConfig);

                // 2. Execute the step using dynamic dispatch to call the correct generic ExecuteAsync method.
                currentContext = await ((dynamic)stepInstance).ExecuteAsync((dynamic)currentContext, cancellationToken);
                
                Console.WriteLine($"Step {stepConfig.Type} completed.");
            }

            Console.WriteLine("Pipeline execution finished successfully.");
        }
    }
}