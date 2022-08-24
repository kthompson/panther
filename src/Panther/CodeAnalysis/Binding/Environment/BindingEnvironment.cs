using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Binding.Environment;

class Symbol : ISymbol
{
    public int Id { get; }
    public SymbolKind Kind { get; }

    public string Name { get; }

    internal Symbol(int id, SymbolKind kind, string name)
    {
        Id = id;
        Name = name;
        Kind = kind;
    }
}

class BindingEnvironment
{
    private readonly List<TypeRecord> _classes = new();
    private readonly Dictionary<string, int> _classLookup = new();

    private readonly List<FieldRecord> _fields = new();
    private readonly Dictionary<string, int> _fieldLookup = new();

    private readonly List<MethodRecord> _methods = new();
    private readonly Dictionary<string, ImmutableArray<int>> _methodLookup = new();
    private readonly Dictionary<int, string> _methodNameLookup = new();

    private readonly List<ParameterRecord> _parameters = new();

    private static string KeyOf(params string[] names) =>
        string.Join(".", names.Where(name => name != string.Empty));

    private static string KeyOf(TypeRecord typeRecord) =>
        KeyOf(typeRecord.Namespace, typeRecord.Name);

    private string KeyOf(int typeId, FieldRecord record)
    {
        var classRecord = _classes[typeId];
        var fullname = KeyOf(classRecord.Namespace, classRecord.Name, record.Name);
        return fullname;
    }

    public int AddClass(string ns, string name, int? baseClass)
    {
        var i = _classes.Count;
        var classRecord = new TypeRecord(ns, name, baseClass, null, _fields.Count, _methods.Count);
        _classes.Add(classRecord);
        _classLookup.Add(KeyOf(classRecord), i);
        return i;
    }

    public int? LookupClass(string ns, string name)
    {
        if (_classLookup.TryGetValue(KeyOf(ns, name), out var id))
            return id;

        return null;
    }

    // public TypeRecord GetClass(int typeId) => _classes[typeId];

    public int AddField(string name, bool isStatic)
    {
        var typeId = _classes.Count - 1;
        var i = _fields.Count;
        var record = new FieldRecord(name, isStatic);
        var fullname = KeyOf(typeId, record);

        _fields.Add(record);
        _fieldLookup.Add(fullname, i);
        return i;
    }

    public int AddMethod(string name, bool isStatic)
    {
        var typeId = _classes.Count - 1;
        var i = _methods.Count;
        var record = new MethodRecord(name, isStatic, _parameters.Count);
        var key = KeyOf(typeId, record);

        _methods.Add(record);

        if (_methodLookup.TryGetValue(key, out var methods))
        {
            _methodLookup[key] = methods.Add(i);
        }
        else
        {
            _methodLookup.Add(key, ImmutableArray.Create(i));
        }

        return i;
    }

    private string KeyOf(int typeId, MethodRecord record)
    {
        var classRecord = _classes[typeId];
        var sig = KeyOf(classRecord.Namespace, classRecord.Name, record.Name);
        return sig;
    }

    // public MethodRecord GetMethod(MethodId methodId) => _methods[methodId.Value];

    // public ImmutableArray<MethodId> LookupMethod(TypeId typeId, string name)
    // {
    //     var classRecord = _classes[typeId.Value];
    //     var sig = KeyOf(classRecord.Namespace, classRecord.Name, name);
    //
    //     return _methodLookup.TryGetValue(sig, out var methods)
    //         ? methods.Select(m => new MethodId(m)).ToImmutableArray()
    //         : new ImmutableArray<MethodId>();
    // }

    public int AddParameter(string name)
    {
        var i = _parameters.Count;
        var record = new ParameterRecord(name);
        _parameters.Add(record);
        return i;
    }

    public ImmutableArray<TypeRecord> GetClasses() => _classes.ToImmutableArray();
}
