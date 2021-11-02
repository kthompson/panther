using System;
using static Panther.Predef;

public static string getName()
{
    println("What is your name?");
    return readLine();
}
