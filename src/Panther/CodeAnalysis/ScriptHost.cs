using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Mono.Cecil;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis;

public class ScriptHost : IDisposable
{
    private ImmutableArray<AssemblyDefinition> _references;
    private Compilation? _previous;
    private Dictionary<Symbol, FieldReference> _previousGlobals;
    private Dictionary<Symbol, MethodReference> _previousMethods;
    private readonly string _moduleName;
    private readonly string _hostPath;
    private int _id = 0;
    private readonly AssemblyLoadContext _loadContext;

    public ScriptHost(ImmutableArray<AssemblyDefinition> references, string moduleName)
        : this(references, null, moduleName)
    {
    }

    public ScriptHost(ImmutableArray<AssemblyDefinition> references, Compilation? previous, string moduleName)
    {
        _references = references;
        _previous = previous;
        _moduleName = moduleName;
        _previousGlobals = new Dictionary<Symbol, FieldReference>();
        _previousMethods = new Dictionary<Symbol, MethodReference>();
        var uuid = Guid.NewGuid().ToString();
        _loadContext = new AssemblyLoadContext(uuid, true);
        _hostPath = Path.Combine(Path.GetTempPath(), "Panther", "Execution", $"{moduleName}-{uuid}");
        if (!Directory.Exists(_hostPath))
            Directory.CreateDirectory(_hostPath);
    }

    public ExecutionResult Execute(params SyntaxTree[] syntaxTrees)
    {
        var compilation = Compile(syntaxTrees);

        return Execute(compilation);
    }

    public ExecutionResult Execute(Compilation compilation)
    {
        var outputPath = Path.Combine(_hostPath, $"script{_id}.dll");
        var emitResult = compilation.Emit($"{_moduleName}{_id}", outputPath, _previousGlobals, _previousMethods);
        if (emitResult.Diagnostics.Any())
            return new ExecutionResult(emitResult.Diagnostics, null);

        // successful emit so save it
        _previousGlobals = emitResult.Globals;
        _previousMethods = emitResult.Methods;
        _previous = compilation;
        if (emitResult.Assembly != null)
        {
            _references = _references.Add(emitResult.Assembly);
        }

        _id++;

        var asm = _loadContext.LoadFromAssemblyPath(outputPath);
        var method = asm.EntryPoint;
        var result = method?.Invoke(null, Array.Empty<object>());

        return new ExecutionResult(ImmutableArray<Diagnostic>.Empty, result);
    }

    public Compilation Compile(params SyntaxTree[] syntaxTrees)
    {
        return Compilation.CreateScript(_references, _previous, syntaxTrees);
    }

    public void Dispose()
    {
        var alcWeakRef = Unload(_loadContext);
        WaitForReferenceToDie(alcWeakRef);

        try
        {
            Directory.Delete(_hostPath, recursive: true);
        }
        catch
        {
            // ignored
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference Unload(AssemblyLoadContext context)
    {
        var alcWeakRef = new WeakReference(context, trackResurrection: true);
        context.Unload();
        return alcWeakRef;
    }

    private static void WaitForReferenceToDie(WeakReference reference)
    {
        for (var i = 0; reference.IsAlive && i < 10; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}