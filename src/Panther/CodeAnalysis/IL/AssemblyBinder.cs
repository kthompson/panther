using System.Collections.Generic;
using System.Collections.Immutable;
using Panther.CodeAnalysis.Metadata;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.IL;

class AssemblyBinder
{
    private DiagnosticBag _diagnostics = new DiagnosticBag();

    private ObjectData _metadata = new ObjectData(
        new StringTable(),
        new TypeDefTable(),
        new MethodDefTable(),
        new FieldDefTable(),
        new ParamDefTable()
    );

    private Dictionary<string, TypeToken> _classNameToToken = new Dictionary<string, TypeToken>();
    private readonly Dictionary<string, MethodToken> _methodToToken =
        new Dictionary<string, MethodToken>();
    private readonly Dictionary<string, FieldToken> _fieldToToken =
        new Dictionary<string, FieldToken>();

    private string _currentClass = "";
    private MethodToken? _entryPoint = null;

    private AssemblyBinder() { }

    public static Assembly Create(AssemblyListingSyntax syntax)
    {
        var binder = new AssemblyBinder();
        var assembly = binder.BindAssembly(syntax);
        return assembly;
    }

    private Assembly BindAssembly(AssemblyListingSyntax syntax)
    {
        var builder = ImmutableArray.CreateBuilder<AssemblyClassDeclaration>();
        foreach (var classDeclaration in syntax.ClassDeclarations)
        {
            BindClass(classDeclaration);
        }

        return new Assembly(_diagnostics.ToImmutableArray(), builder.ToImmutable());
    }

    private void BindClass(AssemblyClassDeclarationSyntax syntax)
    {
        _currentClass = syntax.Name.Text;

        var nameToken = BindString(_currentClass);

        _metadata.TypeDefs.Add(
            new TypeDef(
                nameToken,
                TypeDefFlags.None,
                _metadata.FieldDefs.Current,
                _metadata.MethodDefs.Current
            )
        );

        foreach (var field in syntax.FieldDeclarations)
            BindField(field);

        foreach (var method in syntax.MethodDeclarations)
            BindMethod(method);
    }

    private void BindField(AssemblyFieldDeclarationSyntax field)
    {
        var name = field.Name.Text;
        var nameToken = BindString(name);
        var token = _metadata.FieldDefs.Add(new FieldDef(nameToken, field.StaticToken != null));
        _fieldToToken.Add(SymbolName(name), token);
    }

    private string SymbolName(string name) => $"{_currentClass}.{name}";

    private StringToken BindString(string text) => _metadata.Strings.Add(new StringDef(text));

    private int BindNumber(SyntaxToken syntax)
    {
        if (syntax.Value is int value)
            return value;

        _diagnostics.ReportInvalidNumber(syntax.Location, syntax.Text, "number");

        return 0;
    }

    private void BindMethod(AssemblyMethodDeclarationSyntax syntax)
    {
        var name = syntax.Name.Text;
        var nameToken = BindString(name);
        var entrypoint =
            syntax.EntryPointToken == null ? MethodDefFlags.None : MethodDefFlags.EntryPoint;

        var paramToken = _metadata.ParamDefs.Current;
        var method = _metadata.MethodDefs.Add(new MethodDef(nameToken, entrypoint, paramToken));

        BindParams(syntax.Parameters);

        if (syntax.EntryPointToken != null)
        {
            if (_entryPoint.HasValue)
            {
                _diagnostics.MultipleEntryPoints(syntax.Name.Location);
            }
            _entryPoint = method;
        }
        _methodToToken.Add(SymbolName(name), method);
    }

    private void BindParams(IReadOnlyList<ParameterSyntax> parameters)
    {
        for (var i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];
            var name = BindString(parameter.Identifier.Text);
            // TODO bind type
            _metadata.ParamDefs.Add(new ParamDef(name, (ushort)i));
        }
    }
}