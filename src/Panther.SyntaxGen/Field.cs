#nullable disable
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Panther.SyntaxGen;

public class Field : TreeTypeChild
{
    [XmlAttribute]
    public string Name;

    [XmlAttribute]
    public string Type;

    [XmlAttribute]
    public string Optional;

    [XmlAttribute]
    public string Override;

    [XmlAttribute]
    public string New;

    [XmlAttribute]
    public int MinCount;

    [XmlAttribute]
    public bool AllowTrailingSeparator;

    [XmlElement(ElementName = "Kind", Type = typeof(Kind))]
    public List<Kind> Kinds = new List<Kind>();

    public bool IsToken => Type == "SyntaxToken";
    public bool IsOptional => Optional == "true";
}