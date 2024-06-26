﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Mono.Cecil;
using Panther.CodeAnalysis.Emit;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;
using Panther.CodeAnalysis.Typing;
using Panther.IO;

namespace Panther.CodeAnalysis;

public class Compilation
{
    public bool IsScript { get; }
    public Compilation? Previous { get; }
    public ImmutableArray<SyntaxTree> SyntaxTrees { get; }
    public ImmutableArray<AssemblyDefinition> References { get; }
    public Symbol RootSymbol => TypedAssembly.RootSymbol;
    public ImmutableArray<Diagnostic> Diagnostics =>
        SyntaxTrees
            .SelectMany(tree => tree.Diagnostics)
            .Concat(TypedAssembly.Diagnostics)
            .ToImmutableArray();

    private TypedAssembly? _boundAssembly;
    private TypedAssembly TypedAssembly
    {
        get
        {
            if (_boundAssembly == null)
            {
                var bindAssembly = Typer.BindAssembly(
                    IsScript,
                    SyntaxTrees,
                    Previous?.TypedAssembly,
                    References
                );
                Interlocked.CompareExchange(ref _boundAssembly, bindAssembly, null);
            }

            return _boundAssembly!;
        }
    }

    private Compilation(
        ImmutableArray<AssemblyDefinition> references,
        bool isScript,
        Compilation? previous,
        params SyntaxTree[] syntaxTrees
    )
    {
        References = references;
        IsScript = isScript;
        Previous = previous;
        SyntaxTrees = syntaxTrees.ToImmutableArray();
    }

    public static (ImmutableArray<Diagnostic> diagnostics, Compilation? compilation) Create(
        string[] references,
        params SyntaxTree[] syntaxTrees
    )
    {
        var bag = new DiagnosticBag();
        var assemblies = ImmutableArray.CreateBuilder<AssemblyDefinition>();
        foreach (var reference in references)
        {
            try
            {
                var asm = AssemblyDefinition.ReadAssembly(reference);
                assemblies.Add(asm);
            }
            catch (BadImageFormatException)
            {
                bag.ReportInvalidReference(reference);
            }
        }

        if (bag.Any())
        {
            return (bag.ToImmutableArray(), null);
        }

        return (ImmutableArray<Diagnostic>.Empty, Create(assemblies.ToImmutable(), syntaxTrees));
    }

    public static Compilation Create(
        ImmutableArray<AssemblyDefinition> references,
        params SyntaxTree[] syntaxTrees
    ) => new Compilation(references, false, null, syntaxTrees);

    public static Compilation CreateScript(
        ImmutableArray<AssemblyDefinition> references,
        Compilation? previous,
        params SyntaxTree[] syntaxTrees
    ) => new Compilation(references, isScript: true, previous, syntaxTrees);

    public IEnumerable<Binder.Symbol> GetSymbols()
    {
        var compilation = this;
        var symbolNames = new HashSet<string>();

        while (compilation != null)
        {
            var (_, symbolTable) = Binder.Binder.Bind(compilation.SyntaxTrees);

            foreach (var type in symbolTable.Where(type => symbolNames.Add(type.FullName)))
            {
                yield return type;
                //
                // foreach (var member in type.Members)
                // {
                //     yield return member;
                // }
            }

            compilation = compilation.Previous;
        }
    }

    public void EmitTree(TextWriter writer)
    {
        var entryPoint = TypedAssembly.EntryPoint?.Symbol;
        var methods =
            from type in TypedAssembly.RootSymbol.Types
            from method in type.Methods
            where method != entryPoint
            let body = TypedAssembly.MethodDefinitions.GetValueOrDefault(method)
            where body != null
            select new { method, body };

        foreach (var function in methods)
        {
            EmitTree(function.method, function.body!, writer);
            writer.WriteLine();
        }

        if (entryPoint != null)
        {
            EmitTree(entryPoint, writer);
            writer.WriteLine();
        }
    }

    public void EmitTree(Symbol method, TextWriter writer)
    {
        var assembly = TypedAssembly;

        while (assembly != null)
        {
            if (assembly.MethodDefinitions.TryGetValue(method, out var block))
            {
                EmitTree(method, block, writer);

                return;
            }

            assembly = assembly.Previous;
        }

        method.WriteTo(writer);
    }

    private static void EmitTree(Symbol method, TypedBlockExpression block, TextWriter writer)
    {
        method.WriteTo(writer);
        writer.WritePunctuation(" = ");
        writer.WriteLine();
        block.WriteTo(writer);
    }

    public EmitResult Emit(string moduleName, string outputPath) =>
        Emitter.Emit(TypedAssembly, moduleName, outputPath);

    internal EmitResult Emit(
        string moduleName,
        string outputPath,
        Dictionary<Symbol, FieldReference> previousGlobals,
        Dictionary<Symbol, MethodReference> previousMethods
    ) => Emitter.Emit(TypedAssembly, moduleName, outputPath, previousGlobals, previousMethods);
}
