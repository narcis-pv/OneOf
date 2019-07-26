using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonConvert = Newtonsoft.Json.JsonConvert;
using JsonReader = Newtonsoft.Json.JsonReader;
using JsonWriter = Newtonsoft.Json.JsonWriter;

namespace OneOf
{
    /// <summary>
    /// Cache of implicit cast operators for <see cref="IOneOf"/>
    /// </summary>
    internal static class OneOfImplicitCast
    {
        private static readonly ConcurrentDictionary<(string OneOfType, string TargetType), MethodInfo> ImplicitCastOperatorCache;

        static OneOfImplicitCast()
        {
            ImplicitCastOperatorCache = new ConcurrentDictionary<(string, string), MethodInfo>();
        }

        public static MethodInfo GetImplicitCast(Type targetType, Type sourceType)
        {
            return ImplicitCastOperatorCache.GetOrAdd(
                (targetType.AssemblyQualifiedName, sourceType.AssemblyQualifiedName),
                key =>
                    targetType.GetMethod("op_Implicit",
                        BindingFlags.Public | BindingFlags.Static,
                        null,
                        new[] {sourceType},
                        new ParameterModifier[0]));
        }
    }

    public class OneOfJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var oneOfValue = ((IOneOf) value).Value;

            if (oneOfValue == null)
                throw new JsonSerializationException($"Failed to serialize path '{writer.Path}' because the {value.GetType().Name}'.{nameof(IOneOf.Value)} has unsupported null value.");

            serializer.Serialize(writer, new OneOfContainer
            {
                Value = oneOfValue,
                Type = oneOfValue.GetType().AssemblyQualifiedName
            });
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jtoken = JToken.Load(reader);

            if (!jtoken.HasValues)
                throw new JsonSerializationException($"Failed to deserialize type '{objectType.FullName}' because of invalid json format: '{jtoken}' was not loaded.");

            var typeName = jtoken.Value<string>(nameof(OneOfContainer.Type));
            if (string.IsNullOrWhiteSpace(typeName))
                throw new JsonSerializationException($"Failed to deserialize type '{objectType.FullName}' because property '{nameof(OneOfContainer.Type)}' was empty.");

            var type = Type.GetType(typeName);

            if (type == null)
                throw new JsonSerializationException($"Failed to deserialize type '{objectType.FullName}' because type '{typeName}' was not loaded.");

            var value = jtoken.Value<JToken>(nameof(OneOfContainer.Value)).ToObject(type);

            return OneOfImplicitCast.GetImplicitCast(objectType, type).Invoke(null, new[] { value });
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IOneOf));
        }
    }

    public class OneOfContainer
    {
        public object Value { get; set; }
        public string Type { get; set; }
    }

    public class OneOfBsonSerializer : SerializerBase<IOneOf>
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            Converters = { new OneOfJsonConverter() }
        };

        /// <summary>
        /// The actual type that it's being (de)serialized, and not the nominal type, which in our case is an <see cref="IOneOf"/>
        /// This should be set by the 
        /// </summary>
        public Type ActualType { get; }

        public OneOfBsonSerializer(Type actualType)
        {
            ActualType = actualType ?? throw new ArgumentNullException(nameof(actualType));
        }

        // public methods
        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <param name="args">The deserialization args.</param>
        /// <returns>A deserialized value.</returns>
        public override IOneOf Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var serializer = BsonSerializer.LookupSerializer(typeof(BsonDocument));

            var oneOfContainer = (BsonDocument)serializer.Deserialize(context, args);

            var json = oneOfContainer.ToJson();

            return (IOneOf)JsonConvert.DeserializeObject(json, ActualType, JsonSerializerSettings);
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="args">The serialization args.</param>
        /// <param name="value">The object.</param>
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, IOneOf value)
        {
            var bsonDocument = BsonDocument.Parse(JsonConvert.SerializeObject(value, ActualType, JsonSerializerSettings));

            context.Writer.WriteRawBsonDocument(new RawBsonDocument(bsonDocument.ToBson()).Slice);
        }
    }

    public class OneOfBsonSerializationProvider : IBsonSerializationProvider
    {
        public IBsonSerializer GetSerializer(Type type)
        {
            return type.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IOneOf)) ? new OneOfBsonSerializer(type) : null;
        }
    }
}