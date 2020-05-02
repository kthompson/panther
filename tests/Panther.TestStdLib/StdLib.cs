using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Panther
{
    public static class Predef
    {
        private static readonly StringWriter _writer = new StringWriter();
        private static readonly Queue<string> _lines = new Queue<string>();

        public static string getOutput()
        {
            var output = _writer.ToString();
            _writer.GetStringBuilder().Clear();
            return output;
        }

        public static void mockReadLine(string value)
        {
            _lines.Enqueue(value);
        }

        public static void print(object value) => _writer.Write(value);
        public static void println(object value) => _writer.WriteLine(value);
        public static string readLine() => _lines.Dequeue();
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