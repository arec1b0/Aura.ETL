// src/Aura.Core/Validation/PipelineConfigurationValidator.cs

using Aura.Core.Models;
using FluentValidation;

namespace Aura.Core.Validation
{
    /// <summary>
    /// Validates pipeline configuration to ensure it meets requirements before execution.
    /// </summary>
    public class PipelineConfigurationValidator : AbstractValidator<PipelineConfiguration>
    {
        public PipelineConfigurationValidator()
        {
            RuleFor(x => x.Steps)
                .NotNull()
                .WithMessage("Pipeline steps cannot be null.")
                .NotEmpty()
                .WithMessage("Pipeline must contain at least one step.");

            RuleForEach(x => x.Steps)
                .SetValidator(new StepConfigurationValidator());
        }
    }

    /// <summary>
    /// Validates individual step configuration.
    /// </summary>
    public class StepConfigurationValidator : AbstractValidator<StepConfiguration>
    {
        public StepConfigurationValidator()
        {
            RuleFor(x => x.Type)
                .NotEmpty()
                .WithMessage("Step type cannot be empty.")
                .Must(BeValidTypeFormat)
                .WithMessage("Step type must be in format 'TypeName, AssemblyName'.");

            RuleFor(x => x.Settings)
                .NotNull()
                .WithMessage("Step settings cannot be null.");
        }

        private bool BeValidTypeFormat(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                return false;
            }

            var parts = type.Split(',');
            return parts.Length == 2 && 
                   !string.IsNullOrWhiteSpace(parts[0]) && 
                   !string.IsNullOrWhiteSpace(parts[1]);
        }
    }
}

