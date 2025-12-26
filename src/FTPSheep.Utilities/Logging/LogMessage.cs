using System.Diagnostics;
using System.Globalization;
using System.Text;
using FTPSheep.Utilities.Exceptions;
using StroiRiad.Utilities;

namespace FTPSheep.Utilities.Logging;

public sealed class LogMessage {
    private readonly Action<string> logger;

    private readonly string? message;

    private readonly Func<string>? messageGetter;

    private readonly IDictionary<string, string?> data;

    public LogMessage(Action<string> logger, string message) : this(logger) {
        ArgumentNullException.ThrowIfNull(logger);

        this.message = message ?? throw new ArgumentNullException(nameof(message));
    }

    public LogMessage(Action<string> logger, Func<string> message) : this(logger) {
        ArgumentNullException.ThrowIfNull(logger);

        messageGetter = message ?? throw new ArgumentNullException(nameof(message));
    }

    private LogMessage(Action<string> logger) {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

        data = new Dictionary<string, string?>();
    }

    public void Write() {
        var msg = new StringBuilder();

        if(message != null) {
            msg.Append(message);
        }

        if(Enumerable.Any(data)) {
            msg
                .AppendLine()
                .AppendLine("--- Data ---");

            foreach(var pair in data) {
                msg
                    .Append(pair.Key)
                    .Append(": ");

                if(ExceptionExtensions.IsMultiline(pair.Value)) {
                    msg.Append("\n");
                    msg.Append(ExceptionExtensions.PadLines(pair.Value, "\t", 1, "\n"));
                } else {
                    msg.Append(pair.Value);
                }

                msg.AppendLine();
            }
        }

        this.logger(msg.ToString());
        /*
        switch(entryType) {
            case EntryType.Trace:
                logger.WriteError(msg.ToString());
                break;
            case EntryType.Debug:
                logger.Debug?.Write(msg.ToString());
                break;
            case EntryType.Information:
                logger.WriteInfo(msg.ToString());
                break;
            case EntryType.Warning:
                logger.WriteWarn(msg.ToString());
                break;
            case EntryType.Error:
                logger.WriteError(msg.ToString());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(entryType), entryType, "Unsupported entry type");
        }*/
    }

    public LogMessage AddXml(string key, string? xml) {
        if(key == null) throw new ArgumentNullException(nameof(key));

        return Add(key, FormatXml(xml));
    }

    public LogMessage AddAsJson(string key, object? value) => Add(key, () => JsonUtils.SerializeObject(value, true, true));

    public LogMessage Add(string key, string? value) => AddInternal(key, value);

    public LogMessage Add(string key, IEnumerable<string?>? value, string? separator = null) => AddInternal(key, value?.Implode(separator ?? "\n"));

    public LogMessage Add(string key, int value) => AddInternal(key, value);

    public LogMessage Add(string key, int? value) => AddInternal(key, value);

    public LogMessage Add(string key, long value) => AddInternal(key, value);

    public LogMessage Add(string key, uint value) {
        if(key == null) throw new ArgumentNullException(nameof(key));

        return AddInternal(key, value);
    }

    public LogMessage Add(string key, Func<string?> valueFunction) {
        if(key == null) throw new ArgumentNullException(nameof(key));
        if(valueFunction == null) throw new ArgumentNullException(nameof(valueFunction));

        string? value;
        try {
            //BUG: do not evaluate until written
            value = valueFunction();
        } catch(Exception ex2) {
            value = "Error occurred: " + ex2.GetBrief();
        }

        return AddInternal(key, value);
    }

    public LogMessage Add(string key, Lazy<string?> valueGetter) {
        if(valueGetter == null) throw new ArgumentNullException(nameof(valueGetter));

        string? value;
        try {
            value = valueGetter.Value;
        } catch(Exception ex2) {
            value = "Error occurred: " + ex2.GetBrief();
        }

        return AddInternal(key, value);
    }

    public LogMessage AddXml(string key, Func<string?> valueFunction) {
        if(key == null) throw new ArgumentNullException(nameof(key));
        if(valueFunction == null) throw new ArgumentNullException(nameof(valueFunction));

        string? value;
        try {
            value = valueFunction();
            return AddXml(key, value);
        } catch(Exception ex) {
            value = "Error occurred: " + ex.GetBrief();
            return Add(key, value);
        }
    }

    public LogMessage Add(string key, Guid value) {
        if(key == null) throw new ArgumentNullException(nameof(key));

        return Add(key, value.ToString());
    }

    public LogMessage Add(string key, TimeSpan value) {
        return Add(key, value.ToString);
    }

    private LogMessage AddInternal(string key, object? value) {
        if(key == null) throw new ArgumentNullException(nameof(key));

        try {
            data[key] = value?.ToString();
        } catch(Exception e) {
            data[$"{key} (error)"] = "Failed to add exception parameter: " + e.Message;
        }

        return this;
    }

    [DebuggerStepThrough]
    private static string? FormatXml(string? xml) {
        return xml;
    }

    public LogMessage Add(string key, DateTime value) {
        if(key == null) throw new ArgumentNullException(nameof(key));

        return Add(key, value.ToString(CultureInfo.InvariantCulture));
    }

    public LogMessage Add(string key, DateTimeOffset? value) {
        if(key == null) throw new ArgumentNullException(nameof(key));

        return Add(key, value?.ToString(CultureInfo.InvariantCulture));
    }

    public LogMessage Add(string key, bool value) {
        if(key == null) throw new ArgumentNullException(nameof(key));

        return Add(key, value.ToString(CultureInfo.InvariantCulture));
    }

    public LogMessage Add(string key, Enum value) {
        if(key == null) throw new ArgumentNullException(nameof(key));
        if(value == null) throw new ArgumentNullException(nameof(value));

        return Add(key, value.ToString);
    }

    public LogMessage Add(string key, Uri? value) {
        if(key == null) throw new ArgumentNullException(nameof(key));

        return Add(key, value?.ToString());
    }
}