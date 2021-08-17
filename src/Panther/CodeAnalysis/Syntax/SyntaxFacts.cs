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
                case SyntaxKind.EqualsToken:
                    return (OperatorPrecedence)1;

                case SyntaxKind.PipeToken:
                case SyntaxKind.PipePipeToken:
                    return (OperatorPrecedence)2;

                case SyntaxKind.CaretToken:
                    return (OperatorPrecedence)3;

                case SyntaxKind.AmpersandToken:
                case SyntaxKind.AmpersandAmpersandToken:
                    return (OperatorPrecedence)4;

                case SyntaxKind.EqualsEqualsToken:
                case SyntaxKind.BangEqualsToken:
                    return (OperatorPrecedence)5;

                case SyntaxKind.LessThanToken:
                case SyntaxKind.LessThanEqualsToken:
                case SyntaxKind.GreaterThanToken:
                case SyntaxKind.GreaterThanEqualsToken:
                    return (OperatorPrecedence)6;

                case SyntaxKind.PlusToken:
                case SyntaxKind.DashToken:
                    return (OperatorPrecedence)7;

                case SyntaxKind.StarToken:
                case SyntaxKind.SlashToken:
                    return (OperatorPrecedence)8;

                // Unary/Prefix expressions are 9
                case SyntaxKind.OpenParenToken:
                    return (OperatorPrecedence)10;

                case SyntaxKind.DotToken:
                    return (OperatorPrecedence)11;

                default:
                    return null;
            }
        }

        public static SyntaxKind GetKeywordKind(string span)
        {
            return span switch
            {
                "break" => SyntaxKind.BreakKeyword,
                "class" => SyntaxKind.ClassKeyword,
                "continue" => SyntaxKind.ContinueKeyword,
                "def" => SyntaxKind.DefKeyword,
                "else" => SyntaxKind.ElseKeyword,
                "false" => SyntaxKind.FalseKeyword,
                "for" => SyntaxKind.ForKeyword,
                "if" => SyntaxKind.IfKeyword,
                "implicit" => SyntaxKind.ImplicitKeyword,
                "namespace" => SyntaxKind.NamespaceKeyword,
                "object" => SyntaxKind.ObjectKeyword,
                "new" => SyntaxKind.NewKeyword,
                "static" => SyntaxKind.StaticKeyword,
                "to" => SyntaxKind.ToKeyword,
                "true" => SyntaxKind.TrueKeyword,
                "using" => SyntaxKind.UsingKeyword,
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
                SyntaxKind.BreakKeyword => "break",
                SyntaxKind.CaretToken => "^",
                SyntaxKind.ClassKeyword => "class",
                SyntaxKind.CloseBraceToken => "}",
                SyntaxKind.CloseParenToken => ")",
                SyntaxKind.ColonToken => ":",
                SyntaxKind.CommaToken => ",",
                SyntaxKind.ContinueKeyword => "continue",
                SyntaxKind.DashToken => "-",
                SyntaxKind.DefKeyword => "def",
                SyntaxKind.DotToken => ".",
                SyntaxKind.ElseKeyword => "else",
                SyntaxKind.EqualsEqualsToken => "==",
                SyntaxKind.EqualsToken => "=",
                SyntaxKind.FalseKeyword => "false",
                SyntaxKind.ForKeyword => "for",
                SyntaxKind.GreaterThanEqualsToken => ">=",
                SyntaxKind.GreaterThanToken => ">",
                SyntaxKind.IfKeyword => "if",
                SyntaxKind.ImplicitKeyword => "implicit",
                SyntaxKind.LessThanEqualsToken => "<=",
                SyntaxKind.LessThanToken => "<",
                SyntaxKind.NamespaceKeyword => "namespace",
                SyntaxKind.ObjectKeyword => "object",
                SyntaxKind.NewKeyword => "new",
                SyntaxKind.OpenBraceToken => "{",
                SyntaxKind.OpenParenToken => "(",
                SyntaxKind.PipePipeToken => "||",
                SyntaxKind.PipeToken => "|",
                SyntaxKind.PlusToken => "+",
                SyntaxKind.SlashToken => "/",
                SyntaxKind.StarToken => "*",
                SyntaxKind.StaticKeyword => "static",
                SyntaxKind.TildeToken => "~",
                SyntaxKind.ToKeyword => "to",
                SyntaxKind.TrueKeyword => "true",
                SyntaxKind.UsingKeyword => "using",
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
                .Where(kind =>
                    kind != SyntaxKind.EqualsToken && kind != SyntaxKind.OpenParenToken &&
                    kind != SyntaxKind.DotToken && GetBinaryOperatorPrecedence(kind) > 0);

        public static bool IsTrivia(this SyntaxKind kind) =>
            kind switch
            {
                SyntaxKind.InvalidTokenTrivia => true,
                SyntaxKind.EndOfLineTrivia => true,
                SyntaxKind.WhitespaceTrivia => true,
                SyntaxKind.LineCommentTrivia => true,
                SyntaxKind.BlockCommentTrivia => true,
                _ => false
            };

        public static bool IsComment(this SyntaxKind kind) =>
            kind switch
            {
                SyntaxKind.LineCommentTrivia => true,
                SyntaxKind.BlockCommentTrivia => true,
                _ => false
            };

        public static bool IsKeyword(this SyntaxKind kind) => kind.ToString().EndsWith("Keyword");
    }
}