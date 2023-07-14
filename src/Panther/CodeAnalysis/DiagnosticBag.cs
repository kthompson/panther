using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;
using Panther.CodeAnalysis.Text;
using Panther.CodeAnalysis.Typing;

namespace Panther.CodeAnalysis;

internal sealed class DiagnosticBag : IEnumerable<Diagnostic>
{
    private readonly List<Diagnostic> _diagnostics = new List<Diagnostic>();

    private void Report(TextLocation? span, string message)
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

    private string FriendlyKind(SyntaxKind kind)
    {
        var text = SyntaxFacts.GetText(kind);
        if (text != null)
            return $"'{text}'";

        var str = kind.ToString();
        return Humanize(
            str.EndsWith("Keyword")
                ? str[..^7]
                : str.EndsWith("Token")
                    ? str[..^5]
                    : str
        );
    }

    private string Humanize(string words)
    {
        return Regex.Replace(words, "([a-z])([A-Z])", "$1 $2").ToLowerInvariant();
    }

    public void ReportInvalidNumber(TextLocation textSpan, string text, string type) =>
        Report(textSpan, $"The number {text} isn't a valid '{type}'");

    public void ReportInvalidEscapeSequence(TextLocation location, in char current) =>
        Report(location, $"Invalid character in escape sequence: {current}");

    public void ReportExpectedCharacterLiteral(TextLocation location) =>
        Report(location, "Expected character literal");

    public void ReportBadCharacter(TextLocation location, in char character) =>
        Report(location, $"Invalid character in input: {character}");

    public void ReportUnexpectedToken(
        TextLocation location,
        SyntaxKind currentKind,
        SyntaxKind expectedKind
    ) =>
        Report(
            location,
            $"Unexpected token {FriendlyKind(currentKind)}, expected {FriendlyKind(expectedKind)}"
        );

    public void ReportUnexpectedOpCode(TextLocation location, SyntaxKind currentKind) =>
        Report(location, $"Unexpected opcode {FriendlyKind(currentKind)}");

    public void ReportExpectedEndOfLineTrivia(TextLocation location) =>
        Report(location, "Expected end of line trivia but none found");

    public void ReportUndefinedUnaryOperator(
        TextLocation location,
        string operatorText,
        Type operandType
    ) =>
        Report(
            location,
            $"Unary operator '{operatorText}' is not defined for type '{operandType.ToPrintString()}'"
        );

    public void ReportUndefinedBinaryOperator(
        TextLocation location,
        string operatorText,
        Type leftType,
        Type rightType
    ) =>
        Report(
            location,
            $"Binary operator '{operatorText}' is not defined for types '{leftType.ToPrintString()}' and '{rightType.ToPrintString()}'"
        );

    public void ReportUndefinedName(TextLocation location, string name) =>
        Report(location, $"Variable '{name}' does not exist");

    public void ReportVariableAlreadyDefined(TextLocation location, string name) =>
        Report(location, $"Variable '{name}' is already defined in the current scope");

    public void ReportReassignmentToVal(TextLocation location, string name) =>
        Report(location, $"Reassignment to val '{name}'");

    public void ReportTypeMismatch(TextLocation location, Type expectedType, Type foundType) =>
        Report(
            location,
            $"Type mismatch. Required '{expectedType.ToPrintString()}', found '{foundType.ToPrintString()}'"
        );

    public void ReportExpectedExpression(TextLocation location, SyntaxKind kind) =>
        Report(location, $"Unexpected token {FriendlyKind(kind)}, expected Expression");

    public void ReportUndefinedFunction(TextLocation location, string name) =>
        Report(location, $"Function name '{name}' does not exist");

    public void ReportUnterminatedString(TextLocation location) =>
        Report(location, "Unterminated string literal");

    public void ReportUnterminatedChar(TextLocation location) =>
        Report(location, "Unterminated char literal");

    public void ReportNoOverloads(
        TextLocation location,
        string name,
        ImmutableArray<string> argumentTypes
    ) =>
        Report(
            location,
            $"No overloads matching function name '{name}' and argument types {string.Join(", ", argumentTypes.Select(arg => $"'{arg}'"))}"
        );

    public void ReportAmbiguousMethod(
        TextLocation location,
        string name,
        ImmutableArray<string> argumentTypes
    ) =>
        Report(
            location,
            $"Ambiguous method '{name}' and argument types {string.Join(", ", argumentTypes.Select(arg => $"'{arg}'"))}"
        );

    public void ReportCannotConvert(TextLocation location, Type fromType, Type toType) =>
        Report(
            location,
            $"Cannot convert from '{fromType.ToPrintString()}' to '{toType.ToPrintString()}'"
        );

    public void ReportCannotConvertImplicitly(TextLocation location, Type fromType, Type toType) =>
        Report(
            location,
            $"Cannot convert from '{fromType.ToPrintString()}' to '{toType.ToPrintString()}'. An explicit conversion exists, are you missing a cast?"
        );

    public void ReportUndefinedTypeOrInitializer(TextLocation location) =>
        Report(location, $"A type of initializer is required for a Variable/Value declaration");

    public void ReportUndefinedType(TextLocation location, string name) =>
        Report(location, $"Type '{name}' is not defined");

    public void ReportTypeAnnotationRequired(TextLocation location) =>
        Report(location, "Type annotation is required");

    public void ReportDuplicateParameter(TextLocation location, string parameterName) =>
        Report(location, $"Duplicate parameter '{parameterName}'");

    public void ReportInvalidBreakOrContinue(TextLocation location, string keyword) =>
        Report(location, $"{keyword} not valid in this context");

    public void ReportAllPathsMustReturn(TextLocation location) =>
        Report(location, "All paths must return a value");

    public void ReportNotAFunction(TextLocation location, string name) =>
        Report(location, $"Variable '{name}' is not a function");

    public void ReportInvalidExpressionStatement(TextLocation location) =>
        Report(location, $"Only expressions with side effects can be used as a statement");

    public void ReportCannotMixMainAndGlobalStatements(TextLocation? location) =>
        Report(location, "Cannot mix main and global statements");

    public void ReportMainMustHaveCorrectSignature(TextLocation? location) =>
        Report(location, "Main must have no parameters and return unit");

    public void ReportGlobalStatementsCanOnlyExistInOneFile(TextLocation location) =>
        Report(location, "Global statements can only exist in one file");

    public void ReportUnsupportedFieldAccess(TextLocation location, string fieldAssigment) =>
        Report(location, $"Unsupported field access: {fieldAssigment}");

    public void ReportUnsupportedFunctionCall(TextLocation location) =>
        Report(location, $"Unsupported function call");

    public void ReportUnterminatedBlockComment(TextLocation location) =>
        Report(location, "Unterminated block comment");

    public void ReportNotAssignable(TextLocation location) =>
        Report(location, "Left hand side of expression is not assignable");

    public void ReportAmbiguousType(TextLocation location, string typeName) =>
        Report(location, $"Duplicate type '{typeName}' detected");

    public void ReportMissingDefinition(TextLocation location, Type type, string name) =>
        Report(location, $"'{type.ToPrintString()}' does not contain a definition for '{name}'");

    public void ReportInvalidReference(string reference) =>
        Report(null, $"The specified reference is not valid: {reference}");

    public void ReportBuiltinTypeNotFound(string builtinName) =>
        Report(null, $"The required builtin type '{builtinName}' could not be found");

    public void ReportTypeNotFound(TextLocation location, string typeName) =>
        Report(location, $"The type '{typeName}' could not be found");

    public void ReportTypeNotFound(string typeName) =>
        Report(null, $"The required type '{typeName}' could not be found");

    public void ReportAmbiguousBuiltinType(
        string builtinName,
        IEnumerable<TypeDefinition> foundTypes
    )
    {
        var assemblyNames =
            from type in foundTypes
            let asmName = type.Module.Assembly.Name.Name
            group type by asmName into g
            select g.Key;
        var assemblyNameList = string.Join(", ", assemblyNames);

        Report(
            null,
            $"Ambiguous builtin type '{builtinName}' was found in the given assemblies: {assemblyNameList}"
        );
    }

    public void ReportAmbiguousType(string typeName, ImmutableArray<TypeDefinition> foundTypes)
    {
        var assemblyNames =
            from type in foundTypes
            let asmName = type.Module.Assembly.Name.Name
            group type by asmName into g
            select g.Key;
        var assemblyNameList = string.Join(", ", assemblyNames);

        Report(
            null,
            $"Ambiguous type '{typeName}' was found in the given assemblies: {assemblyNameList}"
        );
    }

    public void ReportRequiredMethodNotFound(
        string typeName,
        string methodName,
        string[] parameterTypeNames
    )
    {
        var parameterTypeNamesList = string.Join(", ", parameterTypeNames);
        Report(
            null,
            $"Required method {typeName}.{methodName}({parameterTypeNamesList}) was not found"
        );
    }

    public void ReportRequiredFieldNotFound(string typeName, string fieldName) =>
        Report(null, $"Required field {typeName}.{fieldName} was not found");

    public void ReportExpressionDoesNotSupportIndexOperator(TextLocation location) =>
        Report(location, "Expression does not support index operator");

    public void ReportArrayCreationRequiresRankOrInitializer(TextLocation location) =>
        Report(location, "Array creation requires rank or initializer");

    public void ReportArrayRankMustBeAnInt(TextLocation location) =>
        Report(location, "Array rank must be of type 'int'");

    public void ReportArrayOnlyRankOrInitializer(TextLocation location) =>
        Report(location, "Array cannot provide rank and initializer");

    public void ReportGenericTypeNotSupported(TextLocation location, string typeName) =>
        Report(location, $"Generic type not supported: {typeName}");

    public void ReportNoThisInScope(TextLocation location, string scopeName) =>
        Report(location, $"`this` keyword not valid in {scopeName} scope");

    public void MultipleEntryPoints(TextLocation location) =>
        Report(location, $"Multiple entry points were detected");
}
