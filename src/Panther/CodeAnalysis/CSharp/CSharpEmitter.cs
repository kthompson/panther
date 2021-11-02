using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Panther.CodeAnalysis.Syntax;
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

            // emit body if any exists
            var saveContainerType = _containerType;

            _containerType = ContainerType.Class;

            node.Template?.Accept(this);

            _containerType = saveContainerType;

            // close class
            EndBlock();
            // WriteLine("<=CD3");
        }

        public override void VisitTemplate(TemplateSyntax node)
        {
            if (_containerType == ContainerType.Class)
            {
                WriteLine();
            }
            VisitSeparated(node.Members, () => WriteLine());
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
            _writer.Write("var ");
            _writer.Write(node.IdentifierToken.Text);
            _writer.Write(" = ");
            node.Expression.Accept(this);
            _writer.WriteLine(";");
        }

        public override void VisitObjectDeclaration(ObjectDeclarationSyntax node)
        {
            _writer.WriteLine($"public partial class {node.Identifier.Text}");
            StartBlock();
            node.Template.Accept(this);
            EndBlock();
        }

        public override void VisitFunctionDeclaration(FunctionDeclarationSyntax node)
        {
            _writer.Write("public ");

            if (_containerType != ContainerType.Class)
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
            node.Body.Accept(this);
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
            StartBlock();

            foreach (var statement in node.Statements)
            {
                statement.Accept(this);
            }

            node.Expression.Accept(this);
            // TODO: this should really only be emitted when our blocks type is non-unit
            _writer.WriteLine(";");

            EndBlock();
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
            node.ThenExpression.Accept(this);

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

            node.ElseExpression.Accept(this);
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

            VisitSeparated(node.Members, previous =>
            {
                if (previous is FunctionDeclarationSyntax or ClassDeclarationSyntax or ObjectDeclarationSyntax)
                {
                    WriteLine();
                }
            });
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

        public static string ToCSharpText(SyntaxTree tree)
        {
            using var emitter = new CSharpEmitter(tree);
            emitter.Visit(tree.Root);
            return emitter.ToText();
        }
        public void Dispose()
        {
            _writer.Dispose();
            _stream.Dispose();
        }
    }
}