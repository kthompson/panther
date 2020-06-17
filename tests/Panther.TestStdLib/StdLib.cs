using System;
using System.Collections.Generic;
using System.IO;

namespace Panther
{
    public static class TestHarness
    {
        private static readonly StringWriter _writer = new StringWriter();
        private static readonly Queue<string> _lines = new Queue<string>();

        public static string GetOutput()
        {
            return _writer.ToString();
        }

        public static void RegisterReadLine(string value)
        {
            _lines.Enqueue(value);
        }

        public static void print(object value) => _writer.Write(value);
        public static void println(object value) => _writer.WriteLine(value);
        public static string readLine() => _lines.Dequeue();
    }

    public static class Predef
    {
        public static void print(object value) => TestHarness.print(value);
        public static void println(object value) => TestHarness.println(value);
        public static string readLine() => TestHarness.readLine();
    }

    public class Unit
    {
        private Unit()
        {
        }

        public static readonly Unit Default = new Unit();

        public override string ToString()
        {
            return "unit";
        }
    }
}