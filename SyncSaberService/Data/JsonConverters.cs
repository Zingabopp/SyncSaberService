using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace SyncSaberService.Data
{
    // From: https://stackoverflow.com/a/55768479
    public class IntegerWithCommasConverter : JsonConverter<int>
    {
        public override int ReadJson(JsonReader reader, Type objectType, int existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                throw new JsonSerializationException("Cannot unmarshal int");
            }
            if (reader.TokenType == JsonToken.Integer)
                return Convert.ToInt32(reader.Value);
            var value = (string) reader.Value;
            const NumberStyles style = NumberStyles.AllowThousands;
            var result = int.Parse(value, style, CultureInfo.InvariantCulture);
            return result;
        }

        public override void WriteJson(JsonWriter writer, int value, JsonSerializer serializer)
        {
            writer.WriteValue(value);
        }
    }

    public class EmptyArrayOrDictionaryConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsAssignableFrom(typeof(Dictionary<string, object>));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JToken token = JToken.Load(reader);
            if (token.Type == JTokenType.Object)
            {
                return token.ToObject(objectType);
            }
            else if (token.Type == JTokenType.Array)
            {
                if (!token.HasValues)
                {
                    // create empty dictionary
                    return Activator.CreateInstance(objectType);
                }
                // Handles case where Beat Saver gives the slashstat in the form of an array.
                if (objectType == typeof(Dictionary<string, int>))
                {
                    var retDict = new Dictionary<string, int>();
                    for (int i = 0; i < token.Count(); i++)
                    {
                        retDict.Add(i.ToString(), (int) token.ElementAt(i));
                    }
                    return retDict;
                }
            }
            //throw new JsonSerializationException($"{objectType.ToString()} or empty array expected, received a {token.Type.ToString()}");
            Logger.Warning($"{objectType.ToString()} or empty array expected, received a {token.Type.ToString()}");
            return Activator.CreateInstance(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}
