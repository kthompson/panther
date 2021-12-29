using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Panther.CodeAnalysis.Syntax;
using Panther.CodeAnalysis.Text;
using CASyntaxTree = Microsoft.CodeAnalysis.SyntaxTree;
using SyntaxFacts = Panther.CodeAnalysis.Syntax.SyntaxFacts;
using SyntaxKind = Panther.CodeAnalysis.Syntax.SyntaxKind;
using SyntaxNode = Panther.CodeAnalysis.Syntax.SyntaxNode;
using SyntaxToken = Panther.CodeAnalysis.Syntax.SyntaxToken;
using SyntaxTree = Panther.CodeAnalysis.Syntax.SyntaxTree;
using SyntaxTrivia = Panther.CodeAnalysis.Syntax.SyntaxTrivia;

namespace Panther.CodeAnalysis.CSharp
{
    enum ContainerType
    {
        None,
        Object,
        Class,
    }

    internal class CSharpEmitter : SyntaxVisitor, IDisposable
    {
        private readonly SyntaxTree _syntaxTree;
        private readonly StringWriter _stream;
        private readonly IndentedTextWriter _writer;
        private readonly DiagnosticBag _diagnostics;
        private ContainerType _containerType = ContainerType.None;
        private string _containerName = "";

        private static readonly IEnumerable<string> DefaultNamespaces =
            new[]
            {
                "System",
                "System.IO",
                "System.Net",
                "System.Linq",
                "System.Text",
                "Panther",
            };

        private static readonly CSharpCompilationOptions DefaultCompilationOptions =
            new CSharpCompilationOptions(OutputKind.ConsoleApplication)
                .WithOverflowChecks(true)
                .WithPlatform(Platform.X64)
                .WithOptimizationLevel(OptimizationLevel.Release)
                .WithSpecificDiagnosticOptions(
                    new Dictionary<string, ReportDiagnostic>
                    {
                        ["CS8019"] = ReportDiagnostic.Suppress // Unnecessary using directive
                    }
                )
                .WithUsings(DefaultNamespaces);

        private static readonly IEnumerable<MetadataReference> DefaultReferences =
            new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Panther.Unit).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.GenericUriParser).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException).Assembly
                    .Location)
            };


        public static string ToCSharpText(SyntaxTree tree)
        {
            using var emitter = new CSharpEmitter(tree);
            emitter.Visit(tree.Root);
            return emitter.ToText();
        }

        public static (bool Success, ImmutableArray<Diagnostic>) ToCSharp(string moduleName, string outputPath, params SyntaxTree[] trees)
        {
            var sourceFileLookup = trees.Select(tree => tree.File).ToDictionary(file => file.FileName);

            var list = new List<CASyntaxTree>();
            foreach (var tree in trees)
            {
                using var emitter = new CSharpEmitter(tree);
                emitter.Visit(tree.Root);
                list.Add(emitter.BuildSyntaxTree());
            }

            var compilation = CSharpCompilation.Create(moduleName, list.ToArray(), DefaultReferences, DefaultCompilationOptions.WithMainTypeName("Program"))
                ;

            using var stream = new MemoryStream();
            var result = compilation.Emit(stream);

            if (result.Success)
            {
                File.WriteAllBytes(outputPath, stream.GetBuffer());
            }

            return (result.Success, result.Diagnostics.SelectMany(diag =>
            {
                if (diag.WarningLevel == 4)
                    return Array.Empty<Diagnostic>();

                var diagLocation = diag.Location;
                // var diagSpan = diagLocation.SourceSpan;
                var path = diagLocation.SourceTree?.FilePath;

                var sourceFile = path != null && sourceFileLookup.TryGetValue(path, out var file) ? file : SourceFile.Empty;


                var location = new TextLocation(sourceFile, new TextSpan(0, 0));

                return new[] { new Diagnostic(location, diag.GetMessage()) };
            }).ToImmutableArray());
        }


        private CSharpEmitter(SyntaxTree syntaxTree)
        {
            _syntaxTree = syntaxTree;
            _stream = new StringWriter();
            _writer = new IndentedTextWriter(_stream);
            _diagnostics = new DiagnosticBag();
            _diagnostics.AddRange(syntaxTree.Diagnostics);
        }

        private string ToText()
        {
            _writer.Flush();
            var result = _stream.ToString();
            return result;
        }

        private CASyntaxTree BuildSyntaxTree()
        {
            _writer.Flush();
            var result = _stream.ToString();
            return CSharpSyntaxTree.ParseText(result, null, _syntaxTree.File.FileName);
        }

        protected override void DefaultVisit(SyntaxNode node)
        {
            throw new NotImplementedException();
        }

        void VisitChildren(SyntaxNode node)
        {
            foreach (var child in node.GetChildren())
            {
                child.Accept(this);
            }
        }

        public override void VisitTrivia(SyntaxTrivia node)
        {
        }

        public override void VisitToken(SyntaxToken node)
        {
        }

        public override void VisitUnitExpression(UnitExpressionSyntax node)
        {
        }


        public override void VisitGlobalStatement(GlobalStatementSyntax node)
        {
            VisitChildren(node);
        }

        public override void VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            switch (node.Value)
            {
                case int i:
                    _writer.Write(i.ToString());
                    break;
                case string s:
                    _writer.Write(node.LiteralToken.Text);
                    break;
                case bool b:
                    _writer.Write(b ? "true": "false");
                    break;
                default:
                    throw new InvalidOperationException("Unknown type: " + (node.Value?.GetType().Name ?? "null"));
            }
        }

        public override void VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            VisitChildren(node);
            _writer.WriteLine(";");
        }

        public override void VisitCallExpression(CallExpressionSyntax node)
        {
            if (node.Arguments.Count == 1 && node.Expression is IdentifierNameSyntax ident && _builtinTypes.Contains(ident.Identifier.Text))
            {
                WriteConversionExpression(ident, node.Arguments.First());
            }
            else
            {
                node.Expression.Accept(this);
                _writer.Write("(");
                VisitSeparatedCommas(node.Arguments);
                _writer.Write(")");
            }
        }

        private void WriteConversionExpression(IdentifierNameSyntax ident, ExpressionSyntax argument)
        {
            var targetType = ident.Identifier.Text;
            switch (targetType)
            {
                case "any":
                    _writer.Write("(object)(");
                    argument.Accept(this);
                    _writer.Write(")");
                    break;

                case "int":
                    _writer.Write("Convert.ToInt32(");
                    argument.Accept(this);
                    _writer.Write(")");
                    break;

                case "bool":
                    _writer.Write("Convert.ToBoolean(");
                    argument.Accept(this);
                    _writer.Write(")");
                    break;

                case "string":
                    _writer.Write("Convert.ToString(");
                    argument.Accept(this);
                    _writer.Write(")");
                    break;

                case "unit":
                    break;

                default:
                    throw new InvalidOperationException("Invalid conversion target: " + targetType);
            }
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            _writer.WriteLine($"class {node.Identifier.Text}");
            StartBlock();

            // build primary constructor

            _writer.Write($"public {node.Identifier.Text}(");
            VisitSeparatedCommas(node.Fields);
            _writer.WriteLine(")");
            StartBlock();
            foreach (var parameter in node.Fields)
            {
                var p = parameter.Identifier.Text;
                _writer.WriteLine($"this.{p} = {p};");
            }
            EndBlock();

            if(node.Fields.Count > 0)
            {
                WriteLine();
            }

            // emit property fields
            foreach (var parameter in node.Fields)
            {
                var p = parameter.Identifier.Text;
                _writer.Write("public ");
                parameter.TypeAnnotation.Accept(this);
                _writer.WriteLine($" {p} {{ get; }}");
            }

            if (node.Fields.Count > 0 && node.Template != null)
            {
                WriteLine();
            }

            // emit body if any exists

            using var _ = MarkContainer(node.Identifier.Text, ContainerType.Class);
            node.Template?.Accept(this);

            // close class
            EndBlock();
        }

        public override void VisitTemplate(TemplateSyntax node)
        {
            VisitMembers(node.Members);
        }

        private void VisitMembers(ImmutableArray<MemberSyntax> members)
        {
            var decls = members
                .Where(member => member is not GlobalStatementSyntax)
                .Where(member => member is not FunctionDeclarationSyntax)
                .ToImmutableArray();

            var funcs = members.OfType<FunctionDeclarationSyntax>().Where(func => func.Identifier.Text != "main")
                .ToImmutableArray();

            var main = members.OfType<FunctionDeclarationSyntax>()
                .FirstOrDefault(func => func.Identifier.Text == "main");

            var statements = members.OfType<GlobalStatementSyntax>().Select(global => global.Statement)
                .ToImmutableArray();

            var emitStatic = _containerType is ContainerType.None or ContainerType.Object;
            var emitMainOrStatic = !statements.IsEmpty || main != null;
            var emitProgram = (emitMainOrStatic || !funcs.IsEmpty) && _containerType == ContainerType.None;

            if (emitProgram)
            {
                _writer.WriteLine("public static partial class Program");
                StartBlock();
            }

            if (emitMainOrStatic)
            {
                // for top level statements we need to iterate twice
                // 1. defining top-level fields for this class,

                foreach (var assignment in statements.OfType<VariableDeclarationStatementSyntax>())
                {
                    _writer.Write("public ");
                    if(emitStatic)
                    {
                        _writer.Write("static ");
                    }

                    assignment.TypeAnnotation!.Type.Accept(this);

                    _writer.Write(" ");
                    _writer.Write(assignment.IdentifierToken.Text);
                    _writer.WriteLine(";");
                    // we emit the value later in case the expression is too complex for a field initializer
                }

                // 2. defining the entry point expressions(or static constructor)
                _writer.WriteLine(emitProgram || main != null ? "public static void main()" : $"static {_containerName}");
                StartBlock();

                foreach (var statement in statements)
                {
                    if (statement is VariableDeclarationStatementSyntax varDecl && varDecl.Initializer != null)
                    {
                        // create a temporary assignment expression and emit that
                        var assignment = new AssignmentExpressionSyntax(varDecl.SyntaxTree,
                            new IdentifierNameSyntax(varDecl.SyntaxTree, varDecl.IdentifierToken), varDecl.Initializer.EqualsToken,
                            varDecl.Initializer.Expression);

                        assignment.Accept(this);
                        _writer.WriteLine(";");
                    }
                    else
                    {
                        statement.Accept(this);
                    }
                }

                // TODO: if topLevelStatements == false this is probably wrong
                // emit the actual main body if there is one, globals will already be assigned
                if (main != null && main.Body.Kind != SyntaxKind.UnitExpression)
                {
                    main.Body.Accept(this);
                }

                // end constructor/main
                EndBlock();

                if (!funcs.IsEmpty)
                {
                    WriteLine();
                }
            }

            VisitSeparated(funcs, WriteLine);

            if (emitProgram)
            {
                EndBlock();
                if (!decls.IsEmpty)
                {
                    WriteLine();
                }
            }

            VisitSeparated(decls, WriteLine);
        }


        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            _writer.Write(node.Identifier.Text);
        }

        public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            node.Expression.Accept(this);
            _writer.Write(".");
            node.Name.Accept(this);
        }

        public override void VisitNewExpression(NewExpressionSyntax node)
        {
            _writer.Write("new ");
            node.Type.Accept(this);
            _writer.Write("(");
            VisitSeparatedCommas(node.Arguments);
            _writer.Write(")");
        }

        public override void VisitVariableDeclarationStatement(VariableDeclarationStatementSyntax node)
        {
            if (node.Initializer == null)
            {
                // int i;
                if (node.TypeAnnotation != null)
                {
                    node.TypeAnnotation.Type.Accept(this);
                    _writer.Write(" ");
                    _writer.Write(node.IdentifierToken.Text);
                    _writer.WriteLine(";");
                }
                else
                {
                    _writer.Write("var ");
                    _writer.Write(node.IdentifierToken.Text);
                    _writer.WriteLine(" = <error> ;");
                }
            }
            else
            {
                // var i = 0;
                _writer.Write("var ");
                _writer.Write(node.IdentifierToken.Text);
                _writer.Write(" = ");
                node.Initializer.Expression.Accept(this);
                _writer.WriteLine(";");
            }
        }

        public override void VisitObjectDeclaration(ObjectDeclarationSyntax node)
        {
            _writer.WriteLine($"public static partial class {node.Identifier.Text}");
            using var _ = MarkContainer(node.Identifier.Text, ContainerType.Object);
            StartBlock();
            node.Template.Accept(this);
            EndBlock();
        }

        public override void VisitFunctionDeclaration(FunctionDeclarationSyntax node)
        {
            _writer.Write("public ");

            if (_containerType is ContainerType.Object or ContainerType.None)
            {
                _writer.Write("static ");
            }

            bool isUnit;
            if (node.TypeAnnotation == null)
            {
                // _diagnostics.ReportTypeAnnotationRequired(node.Identifier.Location);
                // for now lets assume its unit/void
                isUnit = true;
                _writer.Write("void");
            }
            else
            {
                node.TypeAnnotation.Accept(this);
                isUnit = node.TypeAnnotation.Type is IdentifierNameSyntax ident && ident.Identifier.Text == "unit";
            }
            _writer.Write($" {node.Identifier.Text}(");

            VisitSeparatedCommas(node.Parameters);

            _writer.WriteLine(")");
            StartBlock();
            // flatten
            var (statements, expression) = ExpressionFlattener.Flatten(node.Body);
            foreach (var statement in statements)
            {
                statement.Accept(this);
            }

            if (!isUnit)
            {
                _writer.Write("return ");
                expression.Accept(this);
                _writer.WriteLine(";");
            }
            else if(expression.Kind != SyntaxKind.UnitExpression)
            {
                expression.Accept(this);
                _writer.WriteLine(";");
            }

            EndBlock();
        }

        private void EndBlock()
        {
            _writer.Indent--;
            _writer.WriteLine("}");
        }

        private void StartBlock()
        {
            _writer.WriteLine("{");
            _writer.Indent++;
        }

        public override void VisitTypeAnnotation(TypeAnnotationSyntax node)
        {
            if (node.Type is IdentifierNameSyntax identifier)
            {
                _writer.Write(TypeToCSharpType(identifier.Identifier.Text));
            }
            else
            {
                node.Type.Accept(this);
            }
        }

        public override void VisitWhileExpression(WhileExpressionSyntax node)
        {
            _writer.Write("while (");
            node.ConditionExpression.Accept(this);
            _writer.WriteLine(")");
            EmitBlock(node.Body);
        }

        void EmitBlock(ExpressionSyntax node)
        {
            if (node.Kind == SyntaxKind.BlockExpression)
            {
                StartBlock();
                node.Accept(this);
                EndBlock();
            }
            else
            {
                node.Accept(this);
            }
        }

        public override void VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            var op = SyntaxFacts.GetText(node.OperatorToken.Kind)??
                     throw new Exception("Invalid operator");
            var precedence = node.OperatorToken.Kind.GetBinaryOperatorPrecedence() ??
                             throw new Exception("Invalid operator");

            WriteNestedExpression(node.Left, precedence);
            _writer.Write(" ");
            _writer.Write(op);
            _writer.Write(" ");
            WriteNestedExpression(node.Right, precedence);
        }

        public override void VisitUnaryExpression(UnaryExpressionSyntax node)
        {
            var op = SyntaxFacts.GetText(node.OperatorToken.Kind)??
                     throw new Exception("Invalid operator");
            _writer.Write(op);

            WriteNestedExpression(node.Operand, OperatorPrecedence.Prefix);
        }

        public override void VisitParameter(ParameterSyntax node)
        {
            node.TypeAnnotation.Accept(this);
            _writer.Write($" {node.Identifier.Text}");
        }

        private void WriteNestedExpression(ExpressionSyntax node, OperatorPrecedence parentPrecedence)
        {
            if (node is BinaryExpressionSyntax binaryExpression)
            {
                var precedence = binaryExpression.OperatorToken.Kind.GetBinaryOperatorPrecedence() ??
                                 throw new Exception("Invalid operator");

                WriteNestedExpression(binaryExpression, parentPrecedence, precedence);
            }
            else
            {
                node.Accept(this);
            }
        }

        private void WriteNestedExpression(BinaryExpressionSyntax node, OperatorPrecedence parent, OperatorPrecedence current)
        {
            if (parent > current)
            {
                _writer.Write("(");
                node.Accept(this);
                _writer.Write(")");
            }
            else
            {
                node.Accept(this);
            }
        }

        public override void VisitBlockExpression(BlockExpressionSyntax node)
        {
            // StartBlock();

            VisitSeparated(node.Statements, () => { });

            if (node.Expression.Kind != SyntaxKind.UnitExpression)
            {
                node.Expression.Accept(this);
                _writer.WriteLine(";");
            }

            // EndBlock();
        }

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            node.Name.Accept(this);
            _writer.Write(" = ");
            node.Expression.Accept(this);
        }

        public override void VisitIfExpression(IfExpressionSyntax node)
        {
            // TODO: we need to remove blocks from if-expressions because they cannot be ternary expressions
            _writer.Write("if (");
            node.ConditionExpression.Accept(this);
            _writer.WriteLine(")");
            EmitBlock(node.ThenExpression);

            if (node.ThenExpression is not BlockExpressionSyntax)
            {
                _writer.Write(" ");
            }

            if (node.ElseExpression is IfExpressionSyntax)
            {
                _writer.Write("else ");
            }
            else
            {
                _writer.WriteLine("else");
            }

            EmitBlock(node.ElseExpression);
        }

        public override void VisitCompilationUnit(CompilationUnitSyntax node)
        {
            _writer.WriteLine("using System;");
            _writer.WriteLine("using static Panther.Predef;");
            WriteLine();

            foreach (var usingDirective in node.Usings)
            {
                usingDirective.Accept(this);
            }

            VisitMembers(node.Members);
        }

        private void WriteLine()
        {
            _writer.WriteLineNoTabs("");
        }

        private static HashSet<string> _builtinTypes = new()
        {
            "any",
            "int",
            "bool",
            "string",
            "unit",
        };

        string TypeToCSharpType(string type) =>
            type switch
            {
                "any" => "object",
                "unit" => "void",
                _ => type
            };

        void VisitSeparatedCommas<T>(IEnumerable<T> enumerable)
            where T : SyntaxNode
        {
            VisitSeparated(enumerable, () => _writer.Write(", "));
        }

        void VisitSeparated<T>(IEnumerable<T> enumerable, Action betweenNodes)
            where T : SyntaxNode
        {
            VisitSeparated(enumerable, _ => betweenNodes());
        }

        void VisitSeparated<T>(IEnumerable<T> enumerable, Action<T> betweenNodes)
            where T : SyntaxNode
        {
            using var enumerator = enumerable.GetEnumerator();
            if (enumerator.MoveNext())
            {
                var node = enumerator.Current;

                while (enumerator.MoveNext())
                {
                    node.Accept(this);
                    betweenNodes(node);

                    node = enumerator.Current;
                }

                node.Accept(this);
            }
        }

        private IDisposable MarkContainer(string name, ContainerType type)
        {
            var saveContainerName = _containerName;
            var saveContainerType = _containerType;
            _containerName = name;
            _containerType = type;

            return Disposable.Create(() =>
            {
                _containerName = saveContainerName;
                _containerType = saveContainerType;
            });
        }

        public void Dispose()
        {
            _writer.Dispose();
            _stream.Dispose();
        }
    }

    class Disposable
    {
        public static IDisposable Create(Action action) => new ActionDisposable(action);
        private class ActionDisposable : IDisposable
        {
            private Action _action;

            public ActionDisposable(Action action)
            {
                _action = action;
            }

            public void Dispose()
            {
                _action();
            }
        }
    }
}