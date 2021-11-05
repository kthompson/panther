using System;
using static Panther.Predef;

public static partial class Program
{
    public static int x;
    public static void main()
    {
        x = 12;
        println(Convert.ToString(x));
    }
}
