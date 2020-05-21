using System;
using System.Linq;
using Mono.Cecil;
using System.Collections.Generic;
using System.Collections.Immutable;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Symbols;
using Panther.StdLib;

namespace Panther.CodeAnalysis.Emit
{
    internal class Emitter
    {
        private readonly DiagnosticBag _diagnostics = new DiagnosticBag();

        private readonly Dictionary<TypeSymbol, TypeReference> _knownTypes = new Dictionary<TypeSymbol, TypeReference>();

        private readonly Dictionary<MethodSymbol, MethodDefinition> _methods = new Dictionary<MethodSymbol, MethodDefinition>();
        private readonly AssemblyDefinition _assemblyDefinition;
        private readonly List<AssemblyDefinition> _assemblies = new List<AssemblyDefinition>();
        private TypeDefinition _typeDef;

        private readonly TypeReference? _voidType;
        private readonly MethodReference? _consoleWriteLineReference;
        private readonly MethodReference? _consoleReadLineReference;
        private readonly MethodReference? _stringConcatReference;
        private readonly MethodReference? _convertBoolToString;
        private readonly MethodReference? _convertInt32ToString;
        private readonly MethodReference? _convertToBool;
        private readonly MethodReference? _convertToInt32;
        private readonly FieldReference? _unit;

        private readonly Dictionary<VariableSymbol, VariableDefinition> _locals = new Dictionary<VariableSymbol, VariableDefinition>();

        private readonly Dictionary<BoundLabel, int> _labels = new Dictionary<BoundLabel, int>();
        private readonly List<(int InstructionIndex, BoundLabel Target)> _branchInstructionsToPatch = new List<(int InstructionIndex, BoundLabel Target)>();

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
                (TypeSymbol.Unit, typeof(Unit)),
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

            _voidType = ResolveBuiltinType("unit", typeof(void).FullName);

            _consoleWriteLineReference = ResolveMethod("System.Console", "WriteLine", new[] { "System.String" });
            _consoleReadLineReference = ResolveMethod("System.Console", "ReadLine", Array.Empty<string>());
            _consoleReadLineReference = ResolveMethod("System.Console", "ReadLine", Array.Empty<string>());
            _stringConcatReference = ResolveMethod("System.String", "Concat", new[] { "System.String", "System.String" });

            _convertBoolToString = ResolveMethod("System.Convert", "ToString", new[] { "System.Boolean" });
            _convertInt32ToString = ResolveMethod("System.Convert", "ToString", new[] { "System.Int32" });
            _convertToBool = ResolveMethod("System.Convert", "ToBoolean", new[] { "System.Object" });
            _convertToInt32 = ResolveMethod("System.Convert", "ToInt32", new[] { "System.Object" });
            _unit = ResolveField("Panther.StdLib.Unit", "Default");

            var objectType = _knownTypes[TypeSymbol.Any];
            _typeDef = new TypeDefinition("", "Program", TypeAttributes.Abstract | TypeAttributes.Sealed, objectType);
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

            _assemblyDefinition.MainModule.Types.Add(_typeDef);

            // ensure all functions exist first so we can reference them
            // WORKAROUND order these so that our emitter tests are consistent. is there a better way?
            foreach (var functionSignature in program.Functions.Keys.OrderBy(x => x.Name))
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

        private void EmitFunctionDeclaration(MethodSymbol method)
        {
            var type = method.ReturnType;
            var returnType = type == TypeSymbol.Unit ? _voidType : _knownTypes[type];
            var methodDefinition = new MethodDefinition(method.Name, MethodAttributes.Static | MethodAttributes.Private, returnType);
            var methodParams =
                from parameter in method.Parameters
                let paramType = _knownTypes[parameter.Type]
                select new ParameterDefinition(parameter.Name, ParameterAttributes.None, paramType);

            foreach (var p in methodParams)
                methodDefinition.Parameters.Add(p);

            _methods[method] = methodDefinition;

            _typeDef.Methods.Add(methodDefinition);
        }

        private void EmitFunctionBody(MethodSymbol method, BoundBlockExpression block)
        {
            var methodDefinition = _methods[method];
            var ilProcessor = methodDefinition.Body.GetILProcessor();

            _locals.Clear();
            _labels.Clear();
            _branchInstructionsToPatch.Clear();

            foreach (var statement in block.Statements)
                EmitStatement(ilProcessor, statement);

            // only emit block's expression if its a non-unit expression or we are not void
            if (block.Expression != BoundUnitExpression.Default || method.ReturnType != TypeSymbol.Unit)
            {
                // emit expression
                EmitExpression(ilProcessor, block.Expression);

                // pop non-unit expressions off the stack for unit functions
                if (block.Expression.Type != TypeSymbol.Unit && method.ReturnType == TypeSymbol.Unit)
                    ilProcessor.Emit(OpCodes.Pop);
            }

            ilProcessor.Emit(OpCodes.Ret);

            foreach (var (patchIndex, targetLabel) in _branchInstructionsToPatch)
            {
                var targetIndex = _labels[targetLabel];
                var targetInstruction = ilProcessor.Body.Instructions[targetIndex];
                var patchInstruction = ilProcessor.Body.Instructions[patchIndex];
                patchInstruction.Operand = targetInstruction;
            }

            methodDefinition.Body.OptimizeMacros();
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

                case BoundAssignmentStatement assignmentStatement:
                    EmitAssignmentStatement(ilProcessor, assignmentStatement);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(statement), statement.Kind.ToString());
            }
        }

        private void EmitAssignmentStatement(ILProcessor ilProcessor, BoundAssignmentStatement assignmentStatement)
        {
            EmitExpression(ilProcessor, assignmentStatement.Expression);
            ilProcessor.Emit(OpCodes.Stloc, _locals[assignmentStatement.Variable]);
        }

        private void EmitVariableDeclarationStatement(ILProcessor ilProcessor, BoundVariableDeclarationStatement variableDeclarationStatement)
        {
            var pantherVar = variableDeclarationStatement.Variable;
            var variableType = _knownTypes[pantherVar.Type];
            var variableDef = new VariableDefinition(variableType);
            _locals[pantherVar] = variableDef;
            var index = ilProcessor.Body.Variables.Count;
            ilProcessor.Body.Variables.Add(variableDef);

            EmitExpression(ilProcessor, variableDeclarationStatement.Expression);
            ilProcessor.Emit(OpCodes.Stloc, index);
        }

        private void EmitLabelStatement(ILProcessor ilProcessor, BoundLabelStatement labelStatement)
        {
            _labels[labelStatement.BoundLabel] = ilProcessor.Body.Instructions.Count;
        }

        private void EmitGotoStatement(ILProcessor ilProcessor, BoundGotoStatement gotoStatement)
        {
            _branchInstructionsToPatch.Add((ilProcessor.Body.Instructions.Count, gotoStatement.BoundLabel));
            ilProcessor.Emit(OpCodes.Br, Instruction.Create(OpCodes.Nop));
        }

        private void EmitConditionalGotoStatement(ILProcessor ilProcessor, BoundConditionalGotoStatement conditionalGotoStatement)
        {
            EmitExpression(ilProcessor, conditionalGotoStatement.Condition);

            var op = conditionalGotoStatement.JumpIfTrue ? OpCodes.Brtrue : OpCodes.Brfalse;
            _branchInstructionsToPatch.Add((ilProcessor.Body.Instructions.Count, conditionalGotoStatement.BoundLabel));
            ilProcessor.Emit(op, Instruction.Create(OpCodes.Nop));
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
            if (expression.ConstantValue != null)
            {
                EmitConstantExpression(ilProcessor, expression, expression.ConstantValue);
                return;
            }

            switch (expression)
            {
                case BoundBinaryExpression binaryExpression:
                    EmitBinaryExpression(ilProcessor, binaryExpression);
                    break;

                case BoundCallExpression callExpression:
                    EmitCallExpression(ilProcessor, callExpression);
                    break;

                case BoundConversionExpression conversionExpression:
                    EmitConversionExpression(ilProcessor, conversionExpression);
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

        private void EmitConstantExpression(ILProcessor ilProcessor, BoundExpression node, BoundConstant constant)
        {
            if (node.Type == TypeSymbol.Bool)
            {
                ilProcessor.Emit((bool)constant.Value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            }
            else if (node.Type == TypeSymbol.Int)
            {
                ilProcessor.Emit(OpCodes.Ldc_I4, (int)constant.Value);
            }
            else if (node.Type == TypeSymbol.String)
            {
                ilProcessor.Emit(OpCodes.Ldstr, (string)constant.Value);
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
                    ilProcessor.Emit(OpCodes.Cgt);
                    break;

                case BoundBinaryOperatorKind.GreaterThanOrEqual:
                    // int
                    // convert a >= b to !(a < b) or  (a < b) == false
                    ilProcessor.Emit(OpCodes.Clt);
                    ilProcessor.Emit(OpCodes.Ldc_I4_0);
                    ilProcessor.Emit(OpCodes.Ceq);
                    break;

                case BoundBinaryOperatorKind.LessThan:
                    // int
                    ilProcessor.Emit(OpCodes.Clt);
                    break;

                case BoundBinaryOperatorKind.LessThanOrEqual:
                    // int
                    ilProcessor.Emit(OpCodes.Cgt);
                    ilProcessor.Emit(OpCodes.Ldc_I4_0);
                    ilProcessor.Emit(OpCodes.Ceq);
                    break;

                case BoundBinaryOperatorKind.LogicalAnd:
                    // bool
                    ilProcessor.Emit(OpCodes.And);
                    break;

                case BoundBinaryOperatorKind.LogicalOr:
                    // bool
                    ilProcessor.Emit(OpCodes.Or);
                    break;

                case BoundBinaryOperatorKind.Multiplication:
                    // int
                    ilProcessor.Emit(OpCodes.Mul);
                    break;

                case BoundBinaryOperatorKind.NotEqual:
                    // int, bool, string
                    ilProcessor.Emit(OpCodes.Ceq);
                    ilProcessor.Emit(OpCodes.Ldc_I4_0);
                    ilProcessor.Emit(OpCodes.Ceq);
                    break;

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

            if (_methods.TryGetValue(callExpression.Method, out var method))
            {
                ilProcessor.Emit(OpCodes.Call, method);
            }
            else
            {
                // probably a builtin
                // TODO: improve this
                switch (callExpression.Method.Name)
                {
                    // case "rnd":
                    case "println":
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
            EmitExpression(ilProcessor, conversionExpression.Expression);

            var fromType = conversionExpression.Expression.Type;
            var toType = conversionExpression.Type;
            if (toType == TypeSymbol.String)
            {
                if (fromType == TypeSymbol.Bool)
                {
                    ilProcessor.Emit(OpCodes.Call, _convertBoolToString);
                    return;
                }

                if (fromType == TypeSymbol.Int)
                {
                    ilProcessor.Emit(OpCodes.Call, _convertInt32ToString);
                    return;
                }
            }

            if (toType == TypeSymbol.Any)
            {
                if (fromType == TypeSymbol.Bool)
                {
                    ilProcessor.Emit(OpCodes.Box, _knownTypes[TypeSymbol.Int]);
                    return;
                }

                if (fromType == TypeSymbol.Int)
                {
                    ilProcessor.Emit(OpCodes.Box, _knownTypes[TypeSymbol.Int]);
                    return;
                }

                if (fromType == TypeSymbol.String)
                {
                    // no conversion required
                    return;
                }

                if (fromType == TypeSymbol.Unit)
                {
                    // pop the expression if it was a ()
                    ilProcessor.Emit(OpCodes.Pop);
                    ilProcessor.Emit(OpCodes.Ldsfld, _unit);
                    return;
                }
            }

            if (toType == TypeSymbol.Int)
            {
                if (fromType == TypeSymbol.String)
                {
                    ilProcessor.Emit(OpCodes.Call, _convertToInt32);
                    return;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(conversionExpression), $"Could not find conversion from '{fromType}' to '{toType}'");
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
            ilProcessor.Emit(OpCodes.Ldsfld, _unit);
        }

        private void EmitVariableExpression(ILProcessor ilProcessor, BoundVariableExpression variableExpression)
        {
            switch (variableExpression.Variable)
            {
                case ParameterSymbol parameterSymbol:
                    ilProcessor.Emit(OpCodes.Ldarg, parameterSymbol.Index);
                    break;

                case GlobalVariableSymbol _:
                case LocalVariableSymbol _:
                default:
                    ilProcessor.Emit(OpCodes.Ldloc, _locals[variableExpression.Variable]);
                    break;
            }
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

        private FieldReference? ResolveField(string typeName, string fieldName)
        {
            var type = FindTypeByName(typeName);
            if (type == null)
                return null;

            foreach (var fieldDefinition in type.Fields.Where(field => field.Name == fieldName))
            {
                return _assemblyDefinition.MainModule.ImportReference(fieldDefinition);
            }

            _diagnostics.ReportRequiredFieldNotFound(typeName, fieldName);
            return null;
        }

        private MethodReference? ResolveMethod(string typeName, string methodName, string[] parameterTypeNames)
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