using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;
using Type = Panther.CodeAnalysis.Symbols.Type;

namespace Panther.CodeAnalysis.Binding;

internal sealed class ControlFlowGraph
{
    public BasicBlock Start { get; }
    public BasicBlock End { get; }
    public List<BasicBlock> Blocks { get; }
    public List<BasicBlockBranch> Branches { get; }

    private ControlFlowGraph(
        BasicBlock start,
        BasicBlock end,
        List<BasicBlock> blocks,
        List<BasicBlockBranch> branches
    )
    {
        Start = start;
        End = end;
        Blocks = blocks;
        Branches = branches;
    }

    public sealed class BasicBlock
    {
        public BasicBlock() { }

        public BasicBlock(bool isStart)
        {
            IsStart = isStart;
            IsEnd = !isStart;
        }

        public bool IsEnd { get; }
        public bool IsStart { get; }

        public List<TypedStatement> Statements { get; } = new List<TypedStatement>();
        public List<BasicBlockBranch> Incoming { get; } = new List<BasicBlockBranch>();
        public List<BasicBlockBranch> Outgoing { get; } = new List<BasicBlockBranch>();

        public override string ToString()
        {
            if (IsStart)
                return "<Start>";

            if (IsEnd)
                return "<End>";

            using var writer = new StringWriter();
            foreach (var statement in Statements)
            {
                statement.WriteTo(writer);
            }

            return writer.ToString();
        }
    }

    public sealed class BasicBlockBranch
    {
        public BasicBlock From { get; }
        public BasicBlock To { get; }
        public TypedExpression? Condition { get; }

        public BasicBlockBranch(BasicBlock from, BasicBlock to, TypedExpression? condition)
        {
            From = @from;
            To = to;
            Condition = condition;
        }

        public override string ToString() =>
            Condition == null ? string.Empty : Condition.ToString();
    }

    public sealed class BasicBlockBuilder
    {
        private readonly List<TypedStatement> _statements = new List<TypedStatement>();
        private readonly List<BasicBlock> _blocks = new List<BasicBlock>();

        public List<BasicBlock> Build(TypedBlockExpression block)
        {
            foreach (var statement in block.Statements)
            {
                switch (statement.Kind)
                {
                    case TypedNodeKind.ConditionalGotoStatement:
                    case TypedNodeKind.GotoStatement:
                        _statements.Add(statement);
                        StartBlock();
                        break;

                    case TypedNodeKind.LabelStatement:
                        StartBlock();
                        _statements.Add(statement);
                        break;

                    case TypedNodeKind.VariableDeclarationStatement:
                    case TypedNodeKind.ExpressionStatement:
                    case TypedNodeKind.AssignmentStatement:
                    case TypedNodeKind.NopStatement:
                        _statements.Add(statement);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(statement),
                            statement.Kind.ToString()
                        );
                }
            }

            _statements.Add(new TypedExpressionStatement(block.Syntax, block.Expression));

            EndBlock();
            return _blocks.ToList();
        }

        private void StartBlock()
        {
            EndBlock();
        }

        private void EndBlock()
        {
            if (_statements.Any())
            {
                var block = new BasicBlock();
                block.Statements.AddRange(_statements);
                _blocks.Add(block);
                _statements.Clear();
            }
        }
    }

    public sealed class GraphBuilder
    {
        private readonly Dictionary<TypedLabel, BasicBlock> _blockFromLabel =
            new Dictionary<TypedLabel, BasicBlock>();
        private readonly List<BasicBlockBranch> _branches = new List<BasicBlockBranch>();
        private readonly BasicBlock _start = new BasicBlock(isStart: true);
        private readonly BasicBlock _end = new BasicBlock(isStart: false);

        public ControlFlowGraph Build(List<BasicBlock> blocks)
        {
            Connect(_start, !blocks.Any() ? _end : blocks.First());

            foreach (var block in blocks)
            {
                foreach (var statement in block.Statements)
                {
                    if (statement is TypedLabelStatement labelStatement)
                    {
                        _blockFromLabel.Add(labelStatement.TypedLabel, block);
                    }
                }
            }

            for (var index = 0; index < blocks.Count; index++)
            {
                var block = blocks[index];
                var next = index == blocks.Count - 1 ? _end : blocks[index + 1];
                foreach (var statement in block.Statements)
                {
                    var isLastStatementInBlock = statement == block.Statements.Last();
                    Walk(statement, block, next, isLastStatementInBlock);
                }
            }

            var scan = true;
            while (scan)
            {
                scan = false;
                foreach (var block in blocks.Where(block => !block.Incoming.Any()))
                {
                    RemoveBlock(blocks, block);
                    scan = true;
                    break;
                }
            }

            blocks.Insert(0, _start);
            blocks.Add(_end);

            return new ControlFlowGraph(_start, _end, blocks, _branches);
        }

        private void RemoveBlock(List<BasicBlock> blocks, BasicBlock block)
        {
            foreach (var branch in block.Incoming)
            {
                branch.From.Outgoing.Remove(branch);
                _branches.Remove(branch);
            }

            foreach (var branch in block.Outgoing)
            {
                branch.To.Incoming.Remove(branch);
                _branches.Remove(branch);
            }

            blocks.Remove(block);
        }

        private void Walk(
            TypedStatement statement,
            BasicBlock current,
            BasicBlock next,
            in bool isLastStatementInBlock
        )
        {
            switch (statement)
            {
                case TypedConditionalGotoStatement conditionalGotoStatement:
                {
                    if (
                        _blockFromLabel.TryGetValue(
                            conditionalGotoStatement.TypedLabel,
                            out var thenBlock
                        )
                    )
                    {
                        var negatedCondition = Negate(conditionalGotoStatement.Condition);
                        var thenCondition = conditionalGotoStatement.JumpIfTrue
                            ? conditionalGotoStatement.Condition
                            : negatedCondition;
                        var elseCondition = conditionalGotoStatement.JumpIfTrue
                            ? negatedCondition
                            : conditionalGotoStatement.Condition;

                        Connect(current, thenBlock, thenCondition);
                        Connect(current, next, elseCondition);
                    }
                    else
                    {
                        // we really shouldn't get here but we have a test that can produce invalid jumps at the moment
                    }

                    break;
                }
                case TypedGotoStatement gotoStatement:
                {
                    if (_blockFromLabel.TryGetValue(gotoStatement.TypedLabel, out var toBlock))
                    {
                        Connect(current, toBlock);
                    }
                    else
                    {
                        // we really shouldn't get here but we have a test that can produce invalid jumps at the moment
                    }

                    break;
                }
                case TypedLabelStatement _:
                case TypedExpressionStatement _:
                case TypedVariableDeclarationStatement _:
                case TypedAssignmentStatement _:
                case TypedNopStatement _:
                    if (isLastStatementInBlock)
                        Connect(current, next);

                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(statement),
                        statement.Kind.ToString()
                    );
            }
        }

        private TypedExpression Negate(TypedExpression condition)
        {
            if (condition is TypedLiteralExpression literalExpression)
            {
                var value = (bool)literalExpression.Value;
                return new TypedLiteralExpression(condition.Syntax, !value);
            }

            var op =
                TypedUnaryOperator.Bind(SyntaxKind.BangToken, Type.Bool)
                ?? throw new Exception("invalid operator");

            return new TypedUnaryExpression(condition.Syntax, op, condition);
        }

        private void Connect(
            BasicBlock @from,
            BasicBlock to,
            TypedExpression? boundCondition = null
        )
        {
            if (boundCondition is TypedLiteralExpression lit)
            {
                if ((bool)lit.Value)
                {
                    // dont show label for unconditional jump
                    boundCondition = null;
                }
                else
                {
                    // dont connect because the condition is always false
                    return;
                }
            }
            var branch = new BasicBlockBranch(from, to, boundCondition);
            from.Outgoing.Add(branch);
            to.Incoming.Add(branch);
            _branches.Add(branch);
        }
    }

    public void WriteTo(TextWriter writer)
    {
        string quote(string text)
        {
            return "\"" + text.Replace("\"", "\\\"") + "\"";
        }
        writer.WriteLine("digraph G {");

        var blockIds = new Dictionary<BasicBlock, string>();
        for (var index = 0; index < Blocks.Count; index++)
        {
            var id = $"N{index}";
            blockIds.Add(Blocks[index], id);
        }

        foreach (var block in Blocks)
        {
            var id = blockIds[block];
            var label = quote(block.ToString().Replace(Environment.NewLine, "\\l"));
            writer.WriteLine($"    {id} [label = {label} shape = box]");
        }

        foreach (var branch in Branches)
        {
            var fromId = blockIds[branch.From];
            var toId = blockIds[branch.To];
            var label = quote(branch.ToString());
            writer.WriteLine($"    {fromId} -> {toId} [label = {label}]");
        }

        writer.WriteLine("}");
    }

    public static ControlFlowGraph Create(TypedBlockExpression cfgExpression)
    {
        var basicBlockBuilder = new BasicBlockBuilder();
        var blocks = basicBlockBuilder.Build(cfgExpression);

        var graphBuilder = new GraphBuilder();
        return graphBuilder.Build(blocks);
    }

    public static bool AllBlocksReturn(TypedBlockExpression body)
    {
        var graph = Create(body);
        foreach (var branch in graph.End.Incoming)
        {
            var lastStatement = branch.From.Statements.Last();
            if (lastStatement.Kind != TypedNodeKind.ExpressionStatement)
                return false;
        }

        return true;
    }
}
