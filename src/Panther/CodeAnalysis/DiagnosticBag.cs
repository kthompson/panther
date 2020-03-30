using System;
using System.Collections;
using System.Collections.Generic;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis
{
    internal sealed class DiagnosticBag : IEnumerable<Diagnostic>
    {
        private readonly List<Diagnostic> _diagnostics = new List<Diagnostic>();

        private void Report(TextSpan span, string message)
        {
            var diagnostic = new Diagnostic(span, message);
            _diagnostics.Add(diagnostic);
        }

        public DiagnosticBag AddRange(IEnumerable<Diagnostic> diagnostics)
        {
            this._diagnostics.AddRange(diagnostics);
            return this;
        }

        public IEnumerator<Diagnostic> GetEnumerator() => _diagnostics.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void ReportInvalidNumber(TextSpan textSpan, string text, TypeSymbol type) =>
            Report(textSpan, $"The number {text} isn't a valid '{type}'");

        public void ReportInvalidEscapeSequence(in int escapeStart, in int position, in char current) =>
            Report(new TextSpan(escapeStart, position), $"Invalid character in escape sequence: {current}");

        public void ReportBadCharacter(int position, in char character) =>
            Report(new TextSpan(position, 1), $"Invalid character in input: {character}");

        public void ReportUnexpectedToken(TextSpan span, SyntaxKind currentKind, SyntaxKind expectedKind) =>
            Report(span, $"Unexpected token {currentKind}, expected {expectedKind}");

        public void ReportUndefinedUnaryOperator(TextSpan span, string operatorText, TypeSymbol operandType) =>
            Report(span, $"Unary operator '{operatorText}' is not defined for type '{operandType}'");

        public void ReportUndefinedBinaryOperator(TextSpan span, string operatorText, TypeSymbol leftType,
            TypeSymbol rightType) =>
            Report(span, $"Binary operator '{operatorText}' is not defined for types '{leftType}' and '{rightType}'");

        public void ReportUndefinedName(TextSpan span, string name) =>
            Report(span, $"Variable '{name}' does not exist");

        public void ReportVariableAlreadyDefined(TextSpan span, string name) =>
            Report(span, $"Variable '{name}' is already defined in the current scope");

        public void ReportReassignmentToVal(TextSpan span, string name) =>
            Report(span, $"Reassignment to val '{name}'");

        public void ReportTypeMismatch(TextSpan span, TypeSymbol expectedType, TypeSymbol foundType) =>
            Report(span, $"Type mismatch. Required '{expectedType}', found '{foundType}'");

        public void ReportArgumentTypeMismatch(TextSpan span, int number, TypeSymbol expectedType, TypeSymbol foundType) =>
            Report(span, $"Argument {number}, type mismatch. Required '{expectedType}', found '{foundType}'");

        public void ReportExpectedExpression(TextSpan span, SyntaxKind kind) =>
            Report(span, $"Unexpected token {kind}, expected Expression");

        public void ReportUndefinedFunction(TextSpan span, string name) =>
            Report(span, $"Function name '{name}' does not exist");

        public void ReportNotAFunction(TextSpan span, string name) =>
            Report(span, $"The variable '{name}' is not a function");

        public void ReportIncorrectNumberOfArgumentsForFunction(TextSpan span, string name, int expected, int found) =>
            Report(span, $"Incorrect number of arguments for '{name}', expected {expected}, found {found}");

        public void ReportUnterminatedString(TextSpan span)
        {
            Report(span, "Unterminated string literal");
        }
    }
}