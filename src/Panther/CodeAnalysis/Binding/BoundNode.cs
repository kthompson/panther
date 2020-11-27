using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    internal abstract record BoundNode(SyntaxNode Syntax)
    {
        public abstract BoundNodeKind Kind { get; }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }
    }
}