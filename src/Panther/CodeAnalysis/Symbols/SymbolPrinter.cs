using System;
using System.IO;
using Panther.IO;

namespace Panther.CodeAnalysis.Symbols
{
    internal static class SymbolPrinter
    {
        public static void WriteTo(this Symbol symbol, TextWriter writer)
        {
            switch (symbol)
            {
                case MethodSymbol functionSymbol:
                    WriteMethodSymbol(functionSymbol, writer);
                    break;
                case TypeSymbol typeSymbol:
                    WriteTypeSymbol(typeSymbol, writer);
                    break;
                case FieldSymbol globalVariableSymbol:
                    WriteFieldSymbol(globalVariableSymbol, writer);
                    break;
                case ParameterSymbol parameterSymbol:
                    WriteParameterSymbol(parameterSymbol, writer);
                    break;
                case LocalVariableSymbol localVariableSymbol:
                    WriteLocalVariableSymbol(localVariableSymbol, writer);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(symbol));
            }
        }

        private static void WriteTypeSymbol(TypeSymbol symbol, TextWriter writer)
        {
            writer.WriteKeyword("object ");
            writer.WriteIdentifier(symbol.Name);
        }

        private static void WriteLocalVariableSymbol(LocalVariableSymbol symbol, TextWriter writer)
        {
            writer.WriteKeyword(symbol.IsReadOnly ? "val " : "var ");
            writer.WriteIdentifier(symbol.Name);
            writer.WritePunctuation(": ");
            symbol.Type.WriteTo(writer);
        }

        private static void WriteParameterSymbol(ParameterSymbol symbol, TextWriter writer)
        {
            writer.WriteIdentifier(symbol.Name);
            writer.WritePunctuation(" : ");
            symbol.Type.WriteTo(writer);
        }

        private static void WriteFieldSymbol(FieldSymbol symbol, TextWriter writer)
        {
            writer.WriteKeyword(symbol.IsReadOnly ? "val " : "var ");
            writer.WriteIdentifier(symbol.Name);
            writer.WritePunctuation(": ");
            symbol.Type.WriteTo(writer);
        }

        private static void WriteMethodSymbol(MethodSymbol symbol, TextWriter writer)
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
}