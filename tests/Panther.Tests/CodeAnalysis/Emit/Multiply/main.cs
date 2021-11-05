using System;
using static Panther.Predef;

public static partial class Program
{
    public static void main()
    {
        println(Convert.ToString(multiply(1, 7)));
        println(Convert.ToString(multiply(2, 18)));
    }

    public static int multiply(int a, int b)
    {
        return a * b;
    }
}
