#nullable disable
using System.Collections.Generic;

namespace Panther.SyntaxGen;

public class AbstractNode : TreeType
{
    public readonly List<Field> Fields = new List<Field>();
}