using System;
using static Panther.Predef;

public static partial class Program
{
    public static void main()
    {
        var name = getName();
        println("Hello " + name + "!");
    }
}
