using System;
using System.IO;
using Panther.IO;

namespace Panther.CodeAnalysis.Symbols;

internal static class SymbolPrinter
{
    public static void WriteTo(this Type symbol, TextWriter writer)
    {
        switch (symbol)
        {
            case ClassType:
                writer.WriteIdentifier("<classtype>");
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
            default:
                throw new ArgumentOutOfRangeException(nameof(symbol));
        }
    }

    public static void WriteTo(this ISymbol symbol, TextWriter writer)
    {
        switch (symbol.Kind)
        {
            case SymbolKind.Method:
                WriteMethodSymbol(symbol, writer);
                break;

            case SymbolKind.Class:
                WriteTypeSymbol(symbol, writer);
                break;

            case SymbolKind.Parameter:
                WriteParameterSymbol(symbol, writer);
                break;

            case SymbolKind.Value
            or SymbolKind.Variable:
                WriteValueSymbol(symbol, writer);
                break;

            case SymbolKind.Field:
                WriteValueSymbol(symbol, writer);
                break;
            //
            // case var sym when sym == Symbol.None:
            //     writer.WriteIdentifier("<none>");
            //     break;

            default:
                throw new ArgumentOutOfRangeException(nameof(symbol));
        }
    }

    private static void WriteTypeSymbol(ISymbol symbol, TextWriter writer)
    {
        writer.WriteIdentifier(symbol.Name);
    }

    private static void WriteParameterSymbol(ISymbol symbol, TextWriter writer)
    {
        writer.WriteIdentifier(symbol.Name);
        // writer.WritePunctuation(": ");
        // symbol.Type.WriteTo(writer);
    }

    private static void WriteValueSymbol(ISymbol symbol, TextWriter writer)
    {
        writer.WriteKeyword(symbol.Kind == SymbolKind.Value ? "val " : "var ");
        writer.WriteIdentifier(symbol.Name);
        // writer.WritePunctuation(": ");
        // symbol.Type.WriteTo(writer);
    }

    private static void WriteMethodSymbol(ISymbol symbol, TextWriter writer)
    {
        writer.WriteKeyword("def ");
        writer.WriteIdentifier(symbol.Name);
        // writer.WritePunctuation("(");
        //
        // var enumerator = symbol.Parameters.GetEnumerator();
        // if (enumerator.MoveNext())
        // {
        //     enumerator.Current.WriteTo(writer);
        //
        //     while (enumerator.MoveNext())
        //     {
        //         writer.WritePunctuation(", ");
        //         enumerator.Current.WriteTo(writer);
        //     }
        // }
        //
        // writer.WritePunctuation("): ");
        // symbol.ReturnType.WriteTo(writer);
    }
}
