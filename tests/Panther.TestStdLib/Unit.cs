﻿namespace Panther;

public class Unit
{
    private Unit() { }

    public static readonly Unit Default = new Unit();

    public override string ToString()
    {
        return "unit";
    }
}
