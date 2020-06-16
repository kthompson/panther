using System;

namespace Panther
{
    public static class Predef
    {
        public static void print(object value) => Console.Write(value);
        public static void println(object value) => Console.WriteLine(value);

        public static string readLine() => Console.ReadLine();
    }
}