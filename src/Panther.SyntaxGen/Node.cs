#nullable disable
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace Panther.SyntaxGen;

public class Node : TreeType
{
    [XmlAttribute]
    public string Root;

    [XmlAttribute]
    public string Errors;

    [XmlElement(ElementName = "Kind", Type = typeof(Kind))]
    public List<Kind> Kinds = new List<Kind>();

    public IEnumerable<Field> Fields => this.Children.OfType<Field>();
}