using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Binding
{
    record BindResult(
        ImmutableArray<Diagnostic> Diagnostics,
        ImmutableArray<SyntaxTree> Trees,
        Symbol RootSymbol
    );


    [Flags]
    enum ContainerFlags
    {
        None,
        IsContainer, // class/object
        HasLocals,
        IsBlock, // code block
    }

    class SymbolLinks
    {
        public SymbolLinks(Symbol symbol)
        {
            Symbol = symbol;
        }

        public List<SyntaxNode> Declarations { get; } = new List<SyntaxNode>();
        public Symbol Symbol { get; init; }
    }

    class NodeLinks
    {
        public NodeLinks(Symbol symbol)
        {
            Symbol = symbol;
        }

        public Symbol Symbol { get; set; }
    }

    class SymbolBinder : SyntaxVisitor<Symbol>
    {
        private Symbol _root;
        private DiagnosticBag _diagnostics;
        // private SyntaxNode _container;
        private Symbol _container;
        private int _paramIndex = 0;

        /// <summary>
        /// List of symbols for identity mapping
        /// </summary>
        private List<SymbolLinks> _symbols = new List<SymbolLinks>();
        private List<NodeLinks> _nodes = new List<NodeLinks>();

        private SymbolBinder()
        {
            _root = Symbol.NewRoot();
            _container = _root;
            _diagnostics = new DiagnosticBag();
        }

        public static BindResult Bind(params SyntaxTree[] trees) => Bind(trees.ToImmutableArray());

        public static BindResult Bind(ImmutableArray<SyntaxTree> trees)
        {
            var binder = new SymbolBinder();

            foreach (var tree in trees)
                binder.Bind(tree.Root);

            return new BindResult(
                binder._diagnostics.ToImmutableArray(),
                trees,
                binder._root
            );
        }

        private void Bind(SyntaxNode node)
        {
            // bind declaration nodes to a symbol
            // create the symbol and add to the appropriate symbol table
            // symbol tables include:
            // 1. members of the current container's symbol (_container.Members)
            // 2. locals of the current container

            // container may be several levels up in the syntax tree but it is
            // the first symbol above this node
            var saveContainerSymbol = _container;
            var symbol = node.Accept(this);
            if (symbol != Symbol.None)
            {
                _container = symbol;
            }

            // now bind children

            // tokens/trivia dont have children
            if (node is SyntaxToken or SyntaxTrivia)
                return;

            BindChildren(node);

            _container = saveContainerSymbol;
        }

        private void BindChildren(SyntaxNode node)
        {
            foreach (var child in node.GetChildren())
            {
                Bind(child);
            }
        }

        protected override Symbol DefaultVisit(SyntaxNode node) => Symbol.None;

        public override Symbol VisitClassDeclaration(ClassDeclarationSyntax node) =>
            DeclareSymbol(
                _container,
                node.Identifier,
                node,
                SymbolFlags.Class,
                SymbolFlags.ClassExcludes
            );

        public override Symbol VisitObjectDeclaration(ObjectDeclarationSyntax node) =>
            DeclareSymbol(
                _container,
                node.Identifier,
                node,
                SymbolFlags.Object,
                SymbolFlags.ObjectExcludes
            );

        public override Symbol VisitFunctionDeclaration(FunctionDeclarationSyntax node)
        {
            // TODO: check for duplicate function signatures in a separate pass
            _paramIndex = 0;
            return DeclareSymbol(
                _container,
                node.Identifier,
                node,
                SymbolFlags.Method,
                SymbolFlags.MethodExcludes
            );
        }

        public override Symbol VisitParameter(ParameterSyntax node)
        {
            _paramIndex++;
            DeclareSymbol(
                _container,
                node.Identifier,
                node,
                SymbolFlags.Parameter | SymbolFlags.Readonly,
                SymbolFlags.ParameterExcludes
            );

            // not a container symbol
            return Symbol.None;
        }

        public override Symbol VisitNamespaceDeclaration(NamespaceDeclarationSyntax node) =>
            DeclareSymbol(
                _container,
                node.Name,
                node,
                SymbolFlags.Namespace,
                SymbolFlags.NamespaceExcludes
            );

        public override Symbol VisitNestedNamespace(NestedNamespaceSyntax node) =>
            DeclareSymbol(
                _container,
                node.Name,
                node,
                SymbolFlags.Namespace,
                SymbolFlags.NamespaceExcludes
            );

        public override Symbol VisitVariableDeclarationStatement(VariableDeclarationStatementSyntax node)
        {
            DeclareSymbol(
                _container,
                node.IdentifierToken,
                node,
                SymbolFlags.Local | (node.ValOrVarToken.Text == "val" ? SymbolFlags.Readonly : SymbolFlags.None),
                SymbolFlags.LocalExcludes
            );

            // not a container symbol
            return Symbol.None;
        }


        Symbol DeclareSymbol(Symbol parent, SyntaxToken nameToken, SyntaxNode node, SymbolFlags includes, SymbolFlags excludes, int? index = null)
        {
            var name = nameToken.Text;
            var location = nameToken.Location;
            var symbol = parent.NewTerm(location, name, includes);

            if (index != null)
            {
                symbol.Index = index.Value;
            }

            // TODO: certain symbols types should actually be merged. for example namespaces
            var existingSymbols = _container.LookupMembers(name);
            if (existingSymbols.Any())
            {
                foreach (var existingSymbol in existingSymbols)
                {
                    if ((existingSymbol.Flags & excludes) != 0)
                    {
                        // // symbol isn't compatible for merging so lets create some diagnostics
                        // foreach (var declaration in symbol.Declarations)
                        // {
                        //     var errorNode = GetNameOfDeclaration(declaration) ?? declaration;
                        //     _diagnostics.ReportVariableAlreadyDefined(errorNode.Location, name);
                        // }

                        if (symbol.IsParameter)
                        {
                            _diagnostics.ReportDuplicateParameter(location, name);
                        }
                        else if (symbol.IsMember)
                        {
                            _diagnostics.ReportDuplicateDefinition(location, name);
                        }
                        else
                        {
                            _diagnostics.ReportVariableAlreadyDefined(location, name);
                        }


                        // update symbol flags so that we dont report more errors
                        existingSymbol.Flags = SymbolFlags.None;

                        return Symbol.None;
                    }
                }
            }

            AddDeclaration(symbol, node, includes);

            return symbol.Declare();
        }

        private void AddDeclaration(Symbol symbol, SyntaxNode node, SymbolFlags flags)
        {


            this._symbols.Add(new SymbolLinks(symbol)
            {
                Declarations = { node }
            });
        }

        private SyntaxToken? GetNameOfDeclaration(SyntaxNode node) =>
            node switch
            {
                ClassDeclarationSyntax classDeclarationSyntax => classDeclarationSyntax.Identifier,
                NamespaceDeclarationSyntax namespaceDirectiveSyntax => namespaceDirectiveSyntax.Name,
                NestedNamespaceSyntax nestedNamespaceSyntax => nestedNamespaceSyntax.Name,
                FunctionDeclarationSyntax functionDeclarationSyntax => functionDeclarationSyntax.Identifier,
                ObjectDeclarationSyntax objectDeclarationSyntax => objectDeclarationSyntax.Identifier,
                ParameterSyntax parameterSyntax => parameterSyntax.Identifier,
                VariableDeclarationStatementSyntax variableDeclarationStatementSyntax => variableDeclarationStatementSyntax.IdentifierToken,
                _ => null
            };

        private string? GetDeclarationName(SyntaxNode node) => GetNameOfDeclaration(node)?.Text;

        // Symbol? DeclareSymbolAndAddToSymbolTable(SyntaxNode node, SymbolFlags includes, SymbolFlags excludes)
        // {
        //     switch (_container)
        //     {
        //         case ClassDeclarationSyntax:
        //         case ObjectDeclarationSyntax:
        //         case NamespaceDeclarationSyntax:
        //         case CompilationUnitSyntax:
        //         case BlockExpressionSyntax:
        //         case FunctionDeclarationSyntax:
        //
        //             _container.Members ??= new SymbolTable();
        //             return DeclareSymbol(_containerSymbol.Members, _containerSymbol, node, includes, excludes);
        //
        //         // case CompilationUnitSyntax:
        //         // case BlockExpressionSyntax:
        //         // case FunctionDeclarationSyntax:
        //         //     _containerSymbol.Locals ??= new SymbolTable();
        //         //
        //         //     return DeclareSymbol(_containerSymbol.Locals, _containerSymbol, node, includes, excludes);
        //
        //         default:
        //             throw new ArgumentOutOfRangeException(nameof(_container));
        //     }
        // }
    }
}