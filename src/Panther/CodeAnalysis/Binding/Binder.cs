using System;
using System.Collections.Immutable;
using System.Linq;
using Mono.Cecil;
using Panther.CodeAnalysis.Binding.Environment;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;
using Symbol = Panther.CodeAnalysis.Binding.Environment.Symbol;

namespace Panther.CodeAnalysis.Binding;

class BindingContext
{
    public BindingEnvironment Environment { get; } = new BindingEnvironment();
}

class Binder : SyntaxVisitor
{
    private readonly BindingEnvironment _environment;

    private Binder(ImmutableArray<AssemblyDefinition> references)
    {
        _environment = new BindingEnvironment();
        _environment.AddClass("", "<none>", null);

        BindTypes(references, BindType);
        BindTypes(references, BindFields);
        BindTypes(references, BindMethods);
    }

    public static ImmutableArray<Symbol> Bind(ImmutableArray<AssemblyDefinition> references)
    {
        var binder = new Binder(references);
        var builder = ImmutableArray.CreateBuilder<Symbol>();
        var id = 0;

        foreach (var cls in binder._environment.GetClasses())
        {
            builder.Add(new Symbol(id++, SymbolKind.Class, $"{cls.Namespace}.{cls.Name}"));
        }

        return builder.ToImmutable();
    }

    private void BindType(TypeDefinition type)
    {
        if (!type.IsPublic)
            return;

        // TODO: support base classes
        _environment.AddClass(type.Namespace, type.Name, null);
    }

    private void BindFields(TypeDefinition type)
    {
        foreach (var fieldDefinition in type.Fields)
            BindField(fieldDefinition);
    }

    private void BindField(FieldDefinition field)
    {
        if (!field.IsPublic)
            return;

        _environment.AddField(field.Name, field.IsStatic);
    }

    private void BindMethods(TypeDefinition type)
    {
        foreach (var methodDefinition in type.Methods)
            BindMethod(methodDefinition);
    }

    private void BindMethod(MethodDefinition method)
    {
        if (!method.IsPublic)
            return;

        _environment.AddMethod(method.Name, method.IsStatic);

        foreach (var parameter in method.Parameters)
            BindParameter(parameter);
    }

    private void BindParameter(ParameterDefinition parameter)
    {
        _environment.AddParameter(parameter.Name);
    }

    private void BindTypes(
        ImmutableArray<AssemblyDefinition> references,
        Action<TypeDefinition> action
    )
    {
        // TODO: rearrange classes based on type hierarchy
        foreach (var reference in references)
            BindTypes(reference, action);
    }

    private void BindTypes(AssemblyDefinition reference, Action<TypeDefinition> action)
    {
        foreach (var module in reference.Modules)
        {
            BindTypes(module, action);
        }
    }

    private void BindTypes(ModuleDefinition module, Action<TypeDefinition> action)
    {
        foreach (var typeDefinition in module.Types)
        {
            action(typeDefinition);
        }
    }
}
