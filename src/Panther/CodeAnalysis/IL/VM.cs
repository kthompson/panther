using System;
using System.Collections.Immutable;

namespace Panther.CodeAnalysis.IL;

public class VM
{
    private int _sp; // stack pointer
    private int _ip; // instruction pointer
    private int _lp; // locals pointer
    private int _cs; // call stack pointer

    /**
         * stack[_fp]       arg 1, also where we put our return value
         * stack[_fp + 1]   arg 2,
         * stack[_fp + 2]   arg 3,
         * stack[_fp + N]   arg N,
         * stack[_lp - 3]   return address(new ip after return)
         * stack[_lp - 2]   old _fp
         * stack[_lp - 1]   old _lp
         * stack[_lp]       local.0
         * stack[_lp + 1]   local.1
         * stack[_lp + N]   local.N
         */
    private int _fp; // frame pointer

    private ImmutableArray<Instruction> _instructions;
    // indices into the instructions for the call stack
    private readonly int[] _callStack = new int[CallStackDepth];
    private readonly int[] _callStackArgCount = new int[CallStackDepth];
    private readonly long[] _memory;

    [Flags]
    enum TypeCode
    {
        Int     = 0b0000,
        Bool    = 0b0100,
        Float   = 0b1000,
        Pointer = 0b1100,
    }

    private const int CallStackDepth = 20;

    private TypeCode _stackTopType = TypeCode.Bool;
    public VM(ImmutableArray<Instruction> instructions, long[] memory)
    {
        _instructions = instructions;
        _memory = memory;
    }

    public ImmutableArray<VMFrame> BuildCallStack()
    {
        var builder = ImmutableArray.CreateBuilder<VMFrame>();
        var cs = _cs - 1;
        while (cs >= 0)
        {
            var function = _instructions[_callStack[cs]];
            var args = _callStackArgCount[cs];
            builder.Add(new VMFrame(function.Operand1 as string ?? "", args, function.Operand2 as int? ?? 0));
            cs--;
        }

        return builder.ToImmutable();
    }


    /// <summary>
    /// Returns true if there are more instructions to process
    /// </summary>
    /// <returns></returns>
    public bool Step()
    {
        var instruction = _instructions[_ip];

        switch (instruction.Code)
        {
            case OpCode.Add:
                ExecuteAdd();
                break;
            case OpCode.And:
                ExecuteAnd();
                break;
            case OpCode.Br:
                ExecuteBr();
                break;
            case OpCode.Brfalse:
                ExecuteBrfalse();
                break;
            case OpCode.Brtrue:
                ExecuteBrtrue();
                break;
            case OpCode.Call:
                ExecuteCall();
                break;
            case OpCode.Ceq:
                ExecuteCeq();
                break;
            case OpCode.Cgt:
                ExecuteCgt();
                break;
            case OpCode.Clt:
                ExecuteClt();
                break;
            case OpCode.Div:
                ExecuteDiv();
                break;
            case OpCode.Function:
                ExecuteFunction();
                break;
            case OpCode.Ldarg:
                ExecuteLdarg();
                break;
            case OpCode.Ldc:
                ExecuteLdc();
                break;
            case OpCode.Ldfld:
                ExecuteLdfld();
                break;
            case OpCode.Ldloc:
                ExecuteLdloc();
                break;
            case OpCode.Ldsfld:
                ExecuteLdsfld();
                break;
            case OpCode.Ldstr:
                ExecuteLdstr();
                break;
            case OpCode.Mul:
                ExecuteMul();
                break;
            case OpCode.Neg:
                ExecuteNeg();
                break;
            case OpCode.New:
                ExecuteNew();
                break;
            case OpCode.Nop:
                ExecuteNop();
                break;
            case OpCode.Not:
                ExecuteNot();
                break;
            case OpCode.Or:
                ExecuteOr();
                break;
            case OpCode.Pop:
                ExecutePop();
                break;
            case OpCode.Ret:
                ExecuteRet();
                break;
            case OpCode.Starg:
                ExecuteStarg();
                break;
            case OpCode.Stfld:
                ExecuteStfld();
                break;
            case OpCode.Stloc:
                ExecuteStloc();
                break;
            case OpCode.Stsfld:
                ExecuteStsfld();
                break;
            case OpCode.Sub:
                ExecuteSub();
                break;
            case OpCode.Xor:
                ExecuteXor();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return _ip >= 0 && _ip < _instructions.Length;
    }

    private void ExecuteFunction()
    {
        var locals = _instructions[_ip++].Operand2 as int? ?? int.MinValue;

        for (int i = 0; i < locals; i++)
        {
            // TODO what kind of locals???
            _memory[_sp++] = (long)TypeCode.Int;
        }
    }

    private T ThrowInvalidProgram<T>() =>
        throw new InvalidProgramException($"Invalid instruction: {_instructions[_ip - 1]}");

    private void ExecuteCall()
    {
        var ins = _instructions[_ip];
        var newIp = ins.Operand1 as int? ?? ThrowInvalidProgram<int>();
        var argCount = ins.Operand2 as int? ?? ThrowInvalidProgram<int>();

        _callStack[_cs] = newIp;
        _callStackArgCount[_cs] = argCount;
        _cs++;

        _memory[_sp++] = _ip + 1; // return at the address of the next instruction
        _memory[_sp++] = _fp; // save frame pointer
        _memory[_sp++] = _lp; // save local pointer
        _fp = _sp - 3 - argCount;
        _lp = _sp;
        _ip = newIp;
    }

    private void ExecuteRet()
    {
        // drop the top frame off the callstack
        _cs--;

        var frame = _lp;
        var ret = _memory[frame - 3]; // return address

        // pop the top of the stack into our frame pointer which starts at the first arg(or the top of the stack before the call)
        // reposition the return value so that it is at the top of the stack after we remove our args
        _memory[_fp] = _memory[_sp - 1];

        // update stack pointer so that its after the item we just pushed to the stack
        _sp = _fp + 1;

        // restore old registers
        _lp = (int)_memory[frame - 1]; // old _lp
        _fp = (int)_memory[frame - 2]; // old _fp
        _ip = (int)ret;
    }

    private void ExecuteBr()
    {
        _ip = _instructions[_ip].Operand1 as int? ?? int.MinValue;
    }

    private void ExecuteBrtrue()
    {
        if (_memory[--_sp] == 1)
        {
            _ip = _instructions[_ip].Operand1 as int? ?? int.MinValue;
        }
    }

    private void ExecuteBrfalse()
    {
        if (_memory[--_sp] == 0)
        {
            _ip = _instructions[_ip].Operand1 as int? ?? int.MinValue;
        }
    }

    private void ExecuteCgt()
    {
        _sp--;
        _memory[_sp - 1] = FromBool(_memory[_sp - 1] > _memory[_sp]);
        _ip++;
    }

    private void ExecuteCeq()
    {
        _sp--;
        _memory[_sp - 1] = FromBool(_memory[_sp] == _memory[_sp - 1]);
        _ip++;
    }

    private void ExecuteClt()
    {
        _sp--;
        _memory[_sp - 1] = FromBool(_memory[_sp - 1] < _memory[_sp]);
        _ip++;
    }

    private long FromBool(bool b) => b ? 1 : 0;
    private bool ToBool(long value) => value == 1;

    private void ExecuteDiv()
    {
        _sp--;
        _memory[_sp - 1] = ((_memory[_sp - 1] >> 2) / (_memory[_sp] >> 2)) << 2;
        _ip++;
    }

    private void ExecuteLdarg()
    {
        /*
         * stack[_fp]       arg 1, also where we put our return value
         * stack[_fp + 1]   arg 2,
         * stack[_fp + 2]   arg 3,
         * stack[_fp + N]   arg N,
         */

        var argIndex = _instructions[_ip++].Operand1 as int? ?? ThrowInvalidProgram<int>();
        // Push to stack from local
        _memory[_sp++] = _memory[_fp + argIndex];
    }

    private void ExecuteAnd()
    {
        _sp--;
        _memory[_sp - 1] &= _memory[_sp];

        _ip++;
    }

    private void ExecuteLdc()
    {
        var op = _instructions[_ip++].Operand1 ?? ThrowInvalidProgram<int>();
        if (op == null)
            throw new InvalidProgramException($"Invalid instruction at address 0x{(_ip - 1):X8}");

        _memory[_sp++] = (int)op << 2; // type mask is 0 for ints
    }

    private void ExecuteLdfld()
    {
        throw new NotImplementedException();
    }

    private void ExecuteLdloc()
    {
        /*
         * _stack[_lp]       local.0
         * _stack[_lp + 1]   local.1
         * _stack[_lp + N]   local.N
         */

        var localIndex = _instructions[_ip++].Operand1 as int? ?? ThrowInvalidProgram<int>();
        // Push to stack from local
        _memory[_sp++] = _memory[_lp + localIndex];
    }

    private void ExecuteLdsfld()
    {
        throw new NotImplementedException();
    }

    private void ExecuteMul()
    {
        _sp--;
        _memory[_sp - 1] = ((_memory[_sp] >> 2) * (_memory[_sp - 1] >> 2)) << 2;
        _ip++;
    }

    private void ExecuteLdstr()
    {
        throw new NotImplementedException();
    }

    private void ExecuteNeg()
    {
        // TODO: support floats?
        var top = _sp - 1;
        _memory[top] = -(_memory[top] >> 2) << 2;
        _ip++;
    }

    private void ExecuteNew()
    {
        throw new NotImplementedException();
    }

    private void ExecuteNop()
    {
        // just go to the next instruction
        _ip++;
    }

    private void ExecuteOr()
    {
        _sp--;
        _memory[_sp - 1] |= _memory[_sp];

        _ip++;
    }

    private void ExecutePop()
    {
        throw new NotImplementedException();
    }

    private void ExecuteStloc()
    {
        /*
         * _stack[_lp]       local.0
         * _stack[_lp + 1]   local.1
         * _stack[_lp + N]   local.N
         */

        var localIndex = _instructions[_ip++].Operand1 as int? ?? ThrowInvalidProgram<int>();
        // Pop from stack and store
        _memory[_lp + localIndex] = _memory[--_sp];
    }

    private void ExecuteStfld()
    {
        throw new NotImplementedException();
    }

    private void ExecuteStarg()
    {
        throw new NotImplementedException();
    }

    private void ExecuteNot()
    {
        _memory[_sp - 1] = ~_memory[_sp - 1];

        _ip++;
    }

    private void ExecuteXor()
    {
        _sp--;
        _memory[_sp - 1] ^= _memory[_sp];
        _ip++;
    }

    private void ExecuteSub()
    {
        _sp--;
        _memory[_sp - 1] -= _memory[_sp];
        _ip++;
    }

    private void ExecuteStsfld()
    {
        throw new NotImplementedException();
    }

    private void ExecuteAdd()
    {
        _sp--;
        _memory[_sp - 1] = _memory[_sp] + _memory[_sp - 1];
        _ip++;
    }

    public void Run()
    {
        while (Step())
        {
        }
    }

    public object? StackTop()
    {
        var value = _memory[_sp - 1];

        switch (_stackTopType)
        {
            case TypeCode.Pointer:
                // var item = _heap[(int)(value & ValueMask)];
                return null;
            case TypeCode.Int:
                return value;
            case TypeCode.Bool:
                return ToBool(value);
            case TypeCode.Float:
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}