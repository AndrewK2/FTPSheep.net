using System.Diagnostics;
using System.Globalization;
using System.Text;
using FTPSheep.Utilities.Exceptions;
using JetBrains.Annotations;

namespace FTPSheep.Utilities;

public static class StringExtensions {
    extension(string? source) {
        [System.Diagnostics.Contracts.Pure]
        [DebuggerStepThrough]
        public DateTimeOffset? ToDateTimeOffset() {
            if(string.IsNullOrWhiteSpace(source)) {
                return null;
            }

            return !DateTimeOffset.TryParse(source, out var value)
                ? null
                : value;
        }

        [System.Diagnostics.Contracts.Pure]
        [DebuggerStepThrough]
        public Uri? ToUri(UriKind kind = UriKind.RelativeOrAbsolute) {
            if(string.IsNullOrWhiteSpace(source)) {
                return null;
            }

            return !Uri.TryCreate(source, kind, out var value)
                ? null
                : value;
        }

        [DebuggerStepThrough]
        [System.Diagnostics.Contracts.Pure]
        public Uri ToExpectedUri(UriKind kind = UriKind.RelativeOrAbsolute) {
            if(Uri.TryCreate(source, kind, out var value)) {
                Debug.Assert(value != null, "value != null");
                return value;
            }

            throw new Exception($"Given string is not an URI: \"{source}\"");
        }

        [DebuggerStepThrough]
        public Guid ToExpectedGuid() {
            if(Guid.TryParse(source, out var guid)) {
                return guid;
            }

            throw "Given string is not a GUID: {0}"
                .F(source != null ? "\"" + source.Truncate(255) + "\"" : "null")
                .ToException();
        }

        [DebuggerStepThrough]
        public Guid ToGuidOrDefault() => Guid.TryParse(source, out var guid) ? guid : default;

        [DebuggerStepThrough, System.Diagnostics.Contracts.Pure]
        public int ToExpectedInt(Func<string>? error = null) {
            if(source == null) {
                throw new ArgumentNullException(nameof(source), "Can't convert null string value to expected int");
            }

            if(int.TryParse(source, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var parsed)) {
                return parsed;
            }

            var message = error != null
                ? "{0}. Unable to convert \"{1}\" value to expected integer".F(error(), source.Truncate(256))
                : "Unable to convert \"{0}\" value to expected integer".F(source.Truncate(256));

            throw message.ToException();
        }

        [DebuggerStepThrough]
        [System.Diagnostics.Contracts.Pure]
        public int ToIntOrDefault(int @default = 0) {
            return !int.TryParse(source, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var result) ? @default : result;
        }

        [DebuggerStepThrough]
        [System.Diagnostics.Contracts.Pure]
        public decimal ToDecimalOrDefault(decimal @default = 0) {
            return !decimal.TryParse(source, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var result) ? @default : result;
        }

        [DebuggerStepThrough]
        [System.Diagnostics.Contracts.Pure]
        public double ToDoubleOrDefault(double @default = 0) {
            return !double.TryParse(source, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var result) ? @default : result;
        }

        [DebuggerStepThrough, System.Diagnostics.Contracts.Pure]
        public float ToExpectedFloat(Func<string>? error = null) {
            if(source == null) {
                throw new ArgumentNullException(nameof(source), "Can't convert null string value to expected int");
            }

            if(float.TryParse(source, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var parsed)) {
                return parsed;
            }

            var message = error != null
                ? "{0}. Unable to convert \"{1}\" value to expected float".F(error(), source.Truncate(256))
                : "Unable to convert \"{0}\" value to expected float".F(source.Truncate(256));

            throw message.ToException();
        }

        [DebuggerStepThrough, System.Diagnostics.Contracts.Pure]
        public decimal ToExpectedDecimal(Func<string>? error = null) {
            if(source == null) {
                throw new ArgumentNullException(nameof(source), "Can't convert null string value to expected decimal");
            }

            if(decimal.TryParse(source, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var parsed)) {
                return parsed;
            }

            var message = error != null
                ? "{0}. Unable to convert \"{1}\" value to expected decimal".F(error(), source.Truncate(256))
                : "Unable to convert \"{0}\" value to expected decimal".F(source.Truncate(256));

            throw message.ToException();
        }

        [DebuggerStepThrough]
        public string Truncate(int length, string? truncatedTail = "…") {
            if(null == source) {
                return string.Empty;
            }

            if(0 == length) {
                return source;
            }

            if(source.Length <= length) {
                return source;
            }

            var tailLength = string.IsNullOrEmpty(truncatedTail) ? 0 : truncatedTail.Length;
            var truncated = source.Substring(0, length - tailLength);

            return 0 == tailLength
                ? truncated
                : string.Concat(truncated, truncatedTail);
        }
    }
    
    extension(Task<string?> s) {
        [DebuggerStepThrough]
        public async Task<Guid> ToGuidOrDefault() => Guid.TryParse(await s, out var guid) ? guid : default;

        [DebuggerStepThrough]
        public async Task<bool> ToBooleanOrDefault(bool defaultValue = false) {
            return !bool.TryParse(await s, out var boolean) ? defaultValue : boolean;
        }
    }


    [System.Diagnostics.Contracts.Pure]
    [StringFormatMethod("s")]
    [DebuggerStepThrough]
    public static string F(this string s, params object?[] args) {
        try {
            return string.Format(s, args);
        } catch(Exception ex) {
            throw new Exception($"Failed to format \"{s}\" string", ex);
        }
    }

    [StringFormatMethod("s")]
    [DebuggerStepThrough]
    public static string F(this string s, params string?[] args) {
        try {
            return string.Format(s, args);
        } catch(Exception ex) {
            throw new Exception($"Failed to format \"{s}\" string", ex);
        }
    }

    [System.Diagnostics.Contracts.Pure]
    public static string ImplodeNewLine(this IEnumerable<string?>? array) {
        return null == array
            ? string.Empty
            : Implode(Environment.NewLine, array);
    }

    /// <param name="glue"></param>
    /// <param name="dataArray"></param>
    /// <returns></returns>
    private static string Implode(string? glue, IEnumerable<string?>? dataArray) {
        return dataArray switch {
            null => string.Empty,
            ICollection<string> { Count: 1 } or IReadOnlyCollection<string> { Count: 1 } => dataArray.First() ?? string.Empty,
            _ => string.Join(glue, dataArray)
        };
    }

    [ContractAnnotation("source:null=>null;source:notnull=>notnull")]
    public static string? GetFirstLine(this string? source) {
        if(string.IsNullOrEmpty(source)) {
            return source;
        }

        Debug.Assert(source != null, "message != null");
        using var textReader = new StringReader(source);
        return textReader.ReadLine();
    }

    /// <summary>
    /// Omits seconds, if zero
    /// </summary>
    /// <param name="ts"></param>
    /// <param name="includeSeconds"></param>
    /// <param name="addInAgo">add "in"/"ago" strings</param>
    /// <param name="roundToSecondsIfLessThanSeconds"></param>
    /// <param name="roundToMinutesIfLessThanMinutes"></param>
    /// <param name="roundToHoursIfLessThanHours"></param>
    /// <returns></returns>
    public static string TimeSpanToString(this TimeSpan ts, bool includeSeconds = true, bool addInAgo = false, uint roundToSecondsIfLessThanSeconds = 0, uint roundToMinutesIfLessThanMinutes = 0, uint roundToHoursIfLessThanHours = 0, string? agoLabel = "ago", string? inLabel = "in") {
        var stringBuilder = new StringBuilder();

        var isAgo = ts < TimeSpan.Zero;

        if(isAgo) {
            ts = ts.Negate();
        }

        if(addInAgo && !isAgo) {
            stringBuilder.Append(inLabel);
            stringBuilder.Append(' ');
        }

        if(Math.Round(ts.TotalSeconds) > 0) {
            WriteTime();
        } else {
            stringBuilder.Append("0:00");
        }

        if(addInAgo && isAgo) {
            stringBuilder.Append(' ');
            stringBuilder.Append(agoLabel);
        }

        return stringBuilder.ToString();

        static string Pluralize(int count, string singular, string plural) => count == 1 ? singular : plural;

        void WriteTime() {
            if(roundToSecondsIfLessThanSeconds > 0 && ts.TotalSeconds <= roundToSecondsIfLessThanSeconds) {
                var seconds = (int)Math.Floor(ts.TotalSeconds);
                stringBuilder.AppendFormat("{0} {1}", seconds, Pluralize(seconds, "second", "seconds"));
                return;
            }

            if(roundToMinutesIfLessThanMinutes > 0 && ts.TotalMinutes <= roundToMinutesIfLessThanMinutes) {
                var minutes = (int)Math.Floor(ts.TotalMinutes);
                stringBuilder.AppendFormat("{0} {1}", minutes, Pluralize(minutes, "minute", "minutes"));
                return;
            }

            if(roundToHoursIfLessThanHours > 0 && ts.TotalHours <= roundToHoursIfLessThanHours) {
                var hours = Math.Floor(ts.TotalHours);
                stringBuilder.AppendFormat("{0} {1}", hours, Pluralize((int)hours, "hour", "hours"));
                return;
            }

            if(ts.Days >= 1) {
                stringBuilder.AppendFormat("{0} {1}, ", ts.Days, Pluralize(ts.Days, "day", "days"));
            }

            if(includeSeconds) {
                stringBuilder.AppendFormat("{0}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds);
            } else {
                stringBuilder.AppendFormat("{0}:{1:00}", ts.Hours, ts.Minutes);
            }
        }
    }

    [System.Diagnostics.Contracts.Pure]
    public static string Implode(this IEnumerable<string?>? array, string delimiter) {
        if(null == array) {
            return string.Empty;
        }

        return Implode(delimiter, array);
    }

    [System.Diagnostics.Contracts.Pure]
    public static string Implode(this IEnumerable<KeyValuePair<string, string?>>? pairs, string delimiter) {
        if(null == pairs) {
            return string.Empty;
        }

        return Implode(delimiter, pairs.Select(p => p.Key + "=" + p.Value));
    }

    [System.Diagnostics.Contracts.Pure]
    public static string ImplodeNewLine(this IEnumerable<object?>? array) {
        return array?.Select(o => o?.ToString()).ImplodeNewLine() ?? string.Empty;
    }

    /// <summary>
    /// for debug purposes only
    /// </summary>
    /// <param name="array"></param>
    /// <returns></returns>
    [System.Diagnostics.Contracts.Pure]
    public static string ImplodeComma(this IEnumerable<string?>? array) {
        return null == array
            ? string.Empty
            : Implode(", ", array);
    }

    [DebuggerStepThrough, System.Diagnostics.Contracts.Pure]
    public static bool EqualsIgnoreCase(this string source, string? str) {
        if(source == null) {
            throw new ArgumentNullException(nameof(source));
        }

        return string.Equals(source, str, StringComparison.OrdinalIgnoreCase);
    }

    extension(string? str) {
        public string ToHtml(bool whitespacesFormatted = false) {
            if(string.IsNullOrEmpty(str)) {
                return string.Empty;
            }

            var stringBuilder = new StringBuilder(str).Replace("<", "&lt;").Replace(">", "&gt;");
            return whitespacesFormatted ? "<pre>" + stringBuilder.ToString() + "</pre>" : stringBuilder.Replace("\r\n", "\n").Replace("\n", "<BR/>\n").ToString();
        }

        public string ToHtmlAttribute() {
            if(string.IsNullOrEmpty(str)) {
                return string.Empty;
            }

            var stringBuilder = new StringBuilder(str)
                .Replace("&", "&amp;")
                .Replace("'", "&#39;")
                .Replace("\"", "&quot;");

            return stringBuilder.ToString();
        }

        public string Mask(int skipStart, char maskSymbol = '*', bool maskLength = true) {
            if(null == str) {
                return string.Empty;
            }

            if(0 == skipStart) {
                return str;
            }

            if(str.Length <= skipStart) {
                return str;
            }

            return str[..skipStart] + (maskLength ? "***" : new string(maskSymbol, str.Length - skipStart));
        }

        public bool IsNotEmpty() => !string.IsNullOrEmpty(str);
        public string? NullIfEmpty() => string.IsNullOrWhiteSpace(str) ? null : str;
    }
}