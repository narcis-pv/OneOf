using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Dynamitey;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OneOf
{
    public class OneOfJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is IOneOf)
            {
                value = ((IOneOf) value).Value;
            }
            serializer.Serialize(writer, value);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var readFrom = JToken.ReadFrom(reader);

            switch (reader.TokenType)
            {
                case JsonToken.Integer:
                    return Cast(readFrom.Value<int>(), objectType);
                case JsonToken.Float:
                    return Cast(readFrom.Value<decimal>(), objectType);
                case JsonToken.String:
                    return Cast(readFrom.Value<string>(), objectType);
                case JsonToken.Boolean:
                    return Cast(readFrom.Value<bool>(), objectType);
                case JsonToken.Null:
                    return null;
                case JsonToken.Undefined:
                    return null;
                case JsonToken.Date:
                    return Cast(readFrom.Value<DateTime>(), objectType);
                case JsonToken.Bytes:
                    return Cast(readFrom.Value<byte[]>(), objectType);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IOneOf));
        }

        object Cast(object obj, Type castTo)
        {
            return Dynamic.InvokeConvert(obj, castTo, true);
        }
    }

}
