using System;
using System.IO;
using Panther.IO;

namespace Panther.CodeAnalysis.Symbols;

internal static class SymbolPrinter
{
    public static string ToPrintString(this Type type)
    {
        using var writer = new StringWriter();
        type.WriteTo(writer);
        return writer.ToString();
    }

    public static void WriteTo(this Type symbol, TextWriter writer)
    {
        switch (symbol)
        {
            case ClassType(var classSym):
                writer.WriteIdentifier(classSym.Name);
                break;

            case ErrorType:
                writer.WriteIdentifier("<err>");
                break;

            case MethodType methodType:
                writer.WritePunctuation("(");

                var enumerator = methodType.Parameters.GetEnumerator();
                if (enumerator.MoveNext())
                {
                    enumerator.Current.WriteTo(writer);

                    while (enumerator.MoveNext())
                    {
                        writer.WritePunctuation(", ");
                        enumerator.Current.WriteTo(writer);
                    }
                }

                writer.WritePunctuation(") => ");
                methodType.ResultType.WriteTo(writer);
                break;
            case NoType:
                writer.WriteIdentifier("<none>");
                break;
            case TypeConstructor typeConstructor:
                writer.WriteIdentifier(typeConstructor.Name);
                break;
            case Unresolved:
                writer.WriteIdentifier("<unresolved>");
                break;
            case ArrayType(_, var elementType):
                writer.WriteIdentifier("Array");
                writer.WritePunctuation("<");
                elementType.WriteTo(writer);
                writer.WritePunctuation(">");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(symbol));
        }
    }

    public static void WriteTo(this Symbol symbol, TextWriter writer)
    {
        switch (symbol)
        {
            case { IsMethod: true }:
                WriteMethodSymbol(symbol, writer);
                break;

            case TypeSymbol
            or { IsType: true }:
                WriteTypeSymbol(symbol, writer);
                break;

            case { IsParameter: true }:
                WriteParameterSymbol(symbol, writer);
                break;

            case { IsLocal: true }:
                WriteValueSymbol(symbol, writer);
                break;

            case { IsField: true }:
                WriteValueSymbol(symbol, writer);
                break;

            case var sym when sym == Symbol.None:
                writer.WriteIdentifier("<none>");
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(symbol));
        }
    }

    private static void WriteTypeSymbol(Symbol symbol, TextWriter writer)
    {
        writer.WriteIdentifier(symbol.Name);
    }

    private static void WriteParameterSymbol(Symbol symbol, TextWriter writer)
    {
        writer.WriteIdentifier(symbol.Name);
        writer.WritePunctuation(": ");
        symbol.Type.WriteTo(writer);
    }

    private static void WriteValueSymbol(Symbol symbol, TextWriter writer)
    {
        writer.WriteKeyword(symbol.IsReadOnly ? "val " : "var ");
        writer.WriteIdentifier(symbol.Name);
        writer.WritePunctuation(": ");
        symbol.Type.WriteTo(writer);
    }

    private static void WriteMethodSymbol(Symbol symbol, TextWriter writer)
    {
        writer.WriteKeyword("def ");
        writer.WriteIdentifier(symbol.Name);
        writer.WritePunctuation("(");

        var enumerator = symbol.Parameters.GetEnumerator();
        if (enumerator.MoveNext())
        {
            enumerator.Current.WriteTo(writer);

            while (enumerator.MoveNext())
            {
                writer.WritePunctuation(", ");
                enumerator.Current.WriteTo(writer);
            }
        }

        writer.WritePunctuation("): ");
        symbol.ReturnType.WriteTo(writer);
    }
}
