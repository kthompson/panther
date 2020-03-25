using System;
using System.Collections.Generic;
using System.Linq;

namespace Panther.CodeAnalysis.Syntax
{
    public static class SyntaxFacts
    {
        //public static int GetUnaryOperatorPrecedence(this SyntaxKind kind)
        //{
        //    switch (kind)
        //    {
        //        case SyntaxKind.PlusToken:
        //        case SyntaxKind.MinusToken:
        //        case SyntaxKind.BangToken:
        //            return 6;

        //        default:
        //            return 0;
        //    }
        //}

        public static OperatorPrecedence? GetBinaryOperatorPrecedence(this SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.StarToken:
                case SyntaxKind.SlashToken:
                    return (OperatorPrecedence)5;

                case SyntaxKind.PlusToken:
                case SyntaxKind.MinusToken:
                    return (OperatorPrecedence)4;

                case SyntaxKind.EqualsEqualsToken:
                case SyntaxKind.BangEqualsToken:
                    return (OperatorPrecedence)3;

                case SyntaxKind.AmpersandAmpersandToken:
                    return (OperatorPrecedence)2;

                case SyntaxKind.PipePipeToken:
                    return (OperatorPrecedence)1;

                default:
                    return null;
            }
        }

        public static SyntaxKind GetKeywordKind(string span)
        {
            return span switch
            {
                "true" => SyntaxKind.TrueKeyword,
                "false" => SyntaxKind.FalseKeyword,
                "val" => SyntaxKind.ValKeyword,
                "var" => SyntaxKind.VarKeyword,
                _ => SyntaxKind.IdentifierToken
            };
        }

        public static string? GetText(SyntaxKind kind) =>
            kind switch
            {
                SyntaxKind.TrueKeyword => "true",
                SyntaxKind.FalseKeyword => "false",
                SyntaxKind.ValKeyword => "val",
                SyntaxKind.PlusToken => "+",
                SyntaxKind.MinusToken => "-",
                SyntaxKind.SlashToken => "/",
                SyntaxKind.StarToken => "*",
                SyntaxKind.BangToken => "!",
                SyntaxKind.AmpersandAmpersandToken => "&&",
                SyntaxKind.PipePipeToken => "||",
                SyntaxKind.BangEqualsToken => "!=",
                SyntaxKind.EqualsToken => "=",
                SyntaxKind.EqualsEqualsToken => "==",
                SyntaxKind.CloseParenToken => ")",
                SyntaxKind.OpenParenToken => "(",
                SyntaxKind.CloseBraceToken => "}",
                SyntaxKind.OpenBraceToken => "{",
                _ => null
            };

        public static IEnumerable<SyntaxKind> GetUnaryOperatorKinds() => new[]
        {
            SyntaxKind.PlusToken,
            SyntaxKind.MinusToken,
            SyntaxKind.BangToken,
        };

        public static IEnumerable<SyntaxKind> GetBinaryOperatorKinds() =>
            Enum.GetValues(typeof(SyntaxKind)).Cast<SyntaxKind>()
                .Where(kind => GetBinaryOperatorPrecedence(kind) > 0);
    }
}