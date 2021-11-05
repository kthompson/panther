using System;
using static Panther.Predef;

public static partial class Program
{
    public static void main()
    {
        println(Convert.ToString(true));
        println(Convert.ToString(false));
        println(Convert.ToString(1));
        println(Convert.ToString(-1));
        println(Convert.ToString(0));
        println(Convert.ToString(1234567890));
        println("hello");
    }
}
