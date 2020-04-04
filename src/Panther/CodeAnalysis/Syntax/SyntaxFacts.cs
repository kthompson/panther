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

                case SyntaxKind.ContinueKeyword:
                case SyntaxKind.BreakKeyword:
                    return OperatorPrecedence.Prefix;

                default:
                    return null;
            }
        }

        public static SyntaxKind GetKeywordKind(string span)
        {
            return span switch
            {
                "break" => SyntaxKind.BreakKeyword,
                "continue" => SyntaxKind.ContinueKeyword,
                "def" => SyntaxKind.DefKeyword,
                "else" => SyntaxKind.ElseKeyword,
                "false" => SyntaxKind.FalseKeyword,
                "for" => SyntaxKind.ForKeyword,
                "if" => SyntaxKind.IfKeyword,
                "to" => SyntaxKind.ToKeyword,
                "true" => SyntaxKind.TrueKeyword,
                "val" => SyntaxKind.ValKeyword,
                "var" => SyntaxKind.VarKeyword,
                "while" => SyntaxKind.WhileKeyword,
                _ => SyntaxKind.IdentifierToken
            };
        }

        public static string? GetText(SyntaxKind kind) =>
            kind switch
            {
                SyntaxKind.AmpersandAmpersandToken => "&&",
                SyntaxKind.AmpersandToken => "&",
                SyntaxKind.BangEqualsToken => "!=",
                SyntaxKind.BangToken => "!",
                SyntaxKind.BreakKeyword => "break" ,
                SyntaxKind.CaretToken => "^",
                SyntaxKind.CloseBraceToken => "}",
                SyntaxKind.CloseParenToken => ")",
                SyntaxKind.ColonToken => ":",
                SyntaxKind.CommaToken => ",",
                SyntaxKind.ContinueKeyword => "continue",
                SyntaxKind.DashToken => "-",
                SyntaxKind.DefKeyword => "def",
                SyntaxKind.ElseKeyword => "else",
                SyntaxKind.EqualsEqualsToken => "==",
                SyntaxKind.EqualsToken => "=",
                SyntaxKind.FalseKeyword => "false",
                SyntaxKind.ForKeyword => "for",
                SyntaxKind.GreaterThanEqualsToken => ">=",
                SyntaxKind.GreaterThanToken => ">",
                SyntaxKind.IfKeyword => "if",
                SyntaxKind.LessThanEqualsToken => "<=",
                SyntaxKind.LessThanToken => "<",
                SyntaxKind.OpenBraceToken => "{",
                SyntaxKind.OpenParenToken => "(",
                SyntaxKind.PipePipeToken => "||",
                SyntaxKind.PipeToken => "|",
                SyntaxKind.PlusToken => "+",
                SyntaxKind.SlashToken => "/",
                SyntaxKind.StarToken => "*",
                SyntaxKind.TildeToken => "~",
                SyntaxKind.ToKeyword => "to",
                SyntaxKind.TrueKeyword => "true",
                SyntaxKind.ValKeyword => "val",
                SyntaxKind.VarKeyword => "var",
                SyntaxKind.WhileKeyword => "while",
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