using System;

namespace Panther;

public static class Predef
{
    public static void print(object value) => Console.Write(value);

    public static void println(object value) => Console.WriteLine(value);

    public static void println() => Console.WriteLine();

    public static string? readLine() => Console.ReadLine();

    private static Random _random = new Random();

    public static int rnd(int max) => _random.Next(max);

    // Helper methods (not to be used externally)
    public static int strlen(string str) => str.Length;

    public static char getchar(string str, int index) => str[index];

    public static int getargc() => Environment.GetCommandLineArgs().Length;

    public static string getarg(int index) => Environment.GetCommandLineArgs()[index];
}
