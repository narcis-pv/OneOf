using OneOf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;
using NUnit.Framework;
using OneOf.Types;

namespace OneOf.Tests
{
    public class Serialization
    {
        [Test]
        public void CanDeserializeOneOfInherited()
        {
            //Given an object with a OneOf property 
            var x = new SomeThing()
            {
                Prop = "A string value"
            };
            var serializerSettings = new JsonSerializerSettings()
            {
                Converters = {new OneOfJsonConverter()}
            };
            
            //When that object is serialized
            var json = JsonConvert.SerializeObject(x, serializerSettings);

            var deserializedX = JsonConvert.DeserializeObject<SomeThing>(json, serializerSettings);

            Assert.AreEqual(x.Prop, deserializedX.Prop);

        }

        [Test]
        public void CanDeserializeOneOfDefaultProperty()
        {
            //Given an object with a OneOf property 
            var x = new SomeThingNone();

            var serializerSettings = new JsonSerializerSettings
            {
                Converters = { new OneOfJsonConverter() }
            };

            //When that object is serialized
            var json = JsonConvert.SerializeObject(x, serializerSettings);

            var deserializedX = JsonConvert.DeserializeObject<SomeThingNone>(json, serializerSettings);

            Assert.AreEqual(x.Prop, deserializedX.Prop);
        }


        [Test]
        public void ThrowSerializationExceptionWhenOneOfClassesHasNullValue()
        {
            var x = new ClassWithOneOfClasses();

            var serializerSettings = new JsonSerializerSettings { Converters = { new OneOfJsonConverter() } };

            Assert.Throws<JsonSerializationException>(() => JsonConvert.SerializeObject(x, serializerSettings));
        }

        [Test]
        [TestCase("{}")]
        [TestCase("{ \"Type\": \"\" }")]
        [TestCase("{ \"Value\": \"Test\" }")]
        [TestCase("{ \"Type\": null, \"Value\": null }")]
        public void ThrowSerializationExceptionOnInvalidJsonFormat(string propJson)
        {
            var serializerSettings = new JsonSerializerSettings { Converters = { new OneOfJsonConverter() } };

            Assert.Throws<JsonSerializationException>(() => 
                JsonConvert.DeserializeObject<SomeThing>($"{{ \"Prop\": {propJson} }}", serializerSettings));
        }
          
        [Test]
        public void CanDeserializeOneOfValue()
        {
            //Given an object with a OneOf property 
            var x = (OneOf<Class1, Class2, string>) (new Class2() {Name = "Savvas"});
            var serializerSettings = new JsonSerializerSettings()
            {
                Converters = {new OneOfJsonConverter()}
            };
            
            //When that object is serialized
            var json = JsonConvert.SerializeObject(x, serializerSettings);

            var deserializedX = JsonConvert.DeserializeObject<OneOf<Class1, Class2, string>>(json, serializerSettings);

            Assert.AreEqual(typeof(OneOf<Class1, Class2, string>), deserializedX.GetType());
            Assert.AreEqual(x.GetType(), deserializedX.GetType());
            Assert.AreEqual(((Class2)x.Value).Name, ((Class2)deserializedX.Value).Name);
        }

        [Test]
        public void CanWriteAndReadOneOfFromMongoDb()
        {
            var client = new MongoClient(new MongoClientSettings { Server = new MongoServerAddress("localhost", 27017) });
            var database = client.GetDatabase("test");
            
            BsonSerializer.RegisterSerializationProvider(new OneOfBsonSerializationProvider());
            var mongoCollection = database.GetCollection<SomeThing>(nameof(SomeThing));

            var prop = Guid.NewGuid().ToString();

            var expected = new SomeThing { Prop = prop};

            mongoCollection.InsertOne(expected);

            var builder = new FilterDefinitionBuilder<SomeThing>();

            var actual = mongoCollection.FindSync(builder.Eq(thing => thing.Prop, prop)).Single();

            Assert.AreEqual(expected.Prop.Value, actual.Prop.Value);
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

    [BsonIgnoreExtraElements]
    class SomeThing
    {
        public OneOf<string, SomeOtherThing> Prop { get; set; }
    }

    class SomeThingNone
    {
        public OneOf<None,SomeOtherThing> Prop { get; set; }
    }

    class ClassWithOneOfClasses
    {
        public OneOf<SomeThing, SomeOtherThing> Prop { get; set; }
    }

    internal class SomeOtherThing
    {
        public int Prop { get; set; }
    }
}
