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
                case FunctionSymbol functionSymbol:
                    WriteFunctionSymbol(functionSymbol, writer);
                    break;
                case TypeSymbol typeSymbol:
                    WriteTypeSymbol(typeSymbol, writer);
                    break;
                case GlobalVariableSymbol globalVariableSymbol:
                    WriteGlobalVariableSymbol(globalVariableSymbol, writer);
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

        private static void WriteGlobalVariableSymbol(GlobalVariableSymbol symbol, TextWriter writer)
        {
            writer.WriteKeyword(symbol.IsReadOnly ? "val " : "var ");
            writer.WriteIdentifier(symbol.Name);
            writer.WritePunctuation(": ");
            symbol.Type.WriteTo(writer);
        }

        private static void WriteFunctionSymbol(FunctionSymbol symbol, TextWriter writer)
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