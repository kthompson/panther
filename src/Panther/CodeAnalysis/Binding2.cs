using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Panther.CodeAnalysis.Syntax;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Binding2;


// enum TypeFlags
// {
//     Unknown,
//
//     Any,
//     Unit,
//     Nothing,
//     Bool,
//     Int,
//     String,
// }
//
// abstract class Type
// {
//     public TypeFlags Flags { get; init; }
//
// }
//
// class IntrinsicType : Type
// {
//     public IntrinsicType(string name)
//     {
//         this.Name = name;
//     }
//
//     public string Name { get; init; }
// }
//
// static class InternalSymbolNames
// {
//     public const string Missing = "__missing";
// }
//
// public class SymbolTable
// {
//     private readonly Dictionary<string, ImmutableArray<Symbol>> _symbolMap;
//     private readonly List<Symbol> _symbols;
//
//     public SymbolTable()
//     {
//         _symbolMap = new Dictionary<string, ImmutableArray<Symbol>>();
//         _symbols = new List<Symbol>();
//     }
//
//     public IReadOnlyList<Symbol> Symbols => _symbols;
//
//     public Symbol? GetSymbol(string name) =>
//         _symbolMap.TryGetValue(name, out var symbols) ? symbols.FirstOrDefault() : null;
//
//     public ImmutableArray<Symbol> GetSymbols(string name) =>
//         _symbolMap.TryGetValue(name, out var symbols) ? symbols : ImmutableArray<Symbol>.Empty;
//
//     public bool DefineSymbol(Symbol symbol)
//     {
//         // only one field symbol per name
//         if ((symbol.Flags & SymbolFlags.Field) != 0)
//         {
//             if (_symbolMap.ContainsKey(symbol.Name))
//                 return false;
//
//             _symbolMap.Add(symbol.Name, ImmutableArray.Create(symbol));
//             _symbols.Add(symbol);
//             return true;
//         }
//
//         if (_symbolMap.TryGetValue(symbol.Name, out var symbols))
//         {
//             _symbolMap[symbol.Name] = symbols.Add(symbol);
//             _symbols.Add(symbol);
//             return true;
//         }
//
//         _symbolMap.Add(symbol.Name, ImmutableArray.Create(symbol));
//         _symbols.Add(symbol);
//         return true;
//     }
//
// }
//
// [Flags]
// public enum SymbolFlags
// {
//     None = 0,
//
//     // Symbol type
//     Namespace = 1,
//     Object = 1 << 1,
//     Class = 1 << 2,
//     Method = 1 << 3,
//     Field = 1 << 4,
//     Property = 1 << 5,
//     Parameter = 1 << 6,
//     Local = 1 << 7,
//
//     // Access
//     Static = 1 << 8, // methods and fields
//     Readonly = 1 << 9,
// }
//
//
// public class Symbol
// {
//     public Symbol(SymbolFlags flags, string name)
//     {
//         Name = name;
//         Flags = flags;
//     }
//
//     public int Id { get; set; }
//     public string Name { get; }
//     public SymbolFlags Flags { get; }
//
//     /// <summary>
//     /// All child symbols
//     /// </summary>
//     public SymbolTable? Symbols { get; set; }
// }
//
// internal class SymbolLinks
// {
//     public List<SyntaxNode> Declarations { get; } = new List<SyntaxNode>();
//     public Type? Type { get; set; }
// }
//
// class BindingCompilationContext
// {
//
// }
//
// [Flags]
// enum ContainerFlags
// {
//     IsContainer,
//     HasLocals,
//     IsBlockContainer,
//     None
// }

// /// <summary>
// /// The Binder attaches Symbols to nodes in a syntax tree
// /// </summary>
// class NewBinder : SyntaxVisitor
// {
//     private readonly SyntaxTree _tree;
//     private SyntaxNode? _parent;
//     private SyntaxNode _container;
//     private int _symbolCount;
//     private readonly DiagnosticBag _diagnostics;
//
//     public NewBinder(SyntaxTree tree)
//     {
//         _tree = tree;
//         _container = _tree.Root;
//         _parent = null;
//         _diagnostics = _tree.DiagnosticBag;
//     }
//
//     private void SetContainer(SyntaxNode node)
//     {
//         throw new NotImplementedException();
//     }
//
//     // protected override void DefaultVisit(SyntaxNode node)
//     // {
//     //     var savedContainer = _currentContainer;
//     //
//     //     SetContainer(node);
//     //
//     //     foreach (var child in node.GetChildren())
//     //     {
//     //         child.Accept(this);
//     //     }
//     //
//     //     _currentContainer = savedContainer;
//     // }
//
//     // public override void VisitClassDeclaration(ClassDeclarationSyntax node)
//     // {
//     //     base.VisitClassDeclaration(node);
//     // }
//
//     private void BindSyntaxTree() => Bind(_tree.Root);
//
//     private ContainerFlags GetContainerFlags(SyntaxNode node)
//     {
//         switch (node)
//         {
//             case ClassDeclarationSyntax:
//             case ObjectDeclarationSyntax:
//                 return ContainerFlags.IsContainer;
//
//             case CompilationUnitSyntax:
//             case NamespaceDeclarationSyntax:
//             case NestedNamespaceSyntax:
//                 return ContainerFlags.IsContainer;
//
//             // case BlockExpressionSyntax:
//             //     return ContainerFlags.IsContainer | ContainerFlags.HasLocals;
//
//             case FunctionDeclarationSyntax:
//                 return ContainerFlags.IsContainer | ContainerFlags.HasLocals;
//
//             default:
//                 return ContainerFlags.None;
//         }
//     }
//
//     protected override void DefaultVisit(SyntaxNode node)
//     {
//     }
//
//     private void Bind(SyntaxNode node)
//     {
//         // if (_parent != null)
//         // {
//         //     node.Parent = _parent;
//         // }
//
//         // bind declaration nodes to a symbol
//         // create the symbol and add to the appropriate symbol table
//         // symbol tables include:
//         // 1. members of the current container's symbol (_container.Symbol.Members)
//         // 2. locals of the current container
//         node.Accept(this);
//
//         // now bind children
//
//         // tokens dont have children
//         if (node.Kind <= SyntaxKind.LastToken)
//             return;
//
//         var saveParent = _parent;
//         _parent = node;
//
//         var containerFlags = GetContainerFlags(node);
//         if (containerFlags == ContainerFlags.None)
//         {
//             BindChildren(node);
//         }
//         else
//         {
//             BindContainer(node, containerFlags);
//         }
//
//         _parent = saveParent;
//     }
//
//     private void BindContainer(SyntaxNode node, ContainerFlags containerFlags)
//     {
//         var saveContainer = _container;
//
//         if (containerFlags.HasFlag(ContainerFlags.IsContainer))
//         {
//             _container = node;
//         }
//
//         // may need to track containers but for now just call BindChildren
//         BindChildren(node);
//
//         _container = saveContainer;
//     }
//
//     private void BindChildren(SyntaxNode node)
//     {
//         foreach (var child in node.GetChildren())
//         {
//            Bind(child);
//         }
//     }
//
//     public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
//     {
//         DeclareSymbolAndAddToSymbolTable(node, SymbolFlags.Namespace);
//     }
//
//     public override void VisitNestedNamespace(NestedNamespaceSyntax node)
//     {
//         DeclareSymbolAndAddToSymbolTable(node, SymbolFlags.Namespace);
//     }
//
//     public override void VisitObjectDeclaration(ObjectDeclarationSyntax node)
//     {
//         DeclareSymbolAndAddToSymbolTable(node, SymbolFlags.Object);
//     }
//
//     public override void VisitClassDeclaration(ClassDeclarationSyntax node)
//     {
//         DeclareSymbolAndAddToSymbolTable(node, SymbolFlags.Class);
//     }
//
//     public override void VisitFunctionDeclaration(FunctionDeclarationSyntax node)
//     {
//         if (_container.Symbol!.Flags.HasFlag(SymbolFlags.Class))
//         {
//             // has a "this"
//             DeclareSymbolAndAddToSymbolTable(node, SymbolFlags.Method);
//         }
//         else
//         {
//             // top level function or function in an object
//             DeclareSymbolAndAddToSymbolTable(node, SymbolFlags.Method | SymbolFlags.Static);
//         }
//     }
//
//     public override void VisitVariableDeclarationStatement(VariableDeclarationStatementSyntax node)
//     {
//         DeclareSymbolAndAddToSymbolTable(node, SymbolFlags.Local);
//     }
//
//     public override void VisitParameter(ParameterSyntax node)
//     {
//         DeclareSymbolAndAddToSymbolTable(node, SymbolFlags.Parameter);
//     }
//
//     Symbol? DeclareSymbolAndAddToSymbolTable(SyntaxNode node, SymbolFlags symbolFlags)
//     {
//         switch (_container)
//         {
//             case ClassDeclarationSyntax:
//             case ObjectDeclarationSyntax:
//             case NamespaceDeclarationSyntax:
//                 _container.Symbol!.Members ??= new SymbolTable();
//                 return DeclareSymbol(_container.Symbol.Members, _container.Symbol, node, symbolFlags, symbolExcludes);
//
//             case CompilationUnitSyntax:
//             case BlockExpressionSyntax:
//             case FunctionDeclarationSyntax:
//                 _container.Locals ??= new SymbolTable();
//
//                 return DeclareSymbol(_container.Locals, _container.Symbol, node, symbolFlags, symbolExcludes);
//
//             default:
//                 throw new ArgumentOutOfRangeException(nameof(_container));
//         }
//     }
//
//     public static NewBinder Bind(SyntaxTree tree)
//     {
//         var binder = new NewBinder(tree);
//         binder.BindSyntaxTree();
//         return binder;
//     }
//
//     Symbol NewSymbol(SymbolFlags flags, string name)
//     {
//         _symbolCount++;
//         return new Symbol(name, flags);
//     }
//
//     Symbol DeclareSymbol(SymbolTable symbolTable, Symbol? parent, SyntaxNode node, SymbolFlags includes)
//     {
//         var name = GetDeclarationName(node);
//         Symbol? symbol = null;
//         if (name == null)
//         {
//             symbol = NewSymbol(SymbolFlags.None, InternalSymbolNames.Missing);
//         }
//         else
//         {
//            symbol = symbolTable.GetSymbol(name);
//
//            if (symbol == null)
//            {
//                symbol = NewSymbol(SymbolFlags.None, name);
//                symbolTable.DefineSymbol(symbol);
//            }
//            else // if ((symbol.Flags & excludes) != 0)
//            {
//                // symbol isn't compatible for merging so lets create some diagnostics
//                var ident = GetNameOfDeclaration(node) ?? node;
//                // TODO: _tree.DiagnosticBag.ReportVariableAlreadyDefined(ident.Location, name);
//
//                symbol = NewSymbol(SymbolFlags.Namespace, name);
//            }
//         }
//
//         AddDeclaration(symbol, node, includes);
//         // if (symbol.Parent != null)
//         // {
//         //     Debug.Assert(symbol.Parent == parent);
//         // }
//         // else
//         // {
//         //     symbol.Parent = parent;
//         // }
//
//         return symbol;
//     }
//
//     private SyntaxToken? GetNameOfDeclaration(SyntaxNode node) =>
//         node switch
//         {
//             ClassDeclarationSyntax classDeclarationSyntax => classDeclarationSyntax.Identifier,
//             NamespaceDeclarationSyntax namespaceDirectiveSyntax => namespaceDirectiveSyntax.Name,
//             NestedNamespaceSyntax nestedNamespaceSyntax => nestedNamespaceSyntax.Name,
//             FunctionDeclarationSyntax functionDeclarationSyntax => functionDeclarationSyntax.Identifier,
//             ObjectDeclarationSyntax objectDeclarationSyntax => objectDeclarationSyntax.Identifier,
//             ParameterSyntax parameterSyntax => parameterSyntax.Identifier,
//             VariableDeclarationStatementSyntax variableDeclarationStatementSyntax => variableDeclarationStatementSyntax.IdentifierToken,
//             _ => null
//         };
//
//     private string? GetDeclarationName(SyntaxNode node) => GetNameOfDeclaration(node)?.Text;
//
//     void AddDeclaration(Symbol symbol, SyntaxNode node, SymbolFlags flags)
//     {
//         symbol.Flags |= flags;
//         node.Symbol = symbol;
//
//         if ((flags & SymbolFlags.Class) != SymbolFlags.None && symbol.Members == null)
//         {
//             symbol.Members = new SymbolTable();
//         }
//
//         if ((flags & SymbolFlags.Value) != SymbolFlags.None && symbol.ValueNode == null)
//         {
//             symbol.ValueNode = node;
//         }
//
//         symbol.Declarations = symbol.Declarations.Add(node);
//     }
// }