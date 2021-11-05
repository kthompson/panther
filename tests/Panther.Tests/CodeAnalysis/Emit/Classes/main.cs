using System;
using static Panther.Predef;

public static partial class Program
{
    public static void main()
    {
        var p = new Point(10, 20);
        println(p.X);
    }
}

class Point
{
    public Point(int X, int Y)
    {
        this.X = X;
        this.Y = Y;
    }

    public int X { get; }
    public int Y { get; }
}

class Extent
{
    public Extent(int xmin, int xmax, int ymin, int ymax)
    {
        this.xmin = xmin;
        this.xmax = xmax;
        this.ymin = ymin;
        this.ymax = ymax;
    }

    public int xmin { get; }
    public int xmax { get; }
    public int ymin { get; }
    public int ymax { get; }

    public int width()
    {
        return xmax - xmin;
    }

    public int height()
    {
        return ymax - ymin;
    }
}
