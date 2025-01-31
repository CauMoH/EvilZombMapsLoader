using System.IO;
using System.Xml.Serialization;

namespace EvilZombMapsLoader.Xml
{
    public static class XmlParser
    {
        public static bool Parse<T>(string filePath, out T config)
        {
            config = default;

            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return false;

            using (var reader = new StreamReader(filePath))
            {
                var serializer = new XmlSerializer(typeof(T));
                config = (T)serializer.Deserialize(reader);
            }

            return true;
        }

        public static bool Save<T>(string filePath, T config)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            using (var writer = new StreamWriter(filePath))
            {
                var serializer = new XmlSerializer(typeof(T));
                serializer.Serialize(writer, config);
            }

            return true;
        }
    }
}
