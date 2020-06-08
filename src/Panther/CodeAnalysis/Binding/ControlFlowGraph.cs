using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class ControlFlowGraph
    {
        public BasicBlock Start { get; }
        public BasicBlock End { get; }
        public List<BasicBlock> Blocks { get; }
        public List<BasicBlockBranch> Branches { get; }

        private ControlFlowGraph(BasicBlock start, BasicBlock end, List<BasicBlock> blocks, List<BasicBlockBranch> branches)
        {
            Start = start;
            End = end;
            Blocks = blocks;
            Branches = branches;
        }

        public sealed class BasicBlock
        {
            public BasicBlock()
            {
            }

            public BasicBlock(bool isStart)
            {
                IsStart = isStart;
                IsEnd = !isStart;
            }

            public bool IsEnd { get; }
            public bool IsStart { get; }

            public List<BoundStatement> Statements { get; } = new List<BoundStatement>();
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
            public BoundExpression? Condition { get; }

            public BasicBlockBranch(BasicBlock from, BasicBlock to, BoundExpression? condition)
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
            private readonly List<BoundStatement> _statements = new List<BoundStatement>();
            private readonly List<BasicBlock> _blocks = new List<BasicBlock>();

            public List<BasicBlock> Build(BoundBlockExpression block)
            {
                foreach (var statement in block.Statements)
                {
                    switch (statement.Kind)
                    {
                        case BoundNodeKind.ConditionalGotoStatement:
                        case BoundNodeKind.GotoStatement:
                            _statements.Add(statement);
                            StartBlock();
                            break;

                        case BoundNodeKind.LabelStatement:
                            StartBlock();
                            _statements.Add(statement);
                            break;

                        case BoundNodeKind.VariableDeclarationStatement:
                        case BoundNodeKind.ExpressionStatement:
                        case BoundNodeKind.AssignmentStatement:
                        case BoundNodeKind.NopStatement:
                            _statements.Add(statement);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(statement), statement.Kind.ToString());
                    }
                }

                _statements.Add(new BoundExpressionStatement(block.Syntax, block.Expression));

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
            private readonly Dictionary<BoundLabel, BasicBlock> _blockFromLabel = new Dictionary<BoundLabel, BasicBlock>();
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
                        if (statement is BoundLabelStatement labelStatement)
                        {
                            _blockFromLabel.Add(labelStatement.BoundLabel, block);
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

            private void Walk(BoundStatement statement, BasicBlock current, BasicBlock next, in bool isLastStatementInBlock)
            {
                switch (statement)
                {
                    case BoundConditionalGotoStatement conditionalGotoStatement:
                        {
                            if (_blockFromLabel.TryGetValue(conditionalGotoStatement.BoundLabel, out var thenBlock))
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
                    case BoundGotoStatement gotoStatement:
                        {
                            if (_blockFromLabel.TryGetValue(gotoStatement.BoundLabel, out var toBlock))
                            {
                                Connect(current, toBlock);
                            }
                            else
                            {
                                // we really shouldn't get here but we have a test that can produce invalid jumps at the moment
                            }

                            break;
                        }
                    case BoundLabelStatement _:
                    case BoundExpressionStatement _:
                    case BoundVariableDeclarationStatement _:
                    case BoundAssignmentStatement _:
                    case BoundNopStatement _:
                        if (isLastStatementInBlock)
                            Connect(current, next);

                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(statement), statement.Kind.ToString());
                }
            }

            private BoundExpression Negate(BoundExpression condition)
            {
                if (condition is BoundLiteralExpression literalExpression)
                {
                    var value = (bool)literalExpression.Value;
                    return new BoundLiteralExpression(condition.Syntax, !value);
                }

                var op = BoundUnaryOperator.Bind(SyntaxKind.BangToken, TypeSymbol.Bool) ?? throw new Exception("invalid operator");

                return new BoundUnaryExpression(condition.Syntax, op, condition);
            }

            private void Connect(BasicBlock @from, BasicBlock to, BoundExpression? boundCondition = null)
            {
                if (boundCondition is BoundLiteralExpression lit)
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

        public static ControlFlowGraph Create(BoundBlockExpression cfgExpression)
        {
            var basicBlockBuilder = new BasicBlockBuilder();
            var blocks = basicBlockBuilder.Build(cfgExpression);

            var graphBuilder = new GraphBuilder();
            return graphBuilder.Build(blocks);
        }

        public static bool AllBlocksReturn(BoundBlockExpression body)
        {
            var graph = Create(body);
            foreach (var branch in graph.End.Incoming)
            {
                var lastStatement = branch.From.Statements.Last();
                if (lastStatement.Kind != BoundNodeKind.ExpressionStatement)
                    return false;
            }

            return true;
        }
    }
}