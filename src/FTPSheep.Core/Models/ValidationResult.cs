namespace FTPSheep.Core.Models;

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
public sealed class ValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the validation was successful (no errors).
    /// </summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// Gets the list of validation errors.
    /// </summary>
    public List<string> Errors { get; } = new();

    /// <summary>
    /// Gets the list of validation warnings.
    /// </summary>
    public List<string> Warnings { get; } = new();

    /// <summary>
    /// Creates a successful validation result with no errors or warnings.
    /// </summary>
    /// <returns>A <see cref="ValidationResult"/> indicating success.</returns>
    public static ValidationResult Success()
    {
        return new ValidationResult();
    }

    /// <summary>
    /// Creates a failed validation result with the specified errors.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    /// <returns>A <see cref="ValidationResult"/> containing the errors.</returns>
    public static ValidationResult Failed(params string[] errors)
    {
        var result = new ValidationResult();
        foreach (var error in errors)
        {
            result.Errors.Add(error);
        }
        return result;
    }

    /// <summary>
    /// Adds an error to the validation result.
    /// </summary>
    /// <param name="error">The error message to add.</param>
    public void AddError(string error)
    {
        Errors.Add(error);
    }

    /// <summary>
    /// Adds a warning to the validation result.
    /// </summary>
    /// <param name="warning">The warning message to add.</param>
    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
    }

    /// <summary>
    /// Merges another validation result into this one.
    /// </summary>
    /// <param name="other">The validation result to merge.</param>
    public void Merge(ValidationResult other)
    {
        Errors.AddRange(other.Errors);
        Warnings.AddRange(other.Warnings);
    }
}
