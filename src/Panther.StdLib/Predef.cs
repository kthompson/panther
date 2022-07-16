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
}
