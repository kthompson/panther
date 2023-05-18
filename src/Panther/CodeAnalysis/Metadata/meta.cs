using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Threading;

namespace Panther.CodeAnalysis.Metadata;

// TODO: type specs

record ObjectData(
    StringTable Strings,
    TypeDefTable TypeDefs,
    MethodDefTable MethodDefs,
    FieldDefTable FieldDefs,
    ParamDefTable ParamDefs
);

enum TableType : byte
{
    String,
    TypeDef,
    MethodDef,
    ParamDef,
    FieldDef,
    FileDef,
}

class ObjectWriter : IDisposable
{
    private readonly BinaryWriter _writer;

    public ObjectWriter(Stream stream, bool leaveOpen = false)
    {
        _writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen);
    }

    public void WriteBoolean(bool value) => _writer.Write(value);

    public void WriteByte(byte value) => _writer.Write(value);

    public void WriteChar(char ch) => _writer.Write((ushort)ch);

    public void WriteDecimal(decimal value) => _writer.Write(value);

    public void WriteDouble(double value) => _writer.Write(value);

    public void WriteSingle(float value) => _writer.Write(value);

    public void WriteInt32(int value) => _writer.Write(value);

    public void WriteInt64(long value) => _writer.Write(value);

    public void WriteSByte(sbyte value) => _writer.Write(value);

    public void WriteInt16(short value) => _writer.Write(value);

    public void WriteUInt32(uint value) => _writer.Write(value);

    public void WriteUInt64(ulong value) => _writer.Write(value);

    public void WriteUInt16(ushort value) => _writer.Write(value);

    public void WriteToken<T>(T value) where T : Enum => WriteUInt16((ushort)(object)value);

    public void WriteString(string value) => _writer.Write(value);

    public void Dispose()
    {
        _writer.Dispose();
    }
}

interface IWritable
{
    void WriteTo(ObjectWriter writer);
}

interface Table<K, V> : IWritable
    where V : class
    where K : Enum
{
    public TableType TableType { get; }
    public V? Get(K index);
    public K Add(V item);
}

abstract record BaseTable<K, T>(TableType TableType) : Table<K, T>
    where T : class, IWritable
    where K : Enum
{
    private readonly List<T> _data = new List<T>();

    public T? Get(K token)
    {
        var index = (int)(object)token;
        if (index > _data.Count || index < 0)
            return null;
        return _data[index];
    }

    public K Current => (K)(object)_data.Count;

    public K Add(T item)
    {
        var index = _data.Count;
        _data.Add(item);
        return (K)(object)index;
    }

    public void WriteTo(ObjectWriter writer)
    {
        writer.WriteByte((byte)TableType);
        writer.WriteUInt16((ushort)_data.Count);
        foreach (var writable in _data)
            writable.WriteTo(writer);
    }
}

[Flags]
enum TypeDefFlags : byte
{
    None = 0,
}

[Flags]
enum MethodDefFlags : byte
{
    None = 0,
    EntryPoint = 1,
    Static = 2,
}

enum StringToken : ushort { }

enum TypeToken : ushort { }

enum MethodToken : ushort { }

enum ParamToken : ushort { }

enum FieldToken : ushort { }

record TypeDef(StringToken Name, TypeDefFlags Flags, FieldToken FieldList, MethodToken MethodList)
    : IWritable
{
    public void WriteTo(ObjectWriter writer)
    {
        writer.WriteToken(Name);
        writer.WriteByte((byte)Flags);
        writer.WriteToken(FieldList);
        writer.WriteToken(MethodList);
    }
}

record MethodDef(StringToken Name, MethodDefFlags Flags, ParamToken ParamList) : IWritable
{
    public void WriteTo(ObjectWriter writer)
    {
        writer.WriteToken(Name);
        writer.WriteByte((byte)Flags);
        writer.WriteToken(ParamList);
    }
}

/// <summary>
/// A param of a method
/// </summary>
/// <param name="Name">token to string heap</param>
/// <param name="Sequence">ordinal of parameter in method</param>
record ParamDef(StringToken Name, ushort Sequence) : IWritable
{
    public void WriteTo(ObjectWriter writer)
    {
        writer.WriteToken(Name);
        writer.WriteUInt16(Sequence);
    }
}

record FieldDef(StringToken Name, bool Static) : IWritable
{
    public void WriteTo(ObjectWriter writer)
    {
        writer.WriteToken(Name);
    }
}

record StringDef(string Value) : IWritable
{
    public void WriteTo(ObjectWriter writer)
    {
        writer.WriteString(Value);
    }
}

record StringTable() : BaseTable<StringToken, StringDef>(TableType.String);

record TypeDefTable() : BaseTable<TypeToken, TypeDef>(TableType.TypeDef);

record MethodDefTable() : BaseTable<MethodToken, MethodDef>(TableType.MethodDef);

record ParamDefTable() : BaseTable<ParamToken, ParamDef>(TableType.ParamDef);

record FieldDefTable() : BaseTable<FieldToken, FieldDef>(TableType.FieldDef);
