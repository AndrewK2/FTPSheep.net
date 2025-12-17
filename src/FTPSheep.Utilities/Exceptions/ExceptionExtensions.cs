using System.Collections;
using System.Globalization;
using System.Text;
using StroiRiad.Utilities;

namespace FTPSheep.Utilities.Exceptions;

public static class ExceptionExtensions {
    private const int MaxDataLength = 3 * 1024;

    extension(string message) {
        public Exception ToException(Exception? ex = null) {
            return new Exception(message, ex);
        }

        public Exception ToUserFriendlyException(Exception? ex = null) {
            return new UserFriendlyException(message, ex);
        }
    }

    private static Exception AddInternal(Exception ex, string key, object? value) {
        if(ex == null) throw new ArgumentNullException(nameof(ex));
        if(key == null) throw new ArgumentNullException(nameof(key));

        try {
            ex.Data[key] = value?.ToString();
        } catch(Exception e) {
            ex.Data["{0} (error)".F(key)] = "Failed to add exception parameter: {0}".F(e.Message);
            ex.Data[key] = value?.ToString() ?? string.Empty;
        }

        return ex;
    }

    extension(Exception ex) {
        public Exception Add(string key, string? value, int truncateLength = MaxDataLength) {
            if(ex == null) throw new ArgumentNullException(nameof(ex));
            if(key == null) throw new ArgumentNullException(nameof(key));
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(truncateLength);

            return AddInternal(ex, key, value.Truncate(truncateLength));
        }

        public Exception Add(string key, Func<string?> valueFunction) {
            if(ex == null) throw new ArgumentNullException(nameof(ex));
            if(key == null) throw new ArgumentNullException(nameof(key));
            if(valueFunction == null) throw new ArgumentNullException(nameof(valueFunction));

            string? value;
            try {
                value = valueFunction();
            } catch(Exception ex2) {
                value = "Error occurred: " + ex2;
            }

            return AddInternal(ex, key, value);
        }

        public Exception Add(string key, IEnumerable<string?>? enumerable, int maxLength = MaxDataLength) {
            if(ex == null) throw new ArgumentNullException(nameof(ex));
            if(key == null) throw new ArgumentNullException(nameof(key));

            string value;
            if(null == enumerable) {
                value = "null";
            } else {
                try {
                    value = 0 == maxLength
                        ? enumerable.ImplodeNewLine()
                        : enumerable.Select(s => s.Truncate(maxLength)).ImplodeNewLine();
                } catch(Exception ex2) {
                    value = "Error occurred: " + ex2;
                }
            }

            return AddInternal(ex, key, value);
        }

        public Exception Add(string key, IEnumerable<int>? enumerable, string? separator = null) {
            if(ex == null) throw new ArgumentNullException(nameof(ex));
            if(key == null) throw new ArgumentNullException(nameof(key));

            string value;
            if(null == enumerable) {
                value = "null";
            } else {
                try {
                    return ex.Add(key, separator is null
                        ? enumerable.Select(e => e.ToString()).ImplodeNewLine
                        : enumerable.Select(e => e.ToString()).ImplodeComma
                    );
                } catch(Exception ex2) {
                    value = "Error occurred: " + ex2;
                }
            }

            return AddInternal(ex, key, value);
        }

        public Exception Add(string key, IEnumerable<KeyValuePair<string, string>>? pairs, int maxLength = MaxDataLength) {
            if(ex == null) throw new ArgumentNullException(nameof(ex));
            if(key == null) throw new ArgumentNullException(nameof(key));

            if(null == pairs) {
                return ex.Add(key, "null");
            }

            string value;
            try {
                value = pairs.Select(p => p.Key + "=" + p.Value.Truncate(maxLength)).ImplodeNewLine();
            } catch(Exception ex2) {
                value = "Error occurred: " + ex2;
            }

            return AddInternal(ex, key, value);
        }

        public Exception Add(string key, int value) {
            if(ex == null) throw new ArgumentNullException(nameof(ex));
            if(key == null) throw new ArgumentNullException(nameof(key));

            return ex.Add(key, value.ToString(CultureInfo.InvariantCulture));
        }

        public Exception Add(string key, bool value) {
            if(ex == null) throw new ArgumentNullException(nameof(ex));
            if(key == null) throw new ArgumentNullException(nameof(key));

            return ex.Add(key, value.ToString());
        }

        public Exception Add(string key, bool? value) {
            if(ex == null) throw new ArgumentNullException(nameof(ex));
            if(key == null) throw new ArgumentNullException(nameof(key));

            return ex.Add(key, value?.ToString());
        }

        public Exception Add(string key, int? value) {
            if(ex == null) throw new ArgumentNullException(nameof(ex));
            if(key == null) throw new ArgumentNullException(nameof(key));

            return AddInternal(ex, key, value);
        }

        public string GetDescription(bool includeStackTraces = true) {
            try {
                var messageBuilder = new StringBuilder()
                    .AppendLine(ex.GetBrief())
                    .AppendLine()
                    .AppendLine(ex.GetDescriptionInternal(includeStackTraces));

                return messageBuilder.ToString();
            } catch(Exception ex1) {
                return ex + "\n------------\n" + ex1;
            }
        }

        private string GetDescriptionInternal(bool includeStackTraces = true) {
            if(ex is AggregateException aex) {
                ex = aex.InnerException ?? ex;
            }

            var messageBuilder = new StringBuilder();

            messageBuilder
                .AppendLine("Message: {0}{1}".F(IsMultiline(ex.Message) ? Environment.NewLine : string.Empty, ex.Message))
                .AppendLine()
                .AppendLine("Exception: " + ex.GetType());

            switch(ex) {
                case ArgumentOutOfRangeException argEx1:
                    messageBuilder
                        .AppendLine("Parameter Name: " + argEx1.ParamName)
                        .AppendLine("Actual Value: " + argEx1.ActualValue);
                    break;
                case AggregateException agrEx: {
                    if(agrEx.InnerExceptions != null) {
                        messageBuilder
                            .AppendLine("Inner Exceptions ({0}): ".F(agrEx.InnerExceptions.Count));

                        var cnt = 0;
                        foreach(var innerException in agrEx.InnerExceptions) {
                            messageBuilder
                                .AppendLine("Exception #{0}: " + cnt++)
                                .AppendLine(PadLines(innerException.GetDescriptionInternal(), "\t", 1));
                        }
                    }

                    break;
                }
                case ArgumentException argEx:
                    messageBuilder
                        .AppendLine("Parameter Name: " + argEx.ParamName);
                    break;
            }

            try {
                if(ex.Data.Keys.Count > 0) {
                    messageBuilder.AppendLine("Exception Data: {");

                    foreach(DictionaryEntry dataEntry in ex.Data) {
                        messageBuilder
                            .Append("\t")
                            .Append(dataEntry.Key)
                            .Append(": ");

                        if(dataEntry.Value != null) {
                            string? value;
                            try {
                                value = dataEntry.Value?.ToString();
                            } catch {
                                try {
                                    value = dataEntry.Value?.ToString();
                                } catch {
                                    value = string.Concat(dataEntry.Value, "(failed to serialize)");
                                }
                            }

                            messageBuilder.AppendLine(IsMultiline(value) ? Environment.NewLine + PadLines(value, "\t", 2) : value);
                        } else {
                            messageBuilder.AppendLine("NULL");
                        }
                    }

                    messageBuilder.AppendLine("}");
                }
            } catch(Exception e) {
                messageBuilder.AppendLine("Exception Data: {");
                messageBuilder.AppendLine("Failed to serialize: ");
                messageBuilder.AppendLine("}");
                messageBuilder.AppendLine(e.GetBrief());
            }

            if(includeStackTraces) {
                messageBuilder
                    .AppendLine("Trace:")
                    .AppendLine(ex.StackTrace);
            }

            if(ex.InnerException != null) {
                messageBuilder
                    .AppendLine("------ Inner Exception ------")
                    .AppendLine(ex.InnerException.GetDescriptionInternal(includeStackTraces));
            }

            return messageBuilder.ToString();
        }

        public string GetBrief() {
            if(ex == null) throw new ArgumentNullException(nameof(ex));

            var message = new StringBuilder();

            if(ex is AggregateException agr) {
                ex = agr.InnerException ?? ex;
            }

            string trimmedMessage;
            const string defaultArgumentNullExceptionMessage = "Value cannot be null.";
            const string defaultArgumentOutOfRangeExceptionMessage = "Specified argument was out of the range of valid values.";
            if(ex is ArgumentNullException ane && defaultArgumentNullExceptionMessage == ane.Message.GetFirstLine() && ane.ParamName != null) {
                //replace default message with more detailed one
                trimmedMessage = $"Argument \"{ane.ParamName}\" value cannot be null.";
            } else {
                if(ex is ArgumentOutOfRangeException aore && defaultArgumentOutOfRangeExceptionMessage == aore.Message.GetFirstLine() && aore.ParamName != null) {
                    //replace default message with more detailed one
                    trimmedMessage = aore.ActualValue != null
                        ? $"Specified \"{aore.ParamName}\" argument \"{aore.ActualValue?.ToString().Truncate(32)}\" was out of the range of valid values."
                        : $"Specified \"{aore.ParamName}\" argument was out of the range of valid values.";
                } else {
                    trimmedMessage = ex.Message.TrimStart();
                }
            }

            string? targetMessage;
            try {
                using var r = new StringReader(trimmedMessage);
                targetMessage = r.ReadLine();
            } catch {
                targetMessage = trimmedMessage;
            }

            message.AppendLine($"-> {targetMessage}");

            if(ex.InnerException != null) {
                message.Append(ex.InnerException.GetBrief());
            }

            return message.ToString().Trim();
        }

        public Exception Add(string key, IEnumerable<Uri>? enumerable, int maxLength = MaxDataLength) {
            if(ex == null) throw new ArgumentNullException(nameof(ex));
            if(key == null) throw new ArgumentNullException(nameof(key));

            string value;
            if(null == enumerable) {
                value = "null";
            } else {
                try {
                    value = enumerable.Select(ConvertUriToString).ImplodeNewLine();
                } catch(Exception ex2) {
                    value = "Error occurred: " + ex2;
                }
            }

            return AddInternal(ex, key, value);

            string ConvertUriToString(Uri? uri) {
                if(uri == null) {
                    return "NULL";
                }

                try {
                    return (uri.IsAbsoluteUri ? uri.AbsoluteUri : uri.ToString()).Truncate(maxLength);
                } catch {
                    return uri.ToString().Truncate(maxLength);
                }
            }
        }

        public Exception AddData(IEnumerable<KeyValuePair<string, Func<string?>>> items) {
            if(ex == null) throw new ArgumentNullException(nameof(ex));
            if(items == null) throw new ArgumentNullException(nameof(items));

            foreach(var pair in items) {
                ex.Add(pair.Key, pair.Value);
            }

            return ex;
        }

        public Exception Add(IDictionary<string, object?> data) {
            if(ex == null) throw new ArgumentNullException(nameof(ex));
            if(data == null) throw new ArgumentNullException(nameof(data));

            foreach(var p in data.Where(p => null != p.Key)) {
                AddInternal(ex, p.Key, p.Value);
            }

            return ex;
        }

        public Exception Add(string key, TimeSpan value) {
            if(ex == null) throw new ArgumentNullException(nameof(ex));
            if(key == null) throw new ArgumentNullException(nameof(key));

            return AddInternal(ex, key, value);
        }

        public Exception Add(string key, Type? value) {
            if(ex == null) throw new ArgumentNullException(nameof(ex));
            if(key == null) throw new ArgumentNullException(nameof(key));

            return AddInternal(ex, key, value?.GetFriendlyName());
        }
    }

    extension(Exception? ex) {
        public T? FindExceptionByType<T>() where T : Exception {
            if(null == ex) return null;

            if(ex is T type) {
                return type;
            }

            var agr = ex as AggregateException;
            var found = agr?.InnerExceptions
                .Select(e => e.FindExceptionByType<T>())
                .FirstOrDefault();

            return found ?? ex.InnerException?.FindExceptionByType<T>();
        }

        public T? FindFirstThrownExceptionByType<T>() where T : Exception {
            if(null == ex) return null;

            T? found;

            if(ex is T type) {
                found = type;
            } else {
                var agr = ex as AggregateException;

                found = agr?.InnerExceptions
                    .Select(e => e.FindExceptionByType<T>())
                    .FirstOrDefault();
            }

            var nextFound = ex.InnerException?.FindFirstThrownExceptionByType<T>();

            return nextFound ?? found;
        }

        public T? FindFirstExceptionByType<T>() where T : class /*Exception*/ {
            if(null == ex) return null;

            var agr = ex as AggregateException;
            var found = agr?.InnerExceptions
                .Select(e => e.FindFirstExceptionByType<T>())
                .FirstOrDefault();

            if(found != null) return found;

            return null == ex.InnerException
                ? ex as T
                : ex.InnerException.FindFirstExceptionByType<T>() ?? ex as T;
        }
    }

    extension(Exception ex) {
        public Exception GetMostBottom() {
            if(ex == null) throw new ArgumentNullException(nameof(ex));

            return ex.InnerException?.GetMostBottom() ?? ex;
        }

        public Exception Add(string key, DateTime value) {
            if(ex == null) throw new ArgumentNullException(nameof(ex));
            if(key == null) throw new ArgumentNullException(nameof(key));

            return ex.Add(key, value.ToString("yy-MM-dd HH:mm:ss.fff"));
        }

        public Exception Add(string key, DateTimeOffset? value) {
            if(ex == null) throw new ArgumentNullException(nameof(ex));
            if(key == null) throw new ArgumentNullException(nameof(key));

            return ex.Add(key, value?.ToString("yy-MM-dd HH:mm:ss.fff"));
        }

        public Exception Add(string key, Guid value) {
            if(ex == null) throw new ArgumentNullException(nameof(ex));
            if(key == null) throw new ArgumentNullException(nameof(key));

            return ex.Add(key, value.ToString());
        }

        public Exception Add(string key, long value) => ex.Add(key, value.ToString(CultureInfo.InvariantCulture));
        public Exception Add(string key, decimal value) => ex.Add(key, value.ToString(CultureInfo.InvariantCulture));

        public Exception Add(string key, Enum value) {
            if(ex == null) throw new ArgumentNullException(nameof(ex));
            if(key == null) throw new ArgumentNullException(nameof(key));
            if(value == null) throw new ArgumentNullException(nameof(value));

            return ex.Add(key, "{0} ({1})".F(value, value.GetType().FullName));
        }

        public Exception Add(string key, StringBuilder value) {
            if(ex == null) throw new ArgumentNullException(nameof(ex));
            if(key == null) throw new ArgumentNullException(nameof(key));
            if(value == null) throw new ArgumentNullException(nameof(value));

            return ex.Add(key, value.ToString());
        }

        public Exception AddXml(string key, string? xml) {
            if(ex == null) throw new ArgumentNullException(nameof(ex));
            if(key == null) throw new ArgumentNullException(nameof(key));

            return ex.Add(key, xml);
        }

        public Exception AddAsJson(string key, object? value, int truncatedLength = 4 * 1024) {
            if(ex == null) throw new ArgumentNullException(nameof(ex));
            if(key == null) throw new ArgumentNullException(nameof(key));

            return ex.Add(key, JsonUtils.SerializeObject(value, indented: true), truncatedLength);
        }
    }

    internal static bool IsMultiline(string? value) => null != value && value.Contains('\n', StringComparison.OrdinalIgnoreCase);

    internal static string PadLines(string? source, string padString, int amount, string? newLine = null) {
        if(source == null) throw new ArgumentNullException(nameof(source));
        if(padString == null) throw new ArgumentNullException(nameof(padString));

        var result = new StringBuilder();
        using(var stringReader = new StringReader(source)) {
            var notTheFirst = false;
            while(stringReader.ReadLine() is { } line) {
                if(notTheFirst) {
                    result.Append(newLine ?? Environment.NewLine);
                }

                result.Append(Repeat(amount, padString));
                result.Append(line);

                notTheFirst = true;
            }
        }

        return result.ToString();
    }

    private static string Repeat(int count, string strToRepeat) {
        if(strToRepeat == null) throw new ArgumentNullException(nameof(strToRepeat));

        if(count <= 0) {
            return string.Empty;
        }

        var target = new StringBuilder(strToRepeat.Length * count);
        for(var i = 0; i < count; i++) {
            target.Append(strToRepeat);
        }

        return target.ToString();
    }
}