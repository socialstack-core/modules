using Api.Translate;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;

namespace Api.Database;


/// <summary>
/// A serialiser for the MappingData type.
/// </summary>
public class MappingDataSerializer : IBsonSerializer<MappingData>
{
    /// <summary>
    /// Underlying type that this serialises.
    /// </summary>
    public Type ValueType => typeof(MappingData);

    /// <summary>
    /// Serialises the type to BSON.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="args"></param>
    /// <param name="value"></param>
    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, MappingData value)
    {
        var writer = context.Writer;
        writer.WriteStartDocument();

        var vals = value.Values;

        if (vals != null)
        {
            foreach (var kvp in vals)
            {
                writer.WriteName(kvp.Key);
                BsonSerializer.Serialize(writer, kvp.Value);
            }
        }

        writer.WriteEndDocument();
    }

    /// <summary>
    /// Deserialises the type from BSON.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public MappingData Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var reader = context.Reader;
        var map = new MappingData();

        reader.ReadStartDocument();
        while (reader.ReadBsonType() != BsonType.EndOfDocument)
        {
            var localeCode = reader.ReadName();
            var val = BsonSerializer.Deserialize<List<ulong>>(reader);
            map.Set(localeCode, val);
        }
        reader.ReadEndDocument();

        return map;
    }

    object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
        Deserialize(context, args);

    void IBsonSerializer.Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value) =>
        Serialize(context, args, (MappingData)value);
}