using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Panther.CodeAnalysis.Syntax;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Binder;

public class Binder : SyntaxVisitor
{
    private readonly DiagnosticBag _diagnostics;
    private Symbol _symbolTable;

    private Binder(Symbol symbolTable, DiagnosticBag diagnostics)
    {
        _diagnostics = diagnostics;
        _symbolTable = symbolTable;
    }

    public static (ImmutableArray<Diagnostic> diagnostics, Symbol symbolTable) Bind(
        ImmutableArray<SyntaxTree> syntaxTrees
    ) => Bind(Symbol.NewRoot(), syntaxTrees);

    public static (ImmutableArray<Diagnostic> diagnostics, Symbol symbolTable) Bind(
        Symbol symbolTable,
        ImmutableArray<SyntaxTree> syntaxTrees
    )
    {
        var diagnostics = new DiagnosticBag();

        foreach (var tree in syntaxTrees)
        {
            var binder = new Binder(symbolTable, diagnostics);
            binder.Visit(tree.Root);
        }

        return (diagnostics.ToImmutableArray(), symbolTable);
    }

    protected override void DefaultVisit(SyntaxNode node)
    {
        foreach (
            var child in node.GetChildren().Where(child => child.Kind > SyntaxKind.ExpressionMarker)
        )
            Visit(child);
    }

    private void VisitContainer(SyntaxNode node, SymbolFlags symbolFlags, SyntaxToken identifier)
    {
        VisitContainer(node, symbolFlags, identifier.Text, identifier.Location);
    }

    private void VisitContainer(
        SyntaxNode node,
        SymbolFlags symbolFlags,
        string name,
        TextLocation location
    )
    {
        var (scope, existing) = _symbolTable.DeclareSymbol(name, symbolFlags, location);
        if (existing)
        {
            _diagnostics.ReportDuplicateSymbol(name, scope.Location, location);
        }

        var current = _symbolTable;

        _symbolTable = scope;

        this.DefaultVisit(node);

        _symbolTable = current;
    }

    private void DeclareSymbol(SyntaxToken identifier, SymbolFlags symbolFlags)
    {
        var name = identifier.Text;
        var location = identifier.Location;
        var existing = _symbolTable.Lookup(name);
        if (existing is not null)
        {
            _diagnostics.ReportDuplicateSymbol(name, existing.Location, location);
        }

        _symbolTable.DeclareSymbol(name, symbolFlags, location);
    }

    public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
    {
        VisitContainer(node, SymbolFlags.Namespace, node.Name.ToText(), node.Name.Location);
    }

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        VisitContainer(node, SymbolFlags.Class, node.Identifier);
    }

    public override void VisitObjectDeclaration(ObjectDeclarationSyntax node)
    {
        VisitContainer(node, SymbolFlags.Object, node.Identifier);
    }

    public override void VisitFunctionDeclaration(FunctionDeclarationSyntax node)
    {
        VisitContainer(node, SymbolFlags.Method, node.Identifier);
    }

    public override void VisitParameter(ParameterSyntax node)
    {
        var flags = _symbolTable.Flags.HasFlag(SymbolFlags.Class)
            ? SymbolFlags.Field
            : SymbolFlags.Parameter;

        DeclareSymbol(node.Identifier, flags);
    }

    public override void VisitVariableDeclarationStatement(VariableDeclarationStatementSyntax node)
    {
        var flags =
            _symbolTable.Flags.HasFlag(SymbolFlags.Class)
            || _symbolTable.Flags.HasFlag(SymbolFlags.Object)
                ? SymbolFlags.Field
                : SymbolFlags.Local;

        // TODO: need a way to deal with block scope for variables (or maybe we just don't have block scope for now)
        DeclareSymbol(node.IdentifierToken, flags);
    }
}
