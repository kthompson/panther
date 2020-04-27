using System;
using System.Linq;
using Mono.Cecil;
using System.Collections.Generic;
using System.Collections.Immutable;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Emit
{
    internal class Emitter
    {
        private readonly DiagnosticBag _diagnostics = new DiagnosticBag();

        private readonly Dictionary<TypeSymbol, TypeReference> _knownTypes = new Dictionary<TypeSymbol, TypeReference>();
        private readonly Dictionary<FunctionSymbol, MethodDefinition> _methods = new Dictionary<FunctionSymbol, MethodDefinition>();
        private readonly AssemblyDefinition _assemblyDefinition;
        private readonly List<AssemblyDefinition> _assemblies = new List<AssemblyDefinition>();
        private TypeDefinition _typeDef;

        private readonly MethodReference? _consoleWriteLineReference;
        private readonly MethodReference? _consoleReadLineReference;
        private readonly MethodReference? _stringConcatReference;
        private readonly Dictionary<VariableSymbol, VariableDefinition> _locals = new Dictionary<VariableSymbol, VariableDefinition>();
        private readonly Dictionary<VariableSymbol, int> _localIndices = new Dictionary<VariableSymbol, int>();

        private readonly Dictionary<BoundLabel, Instruction> _labelTarget = new Dictionary<BoundLabel, Instruction>();
        private readonly Dictionary<BoundLabel, List<Instruction>> _branchInstructionsToPatch = new Dictionary<BoundLabel, List<Instruction>>();

        private Emitter(string moduleName, string[] references)
        {
            foreach (var reference in references)
            {
                try
                {
                    var asm = AssemblyDefinition.ReadAssembly(reference);
                    _assemblies.Add(asm);
                }
                catch (BadImageFormatException)
                {
                    _diagnostics.ReportInvalidReference(reference);
                }
            }

            var builtinTypes = new List<(TypeSymbol tyoe, Type MetadataType)>
            {
                (TypeSymbol.Any, typeof(object)),
                (TypeSymbol.Bool, typeof(bool)),
                (TypeSymbol.Int, typeof(int)),
                (TypeSymbol.String, typeof(string)),
                (TypeSymbol.Unit, typeof(void)), // this might not work?
            };

            var assemblyName = new AssemblyNameDefinition(moduleName, new Version(1, 0));
            _assemblyDefinition = AssemblyDefinition.CreateAssembly(assemblyName, moduleName, ModuleKind.Console);

            foreach (var (typeSymbol, metadataType) in builtinTypes)
            {
                var typeReference = ResolveBuiltinType(typeSymbol.Name, metadataType.FullName);
                if (typeReference == null)
                    continue;

                _knownTypes.Add(typeSymbol, typeReference);
            }

            _consoleWriteLineReference = ResolveMethod("System.Console", "WriteLine", new[] {"System.String"});
            _consoleReadLineReference = ResolveMethod("System.Console", "ReadLine", Array.Empty<string>());
            _consoleReadLineReference = ResolveMethod("System.Console", "ReadLine", Array.Empty<string>());
            _stringConcatReference = ResolveMethod("System.String", "Concat", new[] {"System.String", "System.String"});
        }

        public static ImmutableArray<Diagnostic> Emit(BoundProgram program, string moduleName, string[] references,
            string outputPath)
        {
            if (program.Diagnostics.Any())
                return program.Diagnostics;

            var emitter = new Emitter(moduleName, references);
            return emitter.Emit(program, outputPath);
        }

        public ImmutableArray<Diagnostic> Emit(BoundProgram program, string outputPath)
        {
            if (_diagnostics.Any())
                return _diagnostics.ToImmutableArray();

            var objectType = _knownTypes[TypeSymbol.Any];
            _typeDef = new TypeDefinition("", "Program", TypeAttributes.Abstract | TypeAttributes.Sealed, objectType);
            _assemblyDefinition.MainModule.Types.Add(_typeDef);

            // ensure all functions exist first so we can reference them
            foreach (var functionSignature in program.Functions.Keys)
                EmitFunctionDeclaration(functionSignature);

            // emit the function bodies now
            foreach (var (functionSignature, boundBlockExpression) in program.Functions)
                EmitFunctionBody(functionSignature, boundBlockExpression);

            if (program.MainFunction != null)
            {
                var entryPoint = _methods[program.MainFunction];
                _assemblyDefinition.EntryPoint = entryPoint;
            }

            _assemblyDefinition.Write(outputPath);

            return _diagnostics.ToImmutableArray();
        }

        private void EmitFunctionDeclaration(FunctionSymbol function)
        {
            var returnType = _knownTypes[function.ReturnType];
            var method = new MethodDefinition(function.Name, MethodAttributes.Static | MethodAttributes.Private, returnType);
            var methodParams =
                from parameter in function.Parameters
                let paramType = _knownTypes[parameter.Type]
                select new ParameterDefinition(parameter.Name, ParameterAttributes.None, paramType);

            foreach (var p in methodParams)
                method.Parameters.Add(p);

            _methods[function] = method;

            _typeDef.Methods.Add(method);
        }

        private void EmitFunctionBody(FunctionSymbol function, BoundBlockExpression block)
        {
            var method = _methods[function];
            var ilProcessor = method.Body.GetILProcessor();

            _locals.Clear();
            _localIndices.Clear();
            _labelTarget.Clear();
            _branchInstructionsToPatch.Clear();

            foreach (var statement in block.Statements)
                EmitStatement(ilProcessor, statement);

            // emit expression
            EmitExpression(ilProcessor, block.Expression);

            // pop non-unit expressions off the stack for unit functions
            if (block.Expression.Type != TypeSymbol.Unit && function.ReturnType == TypeSymbol.Unit)
                ilProcessor.Emit(OpCodes.Pop);

            ilProcessor.Emit(OpCodes.Ret);
            method.Body.OptimizeMacros();

            // TODO: verify that _branchInstructionsToPatch is empty
        }

        private void EmitStatement(ILProcessor ilProcessor, BoundStatement statement)
        {
            switch (statement)
            {
                case BoundConditionalGotoStatement conditionalGotoStatement:
                    EmitConditionalGotoStatement(ilProcessor, conditionalGotoStatement);
                    break;
                case BoundExpressionStatement expressionStatement:
                    EmitExpressionStatement(ilProcessor, expressionStatement);
                    break;
                case BoundGotoStatement gotoStatement:
                    EmitGotoStatement(ilProcessor, gotoStatement);
                    break;
                case BoundLabelStatement labelStatement:
                    EmitLabelStatement(ilProcessor, labelStatement);
                    break;
                case BoundVariableDeclarationStatement variableDeclarationStatement:
                    EmitVariableDeclarationStatement(ilProcessor, variableDeclarationStatement);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(statement));
            }
        }

        private void EmitVariableDeclarationStatement(ILProcessor ilProcessor, BoundVariableDeclarationStatement variableDeclarationStatement)
        {
            var pantherVar = variableDeclarationStatement.Variable;
            var variableType = _knownTypes[pantherVar.Type];
            var variableDef = new VariableDefinition(variableType);
            _locals[pantherVar] = variableDef;
            var index = ilProcessor.Body.Variables.Count;
            _localIndices[pantherVar] = index;
            ilProcessor.Body.Variables.Add(variableDef);

            EmitExpression(ilProcessor, variableDeclarationStatement.Expression);
            ilProcessor.Emit(OpCodes.Stloc, index);
        }

        private void EmitGotoStatement(ILProcessor ilProcessor, BoundGotoStatement gotoStatement)
        {
            var branchInstr = ilProcessor.Create(OpCodes.Br);

            SetBranchTarget(gotoStatement.BoundLabel, branchInstr);

            ilProcessor.Append(branchInstr);
        }

        private void EmitLabelStatement(ILProcessor ilProcessor, BoundLabelStatement labelStatement)
        {
            var target = ilProcessor.Create(OpCodes.Nop);
            ilProcessor.Append(target);
            CreateLabel(labelStatement.BoundLabel, target);
        }

        private void EmitConditionalGotoStatement(ILProcessor ilProcessor, BoundConditionalGotoStatement conditionalGotoStatement)
        {
            EmitExpression(ilProcessor, conditionalGotoStatement.Condition);
            var op = (conditionalGotoStatement.JumpIfTrue) ? OpCodes.Brtrue : OpCodes.Brfalse;
            var branchInstruction = ilProcessor.Create(op);

            SetBranchTarget(conditionalGotoStatement.BoundLabel, branchInstruction);
            ilProcessor.Append(branchInstruction);
        }

        private void EmitExpressionStatement(ILProcessor ilProcessor, BoundExpressionStatement expressionStatement)
        {
            EmitExpression(ilProcessor, expressionStatement.Expression);

            // pop non-unit expressions off the stack as their result wont be used
            // unit expression have no result on the stack
            if (expressionStatement.Expression.Type != TypeSymbol.Unit)
                ilProcessor.Emit(OpCodes.Pop);
        }

        private void EmitExpression(ILProcessor ilProcessor, BoundExpression expression)
        {
            switch (expression)
            {
                case BoundAssignmentExpression assignmentExpression:
                    EmitAssignmentExpression(ilProcessor, assignmentExpression);
                    break;

                case BoundBinaryExpression binaryExpression:
                    EmitBinaryExpression(ilProcessor, binaryExpression);
                    break;

                case BoundCallExpression callExpression:
                    EmitCallExpression(ilProcessor, callExpression);
                    break;

                case BoundConversionExpression conversionExpression:
                    EmitConversionExpression(ilProcessor, conversionExpression);
                    break;

                case BoundLiteralExpression literalExpression:
                    EmitLiteralExpression(ilProcessor, literalExpression);
                    break;

                case BoundUnaryExpression unaryExpression:
                    EmitUnaryExpression(ilProcessor, unaryExpression);
                    break;

                case BoundUnitExpression unitExpression:
                    EmitUnitExpression(ilProcessor, unitExpression);
                    break;

                case BoundVariableExpression variableExpression:
                    EmitVariableExpression(ilProcessor, variableExpression);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(expression));
            }
        }

        private void EmitAssignmentExpression(ILProcessor ilProcessor, BoundAssignmentExpression assignmentExpression)
        {
            EmitExpression(ilProcessor, assignmentExpression.Expression);
            ilProcessor.Emit(OpCodes.Dup);
            ilProcessor.Emit(OpCodes.Stloc, _locals[assignmentExpression.Variable]);
        }

        private void EmitLiteralExpression(ILProcessor ilProcessor, BoundLiteralExpression literalExpression)
        {
            if (literalExpression.Type == TypeSymbol.Bool)
            {
                ilProcessor.Emit((bool) literalExpression.Value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            }
            else if (literalExpression.Type == TypeSymbol.Int)
            {
                ilProcessor.Emit(OpCodes.Ldc_I4, (int) literalExpression.Value);
            }
            else if (literalExpression.Type == TypeSymbol.String)
            {
                ilProcessor.Emit(OpCodes.Ldstr, (string) literalExpression.Value);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private void EmitBinaryExpression(ILProcessor ilProcessor, BoundBinaryExpression binaryExpression)
        {
            EmitExpression(ilProcessor, binaryExpression.Left);
            EmitExpression(ilProcessor, binaryExpression.Right);

            var @operator = binaryExpression.Operator;
            switch (@operator.Kind)
            {
                case BoundBinaryOperatorKind.Addition:
                    // string or int
                    if (@operator.LeftType == TypeSymbol.String)
                    {
                        ilProcessor.Emit(OpCodes.Call, _stringConcatReference);
                    }
                    else if (@operator.LeftType == TypeSymbol.Int)
                    {
                        ilProcessor.Emit(OpCodes.Add);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }

                    break;
                case BoundBinaryOperatorKind.BitwiseAnd:
                    // int
                    ilProcessor.Emit(OpCodes.And);
                    break;
                case BoundBinaryOperatorKind.BitwiseOr:
                    // int
                    ilProcessor.Emit(OpCodes.Or);
                    break;
                case BoundBinaryOperatorKind.BitwiseXor:
                    // int
                    ilProcessor.Emit(OpCodes.Xor);
                    break;
                case BoundBinaryOperatorKind.Division:
                    // int
                    ilProcessor.Emit(OpCodes.Div);
                    break;
                case BoundBinaryOperatorKind.Equal:
                    // int, bool, string
                    ilProcessor.Emit(OpCodes.Ceq);
                    break;
                case BoundBinaryOperatorKind.GreaterThan:
                    // int
                    throw new NotImplementedException();

                case BoundBinaryOperatorKind.GreaterThanOrEqual:
                    // int
                    throw new NotImplementedException();

                case BoundBinaryOperatorKind.LessThan:
                    // int
                    throw new NotImplementedException();

                case BoundBinaryOperatorKind.LessThanOrEqual:
                    // int
                    throw new NotImplementedException();

                case BoundBinaryOperatorKind.LogicalAnd:
                    // bool
                    throw new NotImplementedException();

                case BoundBinaryOperatorKind.LogicalOr:
                    // bool
                    throw new NotImplementedException();

                case BoundBinaryOperatorKind.Multiplication:
                    // int
                    ilProcessor.Emit(OpCodes.Mul);
                    break;
                case BoundBinaryOperatorKind.NotEqual:
                    // int, bool, string
                    throw new NotImplementedException();

                case BoundBinaryOperatorKind.Subtraction:
                    // int
                    ilProcessor.Emit(OpCodes.Sub);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void EmitCallExpression(ILProcessor ilProcessor, BoundCallExpression callExpression)
        {
            foreach (var argExpr in callExpression.Arguments)
                EmitExpression(ilProcessor, argExpr);

            if(_methods.TryGetValue(callExpression.Function, out var method))
            {
                ilProcessor.Emit(OpCodes.Call, method);
            }
            else
            {
                // probably a builtin
                // TODO: improve this
                switch (callExpression.Function.Name)
                {
                    // case "rnd":
                    case "print":
                        ilProcessor.Emit(OpCodes.Call, _consoleWriteLineReference);
                        break;
                    case "read":
                        ilProcessor.Emit(OpCodes.Call, _consoleReadLineReference);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

        }

        private void EmitConversionExpression(ILProcessor ilProcessor, BoundConversionExpression conversionExpression)
        {
            throw new NotImplementedException();
        }

        private void EmitUnaryExpression(ILProcessor ilProcessor, BoundUnaryExpression unaryExpression)
        {
            EmitExpression(ilProcessor, unaryExpression.Operand);
            switch (unaryExpression.Operator.Kind)
            {
                case BoundUnaryOperatorKind.Identity:
                    // no op
                    break;
                case BoundUnaryOperatorKind.Negation:
                    // TODO: is Negation and BitwiseNegation the same opcode??
                    ilProcessor.Emit(OpCodes.Neg);
                    break;
                case BoundUnaryOperatorKind.LogicalNegation:
                    ilProcessor.Emit(OpCodes.Not);
                    break;
                case BoundUnaryOperatorKind.BitwiseNegation:
                    ilProcessor.Emit(OpCodes.Neg);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void EmitUnitExpression(ILProcessor ilProcessor, BoundUnitExpression unitExpression)
        {
            // no-op
        }

        private void EmitVariableExpression(ILProcessor ilProcessor, BoundVariableExpression variableExpression)
        {
            switch (variableExpression.Variable)
            {
                case ParameterSymbol parameterSymbol:
                    ilProcessor.Emit(OpCodes.Ldarg,  parameterSymbol.Index);
                    break;

                case GlobalVariableSymbol _:
                case LocalVariableSymbol _:
                default:
                    ilProcessor.Emit(OpCodes.Ldloc, _locals[variableExpression.Variable]);
                    break;
            }
        }

        private void CreateLabel(BoundLabel boundLabel, Instruction target)
        {
            if (_labelTarget.ContainsKey(boundLabel))
            {
                // todo this shouldn't happen. diag error or throw?
                throw new InvalidProgramException($"Label target '{boundLabel.Name}' has already been assigned");
            }

            // we have two cases here:
            // 1. we have already emitted instructions and they need to be patched to target this label
            if (_branchInstructionsToPatch.TryGetValue(boundLabel, out var targetsToPatch))
            {
                foreach (var instruction in targetsToPatch)
                {
                    instruction.Operand = target;
                }

                _branchInstructionsToPatch.Remove(boundLabel);
            }
            else
            {
                // 2. the label is the first instruction so we just create the label instruction and nothing left to do
                _labelTarget[boundLabel] = target;
            }
        }

        private void SetBranchTarget(BoundLabel boundLabel, Instruction instruction)
        {
            // label exists, return the target instruction
            if (_labelTarget.TryGetValue(boundLabel, out var target))
            {
                instruction.Operand = target;
                return;
            }

            // no label yet, create a reservation to patch later
            if (_branchInstructionsToPatch.TryGetValue(boundLabel, out var list))
            {
                list.Add(instruction);
                return;
            }

            _branchInstructionsToPatch[boundLabel] = new List<Instruction> { instruction };
        }

        private TypeReference? ResolveBuiltinType(string builtinName, string metadataTypeName)
        {
            var foundTypes = FindTypesByName(metadataTypeName);

            switch (foundTypes.Length)
            {
                case 0:
                    _diagnostics.ReportBuiltinTypeNotFound(builtinName);
                    break;
                case 1:
                    return _assemblyDefinition.MainModule.ImportReference(foundTypes[0]);
                default:
                    _diagnostics.ReportAmbiguousBuiltinType(builtinName, foundTypes);
                    break;
            }

            return null;
        }

        private TypeDefinition? FindTypeByName(string typeName)
        {
            var foundTypes = FindTypesByName(typeName);

            switch (foundTypes.Length)
            {
                case 0:
                    _diagnostics.ReportTypeNotFound(typeName);
                    break;
                case 1:
                    return foundTypes[0];
                default:
                    _diagnostics.ReportAmbiguousType(typeName, foundTypes);
                    break;
            }

            return null;
        }

        MethodReference? ResolveMethod(string typeName, string methodName, string[] parameterTypeNames)
        {
            // var types = FindTypesByName(typeName);
            var type = FindTypeByName(typeName);
            if (type == null)
                return null;

            foreach (var methodDefinition in type.Methods.Where(method => method.Name == methodName))
            {
                if (methodDefinition.Parameters.Count != parameterTypeNames.Length)
                    continue;

                var matches = methodDefinition
                    .Parameters
                    .Select(p => p.ParameterType.FullName)
                    .Zip(parameterTypeNames, (methodParam, searchParamName) => methodParam == searchParamName)
                    .ToArray();

                var allParamsMatch = matches
                    .All(matches => matches);

                if (!allParamsMatch)
                {
                    continue;
                }

                return _assemblyDefinition.MainModule.ImportReference(methodDefinition);
            }

            _diagnostics.ReportRequiredMethodNotFound(typeName, methodName, parameterTypeNames);
            return null;
        }

        private TypeDefinition[] FindTypesByName(string metadataTypeName)
        {
            var foundTypes = (
                from asm in _assemblies
                from module in asm.Modules
                from type in module.Types
                where type.FullName == metadataTypeName
                select type
            ).ToArray();
            return foundTypes;
        }
    }
}