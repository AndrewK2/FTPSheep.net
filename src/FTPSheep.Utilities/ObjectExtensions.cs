using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using FTPSheep.Utilities;
using FTPSheep.Utilities.Exceptions;
using JetBrains.Annotations;

namespace StroiRiad.Utilities;

public static class ObjectExtensions {
    [DebuggerStepThrough]
    [System.Diagnostics.Contracts.Pure]
    public static T GetAsExpected<T>([AssertionCondition(AssertionConditionType.IS_NOT_NULL)] this T? o, string message) where T : class {
        return o.GetAsExpected(() => message);
    }
    
    [DebuggerStepThrough]
    [System.Diagnostics.Contracts.Pure]
    public static async Task<T> GetAsExpected<T>([AssertionCondition(AssertionConditionType.IS_NOT_NULL)] this Task<T?> o, string message) where T : class {
        var value = await o;
        return value.GetAsExpected(() => message);
    }

    [DebuggerStepThrough]
    [System.Diagnostics.Contracts.Pure]
    public static T GetAsExpected<T>([AssertionCondition(AssertionConditionType.IS_NOT_NULL)] this T? o, Func<string> message) where T : class {
        if(null != o) return o;
        if(message == null) throw new ArgumentNullException(nameof(message));

        throw new NullReferenceException("Unexpected null reference: {0}".F(message()));
    }
    
    [DebuggerStepThrough]
    [Pure]
    [ContractAnnotation("o:notnull=>notnull;o:null=>null")]
    public static TResult? With<TInput, TResult>(this TInput? o, Func<TInput, TResult?> evaluator) => 
        o == null ? default : evaluator(o);

    [Pure]
    [ContractAnnotation("o:notnull=>notnull;o:null=>null")]
    public static async Task<TResult?> WithAsync<TResult>(this Task<DateTimeOffset?> o, Func<DateTimeOffset, TResult> evaluator) {
        var value = await o;
        return value == null ? default : evaluator(value.Value);
    }
    
    [Pure]
    [ContractAnnotation("o:notnull=>notnull;o:null=>null")]
    public static async Task<TResult?> WithAsync<TInput, TResult>(this Task<TInput?> o, Func<TInput, TResult?> evaluator) {
        var value = await o;
        return value == null ? default : evaluator(value);
    }
    
    [Pure]
    [ContractAnnotation("o:notnull=>notnull;o:null=>null")]
    public static async Task<TResult?> ReturnAsync<TInput, TResult>(this Task<TInput?> o, Func<TInput, TResult?> evaluator, TResult failureValue) {
        var value = await o;
        return value == null ? failureValue : evaluator(value);
    }
    
    [DebuggerStepThrough]
    [Pure]
    [ContractAnnotation("o:notnull=>notnull;o:null=>null")]
    public static TResult? With<TInput, TResult>(this TInput? o, Func<TInput, TResult> evaluator) where TResult : class where TInput : struct {
        return !o.HasValue ? null : evaluator(o.Value);
    }

    [DebuggerStepThrough]
    [Pure]
    [ContractAnnotation("o:notnull=>notnull;o:null=>null")]
    public static TResult? With<TResult>(this int? o, Func<int, TResult?> evaluator) => o.HasValue ? evaluator(o.Value) : default;

    [DebuggerStepThrough]
    [Pure]
    [ContractAnnotation("o:notnull=>notnull;o:null=>null")]
    public static double? With(this double? o, Func<double, double?> evaluator) => o.HasValue ? evaluator(o.Value) : null;

    [DebuggerStepThrough]
    [Pure]
    public static TResult? With<TResult>(this DateTime? o, Func<DateTime, TResult?> evaluator) => o.HasValue ? evaluator(o.Value) : default;
    
    [DebuggerStepThrough]
    [Pure]
    public static TResult? With<TResult>(this DateTime o, Func<DateTime, TResult?> evaluator) => 
        o == default ? default : evaluator(o);

    [DebuggerStepThrough]
    [Pure]
    [ContractAnnotation("o:notnull=>notnull;o:null=>null")]
    public static TResult? With<TResult>(this decimal? o, Func<decimal, TResult?> evaluator) => o.HasValue ? evaluator(o.Value) : default;

    //[DebuggerStepThrough]
    [Pure]
    [ContractAnnotation("o:notnull=>notnull;o:null=>null")]
    public static TResult? With<TResult>(this float? o, Func<float?, TResult> evaluator) where TResult: struct => o.HasValue ? evaluator(o.Value) : null;

    [DebuggerStepThrough]
    [Pure]
    [ContractAnnotation("o:notnull=>notnull;o:null=>null")]
    public static float? With(this float? o, Func<float, float?> evaluator) => o.HasValue ? evaluator(o.Value) : null;

    [DebuggerStepThrough]
    [Pure]
    public static TResult ReturnExpected<TInput, TResult>(this TInput? o, Func<TInput, TResult> evaluator, TResult failureValue) where TResult : class where TInput : class {
        if(failureValue == null) throw new ArgumentNullException(nameof(failureValue));

        return o == null
            ? failureValue
            : evaluator(o).GetAsExpected("Evaluator result");
    }

    [return: System.Diagnostics.CodeAnalysis.NotNull]
    [DebuggerStepThrough]
    [Pure]
    public static TResult CastToExpected<TResult>(this object o) /*where TResult : class*/ {
        if(o == null) throw new ArgumentNullException(nameof(o));

        //---| has different name than Cast<> because Mono seems does not allow generic methods overload by type parameters
        if(o is TResult result) {
            return result;
        }

        var sourceType = o.GetType();
        var targetType = typeof(TResult);

        throw "Failed to cast \"{0}\" to expected \"{1}\""
            .F(sourceType.GetFriendlyName(), targetType.GetFriendlyName())
            .ToException()
            .Add("Original", sourceType.ToString)
            .Add("Expected", targetType.ToString)
            .Add("Object", o.ToString().Truncate(1024));
    }

    [return: System.Diagnostics.CodeAnalysis.NotNull]
    [DebuggerStepThrough]
    [Pure]
    public static TResult CastToExpected<TResult, TFrom>([DisallowNull] this TFrom o) where TResult : TFrom {
        if(o == null) throw new ArgumentNullException(nameof(o));

        //---| has different name than Cast<> because Mono seems does not allow generic methods overload by type parameters
        if(o is TResult result) {
            return result;
        }

        var sourceType = o.GetType();
        var targetType = typeof(TResult);

        throw "Failed to cast \"{0}\" to expected \"{1}\""
            .F(sourceType.GetFriendlyName(), targetType.GetFriendlyName())
            .ToException()
            .Add("Original", sourceType.ToString)
            .Add("Expected", targetType.ToString)
            .Add("Object", o.ToString().Truncate(1024));
    }

}