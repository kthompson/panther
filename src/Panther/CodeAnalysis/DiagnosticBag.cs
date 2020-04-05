using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis
{
    internal sealed class DiagnosticBag : IEnumerable<Diagnostic>
    {
        private readonly List<Diagnostic> _diagnostics = new List<Diagnostic>();

        private void Report(TextLocation span, string message)
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

        public void ReportInvalidNumber(TextLocation textSpan, string text, TypeSymbol type) =>
            Report(textSpan, $"The number {text} isn't a valid '{type}'");

        public void ReportInvalidEscapeSequence(TextLocation location, in char current) =>
            Report(location, $"Invalid character in escape sequence: {current}");

        public void ReportBadCharacter(TextLocation location, in char character) =>
            Report(location, $"Invalid character in input: {character}");

        public void ReportUnexpectedToken(TextLocation location, SyntaxKind currentKind, SyntaxKind expectedKind) =>
            Report(location, $"Unexpected token {currentKind}, expected {expectedKind}");

        public void ReportUndefinedUnaryOperator(TextLocation location, string operatorText, TypeSymbol operandType) =>
            Report(location, $"Unary operator '{operatorText}' is not defined for type '{operandType}'");

        public void ReportUndefinedBinaryOperator(TextLocation location, string operatorText, TypeSymbol leftType,
            TypeSymbol rightType) =>
            Report(location, $"Binary operator '{operatorText}' is not defined for types '{leftType}' and '{rightType}'");

        public void ReportUndefinedName(TextLocation location, string name) =>
            Report(location, $"Variable '{name}' does not exist");

        public void ReportVariableAlreadyDefined(TextLocation location, string name) =>
            Report(location, $"Variable '{name}' is already defined in the current scope");

        public void ReportReassignmentToVal(TextLocation location, string name) =>
            Report(location, $"Reassignment to val '{name}'");

        public void ReportTypeMismatch(TextLocation location, TypeSymbol expectedType, TypeSymbol foundType) =>
            Report(location, $"Type mismatch. Required '{expectedType}', found '{foundType}'");

        public void ReportExpectedExpression(TextLocation location, SyntaxKind kind) =>
            Report(location, $"Unexpected token {kind}, expected Expression");

        public void ReportUndefinedFunction(TextLocation location, string name) =>
            Report(location, $"Function name '{name}' does not exist");

        public void ReportUnterminatedString(TextLocation location) =>
            Report(location, "Unterminated string literal");

        public void ReportNoOverloads(TextLocation location, string name, ImmutableArray<string> argumentTypes) =>
            Report(location, $"No overloads matching function name '{name}' and argument types {string.Join(", ", argumentTypes.Select(arg => $"'{arg}'")) }");

        public void ReportCannotConvert(TextLocation location, TypeSymbol fromType, TypeSymbol toType) =>
            Report(location, $"Cannot convert from '{fromType}' to '{toType}'");

        public void ReportCannotConvertImplicitly(TextLocation location, TypeSymbol fromType, TypeSymbol toType) =>
            Report(location, $"Cannot convert from '{fromType}' to '{toType}'. An explicit conversion exists, are you missing a cast?");

        public void ReportUndefinedType(TextLocation location, string name) =>
            Report(location, $"Type '{name}' is not defined");

        public void ReportArgumentTypeMismatch(TextLocation location, string parameterName, TypeSymbol expectedType, TypeSymbol actualType) =>
            Report(location, $"Argument {parameterName}, type mismatch. Expected '{expectedType}', found '{actualType}'");

        public void ReportParameterAlreadyDeclared(TextLocation location, string parameterName) =>
            Report(location, $"Function parameter '{parameterName}' was already declared");

        public void ReportFunctionAlreadyDeclared(TextLocation location, string functionName) =>
            Report(location, $"Function '{functionName}' was already declared");

        public void ReportInvalidBreakOrContinue(TextLocation location, string keyword) =>
            Report(location, $"{keyword} not valid in this context");

        public void ReportAllPathsMustReturn(TextLocation location) =>
            Report(location, "All paths must return a value");

        public void ReportNotAFunction(TextLocation location, string name) =>
            Report(location, $"Variable '{name}' is not a function");
    }
}