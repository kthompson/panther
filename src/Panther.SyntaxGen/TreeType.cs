#nullable disable
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Panther.SyntaxGen;

public class TreeType
{
    [XmlAttribute]
    public string Name;

    [XmlAttribute]
    public string Base;

    [XmlAttribute]
    public string SkipConvenienceFactories;

    [XmlElement(ElementName = "Field", Type = typeof(Field))]
    public List<TreeTypeChild> Children = new List<TreeTypeChild>();
}