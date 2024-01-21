using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectEarthLauncher.FileTypes
{
    public class YamlFile : IFile
    {
        public Dictionary<string, Dictionary<object, object>> Obj;

        public YamlFile(string _path) : base(_path)
        {
            YamlDotNet.Serialization.Deserializer deserializer = new YamlDotNet.Serialization.Deserializer();
            StreamReader reader = new StreamReader(Path);
            Dictionary<object, object> _obj = deserializer.Deserialize<Dictionary<object, object>>(reader);
            KeyValuePair<object, object>[] keyAndValues = _obj.ToArray();

            Obj = new Dictionary<string, Dictionary<object, object>>();
            for (int i = 0; i < keyAndValues.Length; i++)
            {

                Obj.Add((string)keyAndValues[i].Key, keyAndValues[i].Value as Dictionary<object, object>);//JObject.FromObject(keyAndValues[i].Value).ToObject<Dictionary<object, object>>());
            }
            reader.Close();
        }

        public override void Save()
        {
            StreamWriter writer = new StreamWriter(Path);
            YamlDotNet.Serialization.Serializer serializer = new YamlDotNet.Serialization.Serializer();
            serializer.Serialize(writer, Obj);
            writer.Flush();
            writer.Close();
        }
    }
}
