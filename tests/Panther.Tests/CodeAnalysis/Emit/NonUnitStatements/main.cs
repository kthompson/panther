using System;
using static Panther.Predef;

public static int doThing(int value)
{
    println(Convert.ToString(value));
    return value - 12;
}

public static void main()
{
    doThing(1);
    doThing(2);
}
