using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Dynamitey;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OneOf
{
    public class OneOfJsonConverterWithDeserialization : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is IOneOf)
            {
                value = ((IOneOf) value).Value;
            }
            serializer.Serialize(writer, new
            {
                Value = value,
                Type = value.GetType().FullName
            });
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            dynamic valueAndType = JObject.Load(reader);

            var typeName = (string)valueAndType.Type;
            var type = Type.GetType(typeName);

            var value = ((JToken) valueAndType.Value).ToObject(type);

            var method = objectType.GetMethod("op_Implicit", 
                BindingFlags.Public | BindingFlags.Static, 
                null,
                new Type[] {type},
                new ParameterModifier[0]);

            return method.Invoke(null, new object[] {value});
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IOneOf));
        }       
    }
    
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
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IOneOf));
        }
    }
}
