using System;
using static Panther.Predef;

public static partial class Program
{
    public static string getName()
    {
        println("What is your name?");
        return readLine();
    }
}
