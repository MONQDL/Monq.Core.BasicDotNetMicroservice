using Google.Protobuf.WellKnownTypes;
using Monq.Core.BasicDotNetMicroservice.Extensions;
using System.Text.Json.Nodes;
using Xunit;

namespace Monq.Core.BasicDotNetMicroservice.Tests;

public class JsonGrpcExtensionsTests
{
    [Fact]
    public void ToJsonObject_ConvertsAllTypes()
    {
        var nestedStruct = new Struct();
        nestedStruct.Fields.Add("city", new Value { StringValue = "Moscow" });
        nestedStruct.Fields.Add("zip", new Value { NumberValue = 123456 });

        var nestedList = new ListValue();
        nestedList.Values.Add(new Value { StringValue = "a" });
        nestedList.Values.Add(new Value { NumberValue = 42 });

        var grpcStruct = new Struct();
        grpcStruct.Fields.Add("name", new Value { StringValue = "John" });
        grpcStruct.Fields.Add("age", new Value { NumberValue = 30 });
        grpcStruct.Fields.Add("active", new Value { BoolValue = true });
        grpcStruct.Fields.Add("data", new Value { NullValue = NullValue.NullValue });
        grpcStruct.Fields.Add("address", new Value { StructValue = nestedStruct });
        grpcStruct.Fields.Add("items", new Value { ListValue = nestedList });

        var result = grpcStruct.ToJsonObject();

        Assert.Equal(6, result.Count);
        Assert.Equal("John", result["name"]?.GetValue<string>());
        Assert.Equal(30.0, result["age"]?.GetValue<double>());
        Assert.True(result["active"]?.GetValue<bool>());
        Assert.Null(result["data"]);

        var addressObj = result["address"]?.AsObject();
        Assert.NotNull(addressObj);
        Assert.Equal("Moscow", addressObj["city"]?.GetValue<string>());
        Assert.Equal(123456.0, addressObj["zip"]?.GetValue<double>());

        var itemsArr = result["items"]?.AsArray();
        Assert.NotNull(itemsArr);
        Assert.Equal(2, itemsArr.Count);
    }

    [Fact]
    public void ToJsonNode_ConvertsEachKind()
    {
        var stringVal = new Value { StringValue = "hello" };
        Assert.Equal("hello", stringVal.ToJsonNode()?.GetValue<string>());

        var numVal = new Value { NumberValue = 3.14 };
        Assert.Equal(3.14, numVal.ToJsonNode()?.GetValue<double>());

        var boolVal = new Value { BoolValue = true };
        Assert.True(boolVal.ToJsonNode()?.GetValue<bool>());

        var nullVal = new Value { NullValue = NullValue.NullValue };
        Assert.Null(nullVal.ToJsonNode());

        var structVal = new Value { StructValue = new Struct { Fields = { ["k"] = new Value { StringValue = "v" } } } };
        Assert.Equal("v", structVal.ToJsonNode()?.AsObject()["k"]?.GetValue<string>());

        var listVal = new Value { ListValue = new ListValue { Values = { new Value { StringValue = "x" } } } };
        Assert.Equal("x", listVal.ToJsonNode()?.AsArray()[0]?.GetValue<string>());
    }

    [Fact]
    public void ToJsonArray_ConvertsMixedValues()
    {
        var nestedList = new ListValue { Values = { new Value { NumberValue = 1 } } };
        var nestedStruct = new Struct { Fields = { ["inner"] = new Value { ListValue = nestedList } } };

        var listValue = new ListValue();
        listValue.Values.Add(new Value { StringValue = "text" });
        listValue.Values.Add(new Value { NumberValue = 42 });
        listValue.Values.Add(new Value { BoolValue = false });
        listValue.Values.Add(new Value { NullValue = NullValue.NullValue });
        listValue.Values.Add(new Value { StructValue = nestedStruct });
        listValue.Values.Add(new Value { ListValue = nestedList });

        var result = listValue.ToJsonArray();

        Assert.Equal(6, result.Count);
        Assert.Equal("text", result[0]?.GetValue<string>());
        Assert.Equal(42.0, result[1]?.GetValue<double>());
        Assert.False(result[2]?.GetValue<bool>());
        Assert.Null(result[3]);
    }

    [Fact]
    public void ToProtoStruct_ConvertsJsonObject()
    {
        var nestedObj = new JsonObject { ["city"] = "Moscow", ["zip"] = 123456.0 };
        var arr = new JsonArray { "a", "b" };

        var jsonObject = new JsonObject
        {
            ["name"] = "Alice",
            ["score"] = 95.5,
            ["passed"] = true,
            ["meta"] = null,
            ["address"] = nestedObj,
            ["tags"] = arr
        };

        var result = jsonObject.ToProtoStruct();

        Assert.Equal(6, result.Fields.Count);
        Assert.Equal("Alice", result.Fields["name"].StringValue);
        Assert.Equal(95.5, result.Fields["score"].NumberValue);
        Assert.True(result.Fields["passed"].BoolValue);
        Assert.Equal(NullValue.NullValue, result.Fields["meta"].NullValue);
        Assert.Equal("Moscow", result.Fields["address"].StructValue.Fields["city"].StringValue);
        Assert.Equal("a", result.Fields["tags"].ListValue.Values[0].StringValue);
    }

    [Fact]
    public void ToProtoArray_ConvertsJsonArray()
    {
        var jsonArray = new JsonArray { "text", 42.0, true, false, null };

        var result = jsonArray.ToProtoArray();

        Assert.Equal(5, result.Values.Count);
        Assert.Equal("text", result.Values[0].StringValue);
        Assert.Equal(42, result.Values[1].NumberValue);
        Assert.True(result.Values[2].BoolValue);
        Assert.False(result.Values[3].BoolValue);
        Assert.Equal(NullValue.NullValue, result.Values[4].NullValue);
    }

    [Fact]
    public void ToProtoValue_ConvertsAllTypes()
    {
        JsonNode strNode = "hello";
        Assert.Equal("hello", strNode.ToProtoValue().StringValue);

        JsonNode numNode = 3.14;
        Assert.Equal(3.14, numNode.ToProtoValue().NumberValue);

        JsonNode trueNode = true;
        Assert.True(trueNode.ToProtoValue().BoolValue);

        JsonNode falseNode = false;
        Assert.False(falseNode.ToProtoValue().BoolValue);

        JsonNode? nullNode = null;
        Assert.Equal(NullValue.NullValue, nullNode.ToProtoValue().NullValue);

        var objNode = new JsonObject { ["key"] = "val" };
        Assert.Equal("val", objNode.ToProtoValue().StructValue.Fields["key"].StringValue);

        var arrNode = new JsonArray { "x", "y" };
        var arrVal = arrNode.ToProtoValue();
        Assert.Equal(2, arrVal.ListValue.Values.Count);
    }

    [Fact]
    public void ToJsonNode_AllowsCrossTypeConversion()
    {
        var intVal = new Value { NumberValue = 42 };
        var intNode = intVal.ToJsonNode();
        Assert.Equal(42, intNode?.GetValue<int>());
        Assert.Equal(42.0, intNode?.GetValue<double>());
        Assert.Equal(42L, intNode?.GetValue<long>());
        Assert.Equal(42.0f, intNode?.GetValue<float>());
        Assert.Equal(42.0m, intNode?.GetValue<decimal>());

        var doubleVal = new Value { NumberValue = 3.14 };
        var doubleNode = doubleVal.ToJsonNode();
        Assert.Equal(3.14, doubleNode?.GetValue<double>());
        Assert.Equal(3.14f, doubleNode?.GetValue<float>());
        Assert.Equal(3.14m, doubleNode?.GetValue<decimal>());
    }

    [Fact]
    public void ToProtoValue_AllowsCrossTypeConversion()
    {
        JsonNode intJsonNode = 100;
        Assert.Equal(100.0, intJsonNode.ToProtoValue().NumberValue);

        JsonNode doubleJsonNode = 0.001;
        Assert.Equal(0.001, doubleJsonNode.ToProtoValue().NumberValue);

        JsonNode zeroJsonNode = 0;
        Assert.Equal(0, zeroJsonNode.ToProtoValue().NumberValue);

        JsonNode guidJsonNode = Guid.Parse("d67251b2-64f8-4673-96e0-0f2ddaaa847e");
        Assert.Equal("d67251b2-64f8-4673-96e0-0f2ddaaa847e", guidJsonNode.ToProtoValue().StringValue);
    }

    [Fact]
    public void RoundTrip_PreservesAllData()
    {
        var innerStruct = new Struct();
        innerStruct.Fields.Add("city", new Value { StringValue = "Moscow" });
        innerStruct.Fields.Add("streets", new Value { ListValue = new ListValue { Values = { new Value { StringValue = "Lenina" } } } });

        var original = new Struct();
        original.Fields.Add("name", new Value { StringValue = "John" });
        original.Fields.Add("age", new Value { NumberValue = 30 });
        original.Fields.Add("active", new Value { BoolValue = true });
        original.Fields.Add("data", new Value { NullValue = NullValue.NullValue });
        original.Fields.Add("address", new Value { StructValue = innerStruct });
        original.Fields.Add("tags", new Value { ListValue = new ListValue { Values = { new Value { StringValue = "dev" } } } });

        var json = original.ToJsonObject();
        var result = json.ToProtoStruct();

        Assert.Equal("John", result.Fields["name"].StringValue);
        Assert.Equal(30, result.Fields["age"].NumberValue);
        Assert.True(result.Fields["active"].BoolValue);
        Assert.Equal(NullValue.NullValue, result.Fields["data"].NullValue);
        Assert.Equal("Moscow", result.Fields["address"].StructValue.Fields["city"].StringValue);
        Assert.Single(result.Fields["address"].StructValue.Fields["streets"].ListValue.Values);
        Assert.Equal("dev", result.Fields["tags"].ListValue.Values[0].StringValue);
    }
}
