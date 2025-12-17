using FTPSheep.Utilities.Exceptions;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using StroiRiad.Utilities;

namespace FTPSheep.Utilities.Logging;

[PublicAPI]
public static class LoggerExtensions {
    public const string ExceptionBriefContextKey = "ExceptionBrief";

    [Pure]
    public static LogMessage BuildInfoMessage(this ILogger logger, string message) {
        if(logger == null) throw new ArgumentNullException(nameof(logger));
        if(message == null) throw new ArgumentNullException(nameof(message));

        return new LogMessage(msg => logger.LogInformation(msg), message);
    }

    [Pure]
    [StringFormatMethod("format")]
    public static LogMessage BuildInfoMessage(this ILogger logger, string format, params object?[] args) {
        if(logger == null) throw new ArgumentNullException(nameof(logger));
        if(format == null) throw new ArgumentNullException(nameof(format));

        return new LogMessage(msg => logger.LogInformation(msg), string.Format(format, args));
    }

    [Pure]
    public static LogMessage BuildWarnMessage(this ILogger logger, string message) {
        if(logger == null) throw new ArgumentNullException(nameof(logger));
        if(message == null) throw new ArgumentNullException(nameof(message));

        return new LogMessage(msg => logger.LogWarning(msg), message);
    }

    [Pure]
    [StringFormatMethod("format")]
    public static LogMessage BuildWarnMessage(this ILogger logger, string message, params object?[] args) {
        if(logger == null) throw new ArgumentNullException(nameof(logger));
        if(message == null) throw new ArgumentNullException(nameof(message));

        //TODO: truncate long arguments to prevent log flooded with user-submitted data
        return new LogMessage(msg => logger.LogWarning(msg), string.Format(message, args));
    }

    [Pure]
    public static LogMessage BuildErrorMessage(this ILogger logger, string message) {
        if(logger == null) throw new ArgumentNullException(nameof(logger));
        if(message == null) throw new ArgumentNullException(nameof(message));

        return new LogMessage(msg => logger.LogError(msg), message);
    }

    [Pure]
    [StringFormatMethod("format")]
    public static LogMessage BuildErrorMessage(this ILogger logger, string message, params object?[] args) {
        if(logger == null) throw new ArgumentNullException(nameof(logger));
        if(message == null) throw new ArgumentNullException(nameof(message));

        return new LogMessage(msg => logger.LogError(msg), string.Format(message, args));
    }

    [Pure]
    public static LogMessage BuildDebugMessage(this ILogger logger, string message) {
        if(logger == null) throw new ArgumentNullException(nameof(logger));
        if(message == null) throw new ArgumentNullException(nameof(message));

        return new LogMessage(msg => logger.LogDebug(msg), message);
    }

    [Pure]
    public static LogMessage BuildTraceMessage(this ILogger logger, string message) {
        if(logger == null) throw new ArgumentNullException(nameof(logger));
        if(message == null) throw new ArgumentNullException(nameof(message));

        return new LogMessage(msg => logger.LogTrace(msg), message);
    }

    [Pure]
    [StringFormatMethod("format")]
    public static LogMessage BuildDebugMessage(this ILogger logger, string format, params object?[] args) {
        if(logger == null) throw new ArgumentNullException(nameof(logger));
        if(format == null) throw new ArgumentNullException(nameof(format));

        return new LogMessage(msg => logger.LogDebug(msg), string.Format(format, args));
    }

    public static void LogException(this ILogger logger, Exception ex, string? message = null, bool asWarning = false) {
        var targetMessage = message.With(m => m + "\n") + ex.GetDescription();
        var scopeData = new[] { new KeyValuePair<string, object>(ExceptionBriefContextKey, ex.GetBrief()) };

        using(logger.BeginScope(scopeData)) {
            if(asWarning) {
                logger.LogWarning("{msg}", targetMessage);
            } else {
                logger.LogError("{msg}", targetMessage);
            }
        }
    }
}