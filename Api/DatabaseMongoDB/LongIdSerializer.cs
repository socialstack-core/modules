using Api.Startup;
using MongoDB.Bson.Serialization;
using System;

namespace Api.Database;


/// <summary>
/// A serializer for LongIDCollector.
/// </summary>
public class LongIDCollectorSerializer : IBsonSerializer<LongIDCollector>
{
    /// <summary>
    /// The supported type that this serializes.
    /// </summary>
    public Type ValueType => typeof(LongIDCollector);

    /// <summary>
    /// Deserializes a long collector.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public LongIDCollector Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        // Optional: implement this if you expect to read LongIDCollector from the DB.
        // For now, throw or return an empty one.
        throw new NotSupportedException("Deserialization not supported for LongIDCollector.");
    }

    /// <summary>
    /// Serializes a long collector.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="args"></param>
    /// <param name="value"></param>
	public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, LongIDCollector value)
    {
        var writer = context.Writer;

        writer.WriteStartArray();
        foreach (var id in value)
        {
            writer.WriteInt64((long)id);
        }
        writer.WriteEndArray();
    }

	object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
		=> Deserialize(context, args);

	void IBsonSerializer.Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
		=> Serialize(context, args, (LongIDCollector)value);
}