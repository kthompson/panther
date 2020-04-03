using System;
using System.Collections.Generic;
using System.Linq;

namespace Panther.CodeAnalysis.Syntax
{
    public static class SyntaxFacts
    {
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
                case SyntaxKind.PipeToken:
                case SyntaxKind.PipePipeToken:
                    return (OperatorPrecedence)1;

                case SyntaxKind.CaretToken:
                    return (OperatorPrecedence)1;

                case SyntaxKind.AmpersandToken:
                case SyntaxKind.AmpersandAmpersandToken:
                    return (OperatorPrecedence)3;

                case SyntaxKind.EqualsEqualsToken:
                case SyntaxKind.BangEqualsToken:
                    return (OperatorPrecedence)4;

                case SyntaxKind.LessThanToken:
                case SyntaxKind.LessThanEqualsToken:
                case SyntaxKind.GreaterThanToken:
                case SyntaxKind.GreaterThanEqualsToken:
                    return (OperatorPrecedence)5;

                case SyntaxKind.PlusToken:
                case SyntaxKind.DashToken:
                    return (OperatorPrecedence)6;

                case SyntaxKind.StarToken:
                case SyntaxKind.SlashToken:
                    return (OperatorPrecedence)7;

                default:
                    return null;
            }
        }

        public static SyntaxKind GetKeywordKind(string span)
        {
            return span switch
            {
                "if" => SyntaxKind.IfKeyword,
                "else" => SyntaxKind.ElseKeyword,
                "while" => SyntaxKind.WhileKeyword,
                "true" => SyntaxKind.TrueKeyword,
                "false" => SyntaxKind.FalseKeyword,
                "val" => SyntaxKind.ValKeyword,
                "def" => SyntaxKind.DefKeyword,
                "var" => SyntaxKind.VarKeyword,
                "for" => SyntaxKind.ForKeyword,
                "to" => SyntaxKind.ToKeyword,
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
                SyntaxKind.ElseKeyword => "else",
                SyntaxKind.IfKeyword => "if",
                SyntaxKind.WhileKeyword => "while",
                SyntaxKind.ForKeyword => "for",
                SyntaxKind.DefKeyword => "def",
                SyntaxKind.ToKeyword => "to",
                SyntaxKind.PlusToken => "+",
                SyntaxKind.DashToken => "-",
                SyntaxKind.SlashToken => "/",
                SyntaxKind.StarToken => "*",
                SyntaxKind.BangToken => "!",
                SyntaxKind.TildeToken => "~",
                SyntaxKind.ColonToken => ":",
                SyntaxKind.CommaToken => ",",
                SyntaxKind.CaretToken => "^",
                SyntaxKind.AmpersandToken => "&",
                SyntaxKind.AmpersandAmpersandToken => "&&",
                SyntaxKind.LessThanToken => "<",
                SyntaxKind.LessThanEqualsToken => "<=",
                SyntaxKind.GreaterThanToken => ">",
                SyntaxKind.GreaterThanEqualsToken => ">=",
                SyntaxKind.PipeToken => "|",
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
            SyntaxKind.TildeToken,
            SyntaxKind.PlusToken,
            SyntaxKind.DashToken,
            SyntaxKind.BangToken,
        };

        public static IEnumerable<SyntaxKind> GetBinaryOperatorKinds() =>
            Enum.GetValues(typeof(SyntaxKind)).Cast<SyntaxKind>()
                .Where(kind => GetBinaryOperatorPrecedence(kind) > 0);
    }
}