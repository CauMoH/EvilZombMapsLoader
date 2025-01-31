using System;
using System.Xml.Serialization;

namespace EvilZombMapsLoader.Xml
{
    [Serializable]
    [XmlRoot("MapItem")]
    public class MapXmlItem
    {
        [XmlElement("MapName")]
        public string MapName { get; set; }

        [XmlElement("ImageLoaded")]
        public bool ImageLoaded { get; set; }
    }
}
