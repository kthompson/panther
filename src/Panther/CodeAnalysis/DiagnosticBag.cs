using System;
using System.Collections;
using System.Collections.Generic;
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

        public void ReportInvalidNumber(TextSpan textSpan, string text, Type type) =>
            Report(textSpan, $"The number {text} isn't a valid {type}");

        public void ReportBadCharacter(int position, in char character) =>
            Report(new TextSpan(position, 1), $"Invalid character in input: {character}");

        public void ReportUnexpectedToken(TextSpan span, SyntaxKind currentKind, SyntaxKind expectedKind) =>
            Report(span, $"Unexpected token {currentKind}, expected {expectedKind}");

        public void ReportUndefinedUnaryOperator(TextSpan span, string operatorText, Type operandType) =>
            Report(span, $"Unary operator '{operatorText}' is not defined for type {operandType}");

        public void ReportUndefinedBinaryOperator(TextSpan span, string operatorText, Type leftType, Type rightType) =>
            Report(span, $"Binary operator '{operatorText}' is not defined for types {leftType} and {rightType}");

        public void ReportUndefinedName(TextSpan span, string name) =>
            Report(span, $"Variable '{name}' does not exist");

        public void ReportVariableAlreadyDefined(TextSpan span, string name) =>
            Report(span, $"Variable '{name}' is already defined");
    }
}