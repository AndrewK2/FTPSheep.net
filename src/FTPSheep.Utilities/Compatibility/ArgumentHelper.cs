#if NET48
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace FTPSheep.Utilities.Compatibility;

/// <summary>
/// Provides argument validation helpers for .NET Framework 4.8
/// </summary>
internal static class ArgumentHelper
{
    /// <summary>
    /// Throws an ArgumentNullException if argument is null
    /// </summary>
    public static void ThrowIfNull([NotNull] object? argument, [CallerArgumentExpression("argument")] string? paramName = null)
    {
        if (argument is null)
        {
            throw new ArgumentNullException(paramName);
        }
    }

    /// <summary>
    /// Throws an ArgumentOutOfRangeException if value is negative or zero
    /// </summary>
    public static void ThrowIfNegativeOrZero(int value, [CallerArgumentExpression("value")] string? paramName = null)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, value, "Value must be positive");
        }
    }
}

/// <summary>
/// Polyfill for CallerArgumentExpressionAttribute
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
internal sealed class CallerArgumentExpressionAttribute : Attribute
{
    public CallerArgumentExpressionAttribute(string parameterName)
    {
        ParameterName = parameterName;
    }

    public string ParameterName { get; }
}
#endif
