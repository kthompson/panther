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
        /// <summary>
        ///
        /// From lowest to highest:
        /// (all letters)
        /// |
        /// ^
        /// &
        /// = !
        /// < >
        /// :
        /// + -
        /// * / %
        /// (all other special characters)
        ///
        /// </summary>
        /// <param name="kind"></param>
        /// <returns></returns>
        public static OperatorPrecedence? GetBinaryOperatorPrecedence(this SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.PipePipeToken:
                    return (OperatorPrecedence)1;

                case SyntaxKind.AmpersandAmpersandToken:
                    return (OperatorPrecedence)2;

                case SyntaxKind.EqualsEqualsToken:
                case SyntaxKind.BangEqualsToken:
                    return (OperatorPrecedence)3;

                case SyntaxKind.LessThanToken:
                case SyntaxKind.LessThanEqualsToken:
                case SyntaxKind.GreaterThanToken:
                case SyntaxKind.GreaterThanEqualsToken:
                    return (OperatorPrecedence)4;

                case SyntaxKind.PlusToken:
                case SyntaxKind.MinusToken:
                    return (OperatorPrecedence)5;

                case SyntaxKind.StarToken:
                case SyntaxKind.SlashToken:
                    return (OperatorPrecedence)6;

                default:
                    return null;
            }
        }

        public static SyntaxKind GetKeywordKind(string span)
        {
            return span switch
            {
                "if" => SyntaxKind.IfKeyword,
                "then" => SyntaxKind.ThenKeyword,
                "else" => SyntaxKind.ElseKeyword,
                "while" => SyntaxKind.WhileKeyword,
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
                SyntaxKind.VarKeyword => "var",
                SyntaxKind.ThenKeyword => "then",
                SyntaxKind.ElseKeyword => "else",
                SyntaxKind.IfKeyword => "if",
                SyntaxKind.WhileKeyword => "while",
                SyntaxKind.PlusToken => "+",
                SyntaxKind.MinusToken => "-",
                SyntaxKind.SlashToken => "/",
                SyntaxKind.StarToken => "*",
                SyntaxKind.BangToken => "!",
                SyntaxKind.AmpersandAmpersandToken => "&&",
                SyntaxKind.LessThanToken => "<",
                SyntaxKind.LessThanEqualsToken => "<=",
                SyntaxKind.GreaterThanToken => ">",
                SyntaxKind.GreaterThanEqualsToken => ">=",
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