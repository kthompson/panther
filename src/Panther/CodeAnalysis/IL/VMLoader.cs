using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Panther.CodeAnalysis.IL;

public class VMLoader
{
    private readonly long[] _memory;
    private readonly List<Instruction> _instructionsList;
    private int _allocationBase;
    private int? _entryPoint;

    public VMLoader()
        : this(new long[4096], 2048)
    {
    }

    public VMLoader(long[] memory, int allocationBase)
    {
        _memory = memory;
        _allocationBase = allocationBase;
        _instructionsList = new List<Instruction>();
    }

    public VM CreateVM() => new VM(_instructionsList.ToImmutableArray(), _memory.ToArray());

    public void Load(ObjectListing listing)
    {
        // allocate constants and build constant id to offset map
        var constantMap = new Dictionary<int, int>();
        for (int id = 0; id < listing.Data.Length; id++)
        {
            var offset = Allocate(listing.Data[id]);
            constantMap[id] = offset;
        }

        // if this is our first object listing, insert an initial jump to the entry point
        // we will patch this later, once we know where the actual entry point is
        if (_instructionsList.Count == 0)
        {
            _instructionsList.Insert(0, new Instruction(0, OpCode.Call, null, 0));
            _instructionsList.Insert(1, new Instruction(0, OpCode.Br, -1, null)); // terminate
        }

        var start = _instructionsList.Count;

        // scan for labels and methods
        // we will catalog all labels as method.label and remove each label as we find them
        _instructionsList.AddRange(listing.Instructions);

        string method = "";

        var labelOffsets = new Dictionary<string, int>();
        var functionOffsets = new Dictionary<string, int>();
        for (int i = start; i < _instructionsList.Count; i++)
        {
            var instruction = _instructionsList[i];
            switch (instruction.Code)
            {
                case OpCode.Function:
                {
                    method = instruction.Operand1 as string ?? "";
                    functionOffsets.Add(method, i);

                    if (method is "$Program.$eval" or "$Program.Main" or "Program.Main")
                    {
                        // record our entry point
                        _entryPoint = i;

                        // update our initial jump to this location
                        _instructionsList[0].Operand1 = i;
                    }

                    // instructionsList.RemoveAt(i);
                    // i--;
                    break;
                }
                case OpCode.Label:
                {
                    var label = instruction.Operand1 as string ?? "";
                    labelOffsets.Add($"{method}.{label}", i);
                    _instructionsList.RemoveAt(i);
                    i--;
                    break;
                }
                case OpCode.Br or OpCode.Brfalse or OpCode.Brtrue:
                {
                    // update operands to point to method.label instead of just label
                    var label = instruction.Operand1 as string ?? "";
                    var target = $"{method}.{label}";

                    instruction.Operand1 = target;
                    break;
                }
            }
        }

        method = "";
        // now we have a list of instructions and all of the labels are removed
        // but we have a map to each labelled address

        // now we need to patch each br, brfalse, and brtrue to use the instruction offset instead of the label

        for (var index = start; index < _instructionsList.Count; index++)
        {
            var instruction = _instructionsList[index];
            switch (instruction.Code)
            {
                case OpCode.Br or OpCode.Brfalse or OpCode.Brtrue:
                {
                    // update operand to point to the absolute position of the target
                    var target = instruction.Operand1 as string ?? "";
                    instruction.Operand1 = labelOffsets[target];
                    break;
                }
                case OpCode.Call:
                {
                    var target = instruction.Operand1 as string ?? "";
                    instruction.Operand1 = functionOffsets[target];
                    break;
                }
                case OpCode.Ldstr:
                {
                    // replace ldstr constant id with the allocated string offset
                    var constantId = instruction.Operand1 as int? ?? throw new InvalidProgramException();
                    instruction.Operand1 = constantMap[constantId];
                    break;
                }
            }
        }
    }

    private int Allocate(Data data)
    {
        var offset = _allocationBase;
        switch (data)
        {
            case DataString dataString:
            {
                _allocationBase += 2 + dataString.Value.Length;

                var memory = Unsafe.As<byte[]>(_memory);
                var buffer = Encoding.UTF8.GetBytes(dataString.Value);

                using var ms = new MemoryStream(memory, offset, buffer.Length + 2);
                var bc = new BinaryWriter(ms);
                bc.Write((short)dataString.Value.Length);
                bc.Write(buffer);

                return offset;
            }
            case DataBytes(_):
                throw new NotImplementedException();
            case DataZero(_):
                throw new NotImplementedException();

            default:
                throw new ArgumentOutOfRangeException(nameof(data));
        }
    }
}