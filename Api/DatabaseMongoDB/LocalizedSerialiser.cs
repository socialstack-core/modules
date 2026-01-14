using Api.Translate;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using System;

namespace Api.Database;


/// <summary>
/// A serialiser for the Localized type.
/// </summary>
/// <typeparam name="T"></typeparam>
public class LocalizedSerializer<T> : IBsonSerializer<Localized<T>>
{
    /// <summary>
    /// Underlying type that this serialises.
    /// </summary>
    public Type ValueType => typeof(Localized<T>);

    /// <summary>
    /// Serialises the type to BSON.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="args"></param>
    /// <param name="value"></param>
    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Localized<T> value)
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
    public Localized<T> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var reader = context.Reader;
        var localised = new Localized<T>();

        reader.ReadStartDocument();
        while (reader.ReadBsonType() != BsonType.EndOfDocument)
        {
            var localeCode = reader.ReadName();
            var val = BsonSerializer.Deserialize<T>(reader);
            localised.SetInternalUseOnly(localeCode, val);
        }
        reader.ReadEndDocument();

        return localised;
    }

    object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
        Deserialize(context, args);

    void IBsonSerializer.Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value) =>
        Serialize(context, args, (Localized<T>)value);
}