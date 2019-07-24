using OneOf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;

namespace OneOf.Tests
{
    public class Serialization
    {
        [Test]
        public void CanSerializeOneOfValueTransparently()
        {
            //Given an object with a OneOf property 
            var x = new SomeThing()
            {
                Value = "A string value"
            };
            //When that object is serialized
            var json = JsonConvert.SerializeObject(x, new JsonSerializerSettings()
            {
                Converters = {new OneOfJsonConverter()}
            });

            //Then the OneOfs underlying value should have been written
            Assert.AreEqual("{\"Value\":\"A string value\"}", json);
        }

        [Test]
        public void CanDeserializeOneOfInherited()
        {
            //Given an object with a OneOf property 
            var x = new SomeThing()
            {
                Value = "A string value"
            };
            var serializerSettings = new JsonSerializerSettings()
            {
                Converters = {new OneOfJsonConverterWithDeserialization()}
            };
            
            //When that object is serialized
            var json = JsonConvert.SerializeObject(x, serializerSettings);

            var deserializedX = JsonConvert.DeserializeObject<SomeThing>(json, serializerSettings);

            Assert.AreEqual(x.Value, deserializedX.Value);

        }
        
        [Test]
        public void CanDeserializeOneOfValue()
        {
            //Given an object with a OneOf property 
            var x = (OneOf<Class1, Class2, string>) (new Class2() {Name = "Savvas"});
            var serializerSettings = new JsonSerializerSettings()
            {
                Converters = {new OneOfJsonConverterWithDeserialization()}
            };
            
            //When that object is serialized
            var json = JsonConvert.SerializeObject(x, serializerSettings);

            var deserializedX = JsonConvert.DeserializeObject<OneOf<Class1, Class2, string>>(json, serializerSettings);

            Assert.AreEqual(typeof(OneOf<Class1, Class2, string>), deserializedX.GetType());
            Assert.AreEqual(x.GetType(), deserializedX.GetType());
            Assert.AreEqual(((Class2)x.Value).Name, ((Class2)deserializedX.Value).Name);
        }
    }

    class Class1
    {
        public string Name { get; set; }
    }

    class Class2 : IEquatable<Class2>
    {
        public string Name { get; set; }

        public bool Equals(Class2 other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Name, other.Name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Class2) obj);
        }

        public override int GetHashCode()
        {
            return (Name != null ? Name.GetHashCode() : 0);
        }
    }
    
    class SomeThing
    {
        public OneOf<string, SomeOtherThing> Value { get; set; }
    }

    internal class SomeOtherThing
    {
        public int Value { get; set; }
    }
}
