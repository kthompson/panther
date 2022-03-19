using System;
using System.IO;

namespace Panther.CodeAnalysis.Emit;

class VMProcessor : IDisposable
{
    private readonly StreamWriter _writer;

    public VMProcessor(string outputPath)
    {
        _writer = new StreamWriter(outputPath);
    }

    public void Dispose() => _writer.Dispose();

    public void EmitPush(Segment segment, int index)
    {
        _writer.Write("push ");
        _writer.Write(segment.ToString().ToLowerInvariant());
        _writer.Write(" ");
        _writer.Write(index);
    }

    public void EmitPop(Segment segment, int index)
    {
        _writer.Write("pop ");
        _writer.Write(segment.ToString().ToLowerInvariant());
        _writer.Write(" ");
        _writer.WriteLine(index);
    }

    public void EmitFunction(string name, int locals)
    {
        _writer.Write("function ");
        _writer.Write(name);
        _writer.Write(" ");
        _writer.WriteLine(locals);
    }

    public void EmitCall(string name, int arguments)
    {
        _writer.Write("call ");
        _writer.Write(name);
        _writer.Write(" ");
        _writer.WriteLine(arguments);
    }

    public void EmitReturn()
    {
        _writer.WriteLine("return");
        _writer.WriteLine();
    }

    public void EmitLabel(string label)
    {
        _writer.Write("label ");
        _writer.WriteLine(label);
    }

    public void EmitGotoLabel(string label)
    {
        _writer.Write("goto ");
        _writer.WriteLine(label);
    }

    public void EmitIfGotoLabel(string label)
    {
        _writer.Write("if-goto ");
        _writer.WriteLine(label);
    }

    public void EmitArithmetic(Arithmetic code)
    {
        _writer.WriteLine(code.ToString().ToLowerInvariant());
    }

    public void EmitComment(string comment)
    {
        _writer.WriteLine($"// {comment}");
    }
}