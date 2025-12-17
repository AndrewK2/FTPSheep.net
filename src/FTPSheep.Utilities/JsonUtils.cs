using System.Text.Json;
using System.Text.Json.Serialization;
using FTPSheep.Utilities.Exceptions;
using JetBrains.Annotations;

namespace FTPSheep.Utilities;

[PublicAPI]
public static class JsonUtils {
    [Pure]
    [ContractAnnotation("source:notnull=>notnull;source:null=>null")]
    public static string? BeautifyJson(string? source) {
        if(string.IsNullOrWhiteSpace(source)) {
            return source;
        }

        try {
            using var doc = JsonDocument.Parse(source);
            return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
        } catch {
            return source;
        }
    }

    [Pure]
    [ContractAnnotation("source:notnull=>notnull;source:null=>null")]
    public static string? SerializeObject(object? source, bool indented = true, bool ignoreNulls = false) {
        if(source == null) return null;

        var options = new JsonSerializerOptions {
            WriteIndented = indented
        };

        if(ignoreNulls) {
            options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        }

        return JsonSerializer.Serialize(source, options);
    }

    [Pure]
    public static T DeserializeExpectedObject<T>(string json, JsonSerializerOptions? options = null) {
        if(string.IsNullOrWhiteSpace(json)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(json));

        var o = DeserializeObject<T?>(json, options);

        if(o is null) {
            throw new Exception("Deserialized object is null")
                .Add("JSON", json.Truncate(1024));
        }

        return o;
    }

    [Pure]
    public static T? DeserializeObject<T>(string json, JsonSerializerOptions? options = null) {
        if(json == null) throw new ArgumentNullException(nameof(json));

        try {
            return JsonSerializer.Deserialize<T>(json, options);
        } catch(Exception ex) {
            throw "Failed to deserialize JSON into target type \"{0}\""
                .F(typeof(T).Name)
                .ToException(ex)
                .Add("Target Type", typeof(T))
                .Add("JSON length", json.Length)
                .Add("JSON", json.Truncate(1024));
        }
    }
}