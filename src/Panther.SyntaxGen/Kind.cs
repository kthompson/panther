#nullable disable
using System.Xml.Serialization;

namespace Panther.SyntaxGen;

public class Kind
{
    [XmlAttribute]
    public string Name;

    public override bool Equals(object obj) => obj is Kind kind && Name == kind.Name;

    public override int GetHashCode() => Name.GetHashCode();
}