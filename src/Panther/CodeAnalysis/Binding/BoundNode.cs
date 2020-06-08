using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    internal abstract class BoundNode
    {
        public SyntaxNode Syntax { get; }
        public abstract BoundNodeKind Kind { get; }

        public BoundNode(SyntaxNode syntax)
        {
            Syntax = syntax;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }
    }
}