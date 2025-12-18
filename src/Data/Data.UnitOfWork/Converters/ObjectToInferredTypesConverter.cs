using System.Text.Json;
using System.Text.Json.Serialization;

namespace EtherGizmos.Common.Converters;

public class ObjectToInferredTypesConverter : JsonConverter<object>
{
    public override object? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options) => reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Number when reader.TryGetInt64(out long l) => l,
            JsonTokenType.Number => reader.GetDouble(),
            JsonTokenType.String when reader.TryGetDateTimeOffset(out DateTimeOffset datetime) => datetime,
            JsonTokenType.String => reader.GetString()!,
            JsonTokenType.StartObject => ReadObject(ref reader, options),
            JsonTokenType.StartArray => ReadArray(ref reader, options),
            _ => JsonDocument.ParseValue(ref reader).RootElement.Clone()
        };

    private object ReadObject(
        ref Utf8JsonReader reader,
        JsonSerializerOptions options)
    {
        var dictionary = new Dictionary<string, object?>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            var propertyName = reader.GetString()!;
            reader.Read();
            dictionary[propertyName] = Read(ref reader, typeof(object), options);
        }

        return dictionary;
    }

    private object ReadArray(
        ref Utf8JsonReader reader,
        JsonSerializerOptions options)
    {
        var list = new List<object?>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            list.Add(Read(ref reader, typeof(object), options));
        }

        return list;
    }

    public override void Write(
        Utf8JsonWriter writer,
        object objectToWrite,
        JsonSerializerOptions options)
    {
        var runtimeType = objectToWrite.GetType();
        if (runtimeType == typeof(object))
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
            return;
        }

        JsonSerializer.Serialize(writer, objectToWrite, runtimeType, options);
    }
}
