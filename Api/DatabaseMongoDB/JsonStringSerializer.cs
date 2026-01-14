using Api.Database;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System;

/// <summary>
/// A serializer for the JsonString type.
/// </summary>
public class JsonStringSerializer : IBsonSerializer<JsonString>
{
    /// <summary>
    /// The type that this serializer serializes (JsonString in this case).
    /// </summary>
    public Type ValueType => typeof(JsonString);

    /// <summary>
    /// Serialize the given value.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="args"></param>
    /// <param name="value"></param>
    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, JsonString value)
{
    var val = value.ValueOf();

    if (string.IsNullOrWhiteSpace(val))
    {
        context.Writer.WriteNull();
        return;
    }

    // Parse the JSON into a BsonValue (could be document, array, etc.)
    BsonValue bsonValue;
    try
    {
        bsonValue = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonValue>(val);
    }
    catch (Exception ex)
    {
        throw new FormatException("Failed to parse JSON string into a BsonValue.", ex);
    }

    switch (bsonValue.BsonType)
    {
        case BsonType.Document:
            BsonDocumentSerializer.Instance.Serialize(context, (BsonDocument)bsonValue);
            break;

        case BsonType.Array:
            BsonArraySerializer.Instance.Serialize(context, (BsonArray)bsonValue);
            break;

        // Optionally support basic types
        case BsonType.String:
            context.Writer.WriteString(bsonValue.AsString);
            break;

        case BsonType.Int32:
            context.Writer.WriteInt32(bsonValue.AsInt32);
            break;

        case BsonType.Int64:
            context.Writer.WriteInt64(bsonValue.AsInt64);
            break;

        case BsonType.Boolean:
            context.Writer.WriteBoolean(bsonValue.AsBoolean);
            break;

        case BsonType.Double:
            context.Writer.WriteDouble(bsonValue.AsDouble);
            break;

        case BsonType.Null:
            context.Writer.WriteNull();
            break;

        default:
            throw new NotSupportedException($"Unsupported BSON type for serialization: {bsonValue.BsonType}");
    }
}

    /// <summary>
    /// Deserialize from a contextual bson document to he value.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public JsonString Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var bsonType = context.Reader.GetCurrentBsonType();

        BsonValue value;

        switch (bsonType)
        {
            case BsonType.Document:
                value = BsonDocumentSerializer.Instance.Deserialize(context);
                break;

            case BsonType.Null:
				context.Reader.ReadNull();
				return new JsonString(null);

            case BsonType.Array:
                value = BsonArraySerializer.Instance.Deserialize(context);
                break;

            case BsonType.String:
                value = BsonStringSerializer.Instance.Deserialize(context);
                break;

            default:
                throw new FormatException($"Unexpected BSON type: {bsonType}");
        }

        var json = value.ToJson(new JsonWriterSettings
        {
            OutputMode = JsonOutputMode.RelaxedExtendedJson,
            Indent = true,
            NewLineChars = "",
            IndentChars = ""
        });

        return new JsonString(json);
    }

    object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        => Deserialize(context, args);

    void IBsonSerializer.Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        => Serialize(context, args, (JsonString)value);
}