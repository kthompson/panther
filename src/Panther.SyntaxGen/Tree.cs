#nullable disable
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace Panther.SyntaxGen;

[XmlRoot]
public class Tree
{
    [XmlAttribute]
    public string Root;

    [XmlElement(ElementName = "Node", Type = typeof(Node))]
    [XmlElement(ElementName = "AbstractNode", Type = typeof(AbstractNode))]
    [XmlElement(ElementName = "PredefinedNode", Type = typeof(PredefinedNode))]
    public List<TreeType> Types;

    public IEnumerable<TreeType> ConcreteNodes =>
        Types.Where(type => type is not AbstractNode).OrderBy(node => node.Name);
}