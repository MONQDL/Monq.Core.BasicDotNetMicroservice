using Google.Protobuf.WellKnownTypes;
using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Monq.Core.BasicDotNetMicroservice.Extensions;

/// <summary>
/// Extension methods for converting Grpc Struct to System.Text.Json and back.
/// </summary>
public static class JsonGrpcExtensions
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
    public static JsonNode? ToJsonNode(this Value value)
        => GetJsonValue(value);

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
    public static Value ToProtoValue(this JsonNode jsonNode)
        => GetProtoValue(jsonNode);

    static JsonNode? GetJsonValue(Value value)
        => value.KindCase switch
        {
            Value.KindOneofCase.NullValue => null,
            Value.KindOneofCase.NumberValue => JsonValue.Create(value.NumberValue).AsJsonElementValue(),
            Value.KindOneofCase.StringValue => JsonValue.Create(value.StringValue),
            Value.KindOneofCase.BoolValue => JsonValue.Create(value.BoolValue),
            Value.KindOneofCase.StructValue => value.StructValue.ToJsonObject(),
            Value.KindOneofCase.ListValue => value.ListValue.ToJsonArray(),
            _ => null
        };

    static Value GetProtoValue(JsonNode? value)
        => value switch
        {
            null => new Value { NullValue = NullValue.NullValue },
            JsonObject jO => GetProtoValue(jO),
            JsonArray jA => GetProtoValue(jA),
            JsonValue jV => GetProtoValue(jV),
            _ => new Value()
        };

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
        => jsonValue.GetValueKind() switch
        {
            JsonValueKind.Null => new Value { NullValue = NullValue.NullValue },
            JsonValueKind.String => new Value { StringValue = jsonValue.GetValue<string>() },
            JsonValueKind.Number => new Value { NumberValue = jsonValue.AsJsonElementValue()?.GetValue<double>() ?? 0 },
            JsonValueKind.True => new Value { BoolValue = jsonValue.GetValue<bool>() },
            JsonValueKind.False => new Value { BoolValue = jsonValue.GetValue<bool>() },
            JsonValueKind => new Value { StringValue = jsonValue.ToString() },
        };

    /// <summary>
    /// Force using <see cref="JsonElement"/> as underlying value to automatically handle type conversions in <see cref="JsonNode.GetValue{T}"/>.
    /// <para> See p.2 https://github.com/dotnet/runtime/issues/64472</para>
    /// </summary>
    static JsonValue AsJsonElementValue(this JsonValue jsonValue)
    {
        var node = JsonNode.Parse(jsonValue.ToJsonString())
            ?? throw new InvalidOperationException();
        return node.AsValue();
    }
}
