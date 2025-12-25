using Google.Protobuf.WellKnownTypes;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Monq.Core.BasicDotNetMicroservice.Extensions;

/// <summary>
/// Extension methods for converting Grpc Struct to System.Text.Json and back.
/// </summary>
public static class JsonGrpc
{
    /// <summary>
    /// Convert Grpc <see cref="Struct"/> to <see cref="JsonObject"/>.
    /// </summary>
    /// <param name="struct">The Grpc <see cref="Struct"/> object.</param>
    /// <returns></returns>
    public static JsonObject ToJsonObject(this Struct @struct)
    {
        var obj = new JsonObject();
        foreach (var item in @struct.Fields)
        {
            obj.Add(item.Key, GetJsonValue(item.Value));
        }
        return obj;
    }

    /// <summary>
    /// Convert Grpc <see cref="Value"/> to <see cref="JsonNode"/>.
    /// </summary>
    /// <param name="value">The Grpc <see cref="Value"/> object.</param>
    /// <returns>JsonNode or null.</returns>
    public static JsonNode? ToJsonNode(this Value value) => GetJsonValue(value);

    /// <summary>
    /// Convert Grpc <see cref="ListValue"/> to <see cref="JsonArray"/>.
    /// </summary>
    /// <param name="listValue">Grpc <see cref="ListValue"/> object.</param>
    /// <returns></returns>
    public static JsonArray ToJsonArray(this ListValue listValue)
    {
        var array = new JsonArray();
        foreach (var item in listValue.Values)
        {
            array.Add(GetJsonValue(item));
        }
        return array;
    }

    /// <summary>
    /// Convert <see cref="JsonObject"/> to Grpc <see cref="Struct"/>.
    /// </summary>
    /// <param name="jsonObject">Json <see cref="JsonObject"/> object.</param>
    /// <returns></returns>
    public static Struct ToProtoStruct(this JsonObject jsonObject)
    {
        var @struct = new Struct();
        foreach (var item in jsonObject)
        {
            @struct.Fields.Add(item.Key, GetProtoValue(item.Value));
        }
        return @struct;
    }

    /// <summary>
    /// Convert <see cref="JsonArray"/> to Grpc <see cref="ListValue"/>.
    /// </summary>
    /// <param name="jsonArray">Json <see cref="JsonArray"/> array.</param>
    /// <returns></returns>
    public static ListValue ToProtoArray(this JsonArray jsonArray)
    {
        var listValue = new ListValue();
        foreach (var item in jsonArray)
        {
            listValue.Values.Add(GetProtoValue(item));
        }
        return listValue;
    }

    /// <summary>
    /// Convert <see cref="JsonNode"/> to Grpc <see cref="Value"/>.
    /// </summary>
    /// <param name="jsonNode">Json <see cref="JsonNode"/>.</param>
    /// <returns></returns>
    public static Value ToProtoValue(this JsonNode jsonNode) =>
        GetProtoValue(jsonNode);

    static JsonNode? GetJsonValue(Value value)
    {
        switch (value.KindCase)
        {
            // Такой интересный хак, из-за https://github.com/dotnet/runtime/issues/64472 пункт 2.
            case Value.KindOneofCase.BoolValue: return JsonValue.Parse(JsonValue.Create(value.BoolValue).ToJsonString());
            case Value.KindOneofCase.NullValue: return null;
            case Value.KindOneofCase.StructValue: return ToJsonObject(value.StructValue);
            case Value.KindOneofCase.NumberValue: return JsonValue.Parse(JsonValue.Create(value.NumberValue).ToJsonString());
            case Value.KindOneofCase.StringValue: return JsonValue.Parse(JsonValue.Create(value.StringValue).ToJsonString());
            case Value.KindOneofCase.ListValue: return ToJsonArray(value.ListValue);
            default: return null;
        }
    }

    static Value GetProtoValue(JsonNode? value)
    {
        if (value == null)
            return new Value { NullValue = NullValue.NullValue };
        return value switch
        {
            JsonObject jO => GetProtoValue(jO),
            JsonArray jA => GetProtoValue(jA),
            JsonValue jV => GetProtoValue(jV),
            _ => new Value()
        };
    }

    static Value GetProtoValue(this JsonObject jsonObject)
    {
        var @struct = new Struct();
        foreach (var item in jsonObject)
        {
            @struct.Fields.Add(item.Key, GetProtoValue(item.Value));
        }
        return new Value { StructValue = @struct };
    }

    static Value GetProtoValue(this JsonArray jsonArray)
    {
        var listValue = new ListValue();
        foreach (var item in jsonArray)
        {
            listValue.Values.Add(GetProtoValue(item));
        }
        return new Value { ListValue = listValue };
    }

    static Value GetProtoValue(this JsonValue jsonValue)
    {
        switch (jsonValue.GetValue<JsonElement>().ValueKind)
        {
            case JsonValueKind.Null: return new Value { NullValue = NullValue.NullValue };
            case JsonValueKind.String: return new Value { StringValue = jsonValue.GetValue<string>() };
            case JsonValueKind.Number: return new Value { NumberValue = jsonValue.GetValue<double>() };
            case JsonValueKind.True: return new Value { BoolValue = jsonValue.GetValue<bool>() };
            case JsonValueKind.False: return new Value { BoolValue = jsonValue.GetValue<bool>() };
            default:
                return new Value { StringValue = jsonValue.ToString() };
        }
    }
}
