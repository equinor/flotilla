using System.ComponentModel.DataAnnotations;

namespace Api.Utilities
{
    public static class AtexPressureValidation
    {
        public static IEnumerable<ValidationResult> Validate(float? lower, float? upper)
        {
            if (lower.HasValue && !float.IsFinite(lower.Value))
                yield return new ValidationResult(
                    "The lower pressure warning threshold must be finite.",
                    ["LowerPressureWarningThreshold"]
                );

            if (upper.HasValue && !float.IsFinite(upper.Value))
                yield return new ValidationResult(
                    "The upper pressure warning threshold must be finite.",
                    ["UpperPressureWarningThreshold"]
                );

            if (lower.HasValue && upper.HasValue && lower >= upper)
                yield return new ValidationResult(
                    "The lower pressure warning threshold must be less than the upper threshold.",
                    ["LowerPressureWarningThreshold", "UpperPressureWarningThreshold"]
                );
        }
    }
}
