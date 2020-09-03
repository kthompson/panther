﻿using System.Collections.Generic;

namespace Panther.CodeAnalysis.Syntax
{
    public abstract class MemberSyntax : SyntaxNode
    {
        protected MemberSyntax(SyntaxTree syntaxTree) : base(syntaxTree)
        {
        }
    }
}