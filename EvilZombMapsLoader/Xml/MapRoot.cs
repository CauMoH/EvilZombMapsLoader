using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace EvilZombMapsLoader.Xml
{
    [Serializable]
    [XmlRoot("MapRoot")]
    public class MapRoot
    {
        [XmlArray("Maps")]
        [XmlArrayItem("MapItem", typeof(MapXmlItem))]
        public List<MapXmlItem> Maps { get; set; } = new List<MapXmlItem>();
    }
}
