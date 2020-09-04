using System;
using System.Linq;
using Mono.Cecil;
using System.Collections.Generic;
using System.Collections.Immutable;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Emit
{
    internal class Emitter
    {
        private readonly DiagnosticBag _diagnostics = new DiagnosticBag();

        private readonly Dictionary<TypeSymbol, TypeReference> _knownTypes = new Dictionary<TypeSymbol, TypeReference>();

        private readonly Dictionary<TypeSymbol, TypeDefinition> _types = new Dictionary<TypeSymbol, TypeDefinition>();
        private readonly Dictionary<MethodSymbol, MethodDefinition> _methods = new Dictionary<MethodSymbol, MethodDefinition>();
        private readonly Dictionary<LocalVariableSymbol, VariableDefinition> _locals = new Dictionary<LocalVariableSymbol, VariableDefinition>();

        private readonly AssemblyDefinition _assemblyDefinition;

        private readonly TypeReference? _voidType;
        private readonly MethodReference? _stringConcatReference;
        private readonly MethodReference? _convertBoolToString;
        private readonly MethodReference? _convertInt32ToString;
        private readonly MethodReference? _convertToBool;
        private readonly MethodReference? _convertToInt32;
        private readonly FieldReference? _unit;

        private readonly Dictionary<FieldSymbol, FieldReference> _fields;
        private readonly Dictionary<MethodSymbol, MethodReference> _methodReferences;

        private readonly Dictionary<BoundLabel, int> _labels = new Dictionary<BoundLabel, int>();
        private readonly List<(int InstructionIndex, BoundLabel Target)> _branchInstructionsToPatch = new List<(int InstructionIndex, BoundLabel Target)>();

        private Emitter(string moduleName, ImmutableArray<AssemblyDefinition> references, Dictionary<FieldSymbol, FieldReference>? previousFields = null, Dictionary<MethodSymbol, MethodReference>? previousMethods = null)
        {
            _typeCache = (
                from asm in references
                from module in asm.Modules
                from type in module.Types
                group type by type.FullName
                into g
                select g
            ).ToDictionary(g => g.Key, g => g.ToImmutableArray());

            var assemblyName = new AssemblyNameDefinition(moduleName, new Version(1, 0));
            _assemblyDefinition = AssemblyDefinition.CreateAssembly(assemblyName, moduleName, ModuleKind.Console);

            // import known types
            var builtinTypes = new List<(TypeSymbol type, string MetadataType)>
            {
                (TypeSymbol.Any, "System.Object"),
                (TypeSymbol.Bool, "System.Boolean"),
                (TypeSymbol.Int, "System.Int32"),
                (TypeSymbol.String, "System.String"),
                (TypeSymbol.Unit, "Panther.Unit"),
            };

            foreach (var (typeSymbol, metadataType) in builtinTypes)
            {
                var typeReference = ResolveBuiltinType(typeSymbol.Name, metadataType);
                if (typeReference == null)
                    continue;

                _knownTypes.Add(typeSymbol, typeReference);
            }

            // import fields from previous compilation
            _fields = (previousFields ?? new Dictionary<FieldSymbol, FieldReference>()).Select(kv =>
                    (kv.Key, _assemblyDefinition.MainModule.ImportReference(kv.Value)))
                .ToDictionary(kv => kv.Item1, kv => kv.Item2);

            _methodReferences = (previousMethods ?? new Dictionary<MethodSymbol, MethodReference>()).Select(kv =>
                    (kv.Key, _assemblyDefinition.MainModule.ImportReference(kv.Value)))
                .ToDictionary(kv => kv.Item1, kv => kv.Item2);

            _voidType = ResolveBuiltinType("unit", "System.Void");

            _stringConcatReference = ResolveMethod("System.String", "Concat", new[] { "System.String", "System.String" });

            _convertBoolToString = ResolveMethod("System.Convert", "ToString", new[] { "System.Boolean" });
            _convertInt32ToString = ResolveMethod("System.Convert", "ToString", new[] { "System.Int32" });
            _convertToBool = ResolveMethod("System.Convert", "ToBoolean", new[] { "System.Object" });
            _convertToInt32 = ResolveMethod("System.Convert", "ToInt32", new[] { "System.Object" });
            _unit = ResolveField("Panther.Unit", "Default");
        }

        public static EmitResult Emit(BoundAssembly assembly, string moduleName, string outputPath, Dictionary<FieldSymbol, FieldReference>? previousGlobals = null, Dictionary<MethodSymbol, MethodReference>? previousMethods = null)
        {
            if (assembly.Diagnostics.Any())
                return new EmitResult(assembly.Diagnostics,
                    previousGlobals ?? new Dictionary<FieldSymbol, FieldReference>(),
                    previousMethods ?? new Dictionary<MethodSymbol, MethodReference>(), null);

            var emitter = new Emitter(moduleName, assembly.References, previousGlobals, previousMethods);
            return emitter.Emit(assembly, outputPath);
        }

        public EmitResult Emit(BoundAssembly assembly, string outputPath)
        {
            if (_diagnostics.Any())
                return new EmitResult(_diagnostics.ToImmutableArray(), _fields, _methodReferences, null);

            // ensure all functions exist first so we can reference them
            // WORKAROUND order these so that our emitter tests are consistent. is there a better way?
            // TODO: this is probably wrong now, the functions will be in scope even though they shouldn't be
            // unless we have an import
            foreach (var type in assembly.Types)
            {
                EmitTypeDeclaration(type);
            }

            // emit the function bodies now
            foreach (var type in assembly.Types)
            {
                var typeDef = _types[type];
                foreach (var (functionSignature, boundBlockExpression) in type.MethodDefinitions)
                {
                    EmitFunctionBody(typeDef, functionSignature, boundBlockExpression);
                }
            }

            if (assembly.EntryPoint != null)
            {
                _assemblyDefinition.EntryPoint = _methods[assembly.EntryPoint.Symbol];
            }

            _assemblyDefinition.Write(outputPath);

            return new EmitResult(_diagnostics.ToImmutableArray(), _fields, _methodReferences, _assemblyDefinition);
        }

        private void EmitTypeDeclaration(BoundType type)
        {
            var objectType = _knownTypes[TypeSymbol.Any];

            // TODO keep track of current namespace
            var typeDef = new TypeDefinition("", type.Name, TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Public, objectType);

            foreach (var functionSignature in type.MethodDefinitions.Keys.OrderBy(x => x.Name))
            {
                EmitFunctionDeclaration(typeDef, functionSignature);
            }

            _types[type] = typeDef;
            _assemblyDefinition.MainModule.Types.Add(typeDef);
        }

        private void EmitFunctionDeclaration(TypeDefinition declaringType, MethodSymbol method)
        {
            var type = method.ReturnType;
            var returnType = type == TypeSymbol.Unit ? _voidType : _knownTypes[type];
            var methodDefinition = new MethodDefinition(method.Name, MethodAttributes.Static | MethodAttributes.Public, returnType);
            var methodParams =
                from parameter in method.Parameters
                let paramType = _knownTypes[parameter.Type]
                select new ParameterDefinition(parameter.Name, ParameterAttributes.None, paramType);

            foreach (var p in methodParams)
                methodDefinition.Parameters.Add(p);

            _methods[method] = methodDefinition;
            _methodReferences[method] = methodDefinition;

            declaringType.Methods.Add(methodDefinition);
        }

        private void EmitFunctionBody(TypeDefinition declaringType, MethodSymbol method, BoundBlockExpression block)
        {
            var methodDefinition = _methods[method];
            var ilProcessor = methodDefinition.Body.GetILProcessor();

            _locals.Clear();
            _labels.Clear();
            _branchInstructionsToPatch.Clear();

            foreach (var statement in block.Statements)
                EmitStatement(declaringType, ilProcessor, statement);

            // only emit block's expression if its a non-unit expression or we are not void
            if (block.Expression.Kind != BoundNodeKind.UnitExpression || method.ReturnType != TypeSymbol.Unit)
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

        private void EmitStatement(TypeDefinition declaringType, ILProcessor ilProcessor, BoundStatement statement)
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
                    EmitVariableDeclarationStatement(declaringType, ilProcessor, variableDeclarationStatement);
                    break;

                case BoundAssignmentStatement assignmentStatement:
                    EmitAssignmentStatement(ilProcessor, assignmentStatement);
                    break;

                case BoundNopStatement nopStatement:
                    EmitNopStatement(ilProcessor, nopStatement);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(statement), statement.Kind.ToString());
            }
        }

        private void EmitNopStatement(ILProcessor ilProcessor, BoundNopStatement nopStatement)
        {
            ilProcessor.Emit(OpCodes.Nop);
        }

        private void EmitAssignmentStatement(ILProcessor ilProcessor, BoundAssignmentStatement assignmentStatement)
        {
            EmitExpression(ilProcessor, assignmentStatement.Expression);

            switch (assignmentStatement.Variable)
            {
                case FieldSymbol globalVariableSymbol:
                    ilProcessor.Emit(OpCodes.Stsfld, _fields[globalVariableSymbol]);
                    break;

                case LocalVariableSymbol localVariableSymbol:
                    ilProcessor.Emit(OpCodes.Stloc, _locals[localVariableSymbol]);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void EmitVariableDeclarationStatement(TypeDefinition declaringType, ILProcessor ilProcessor, BoundVariableDeclarationStatement variableDeclarationStatement)
        {
            var pantherVar = variableDeclarationStatement.Variable;
            var variableType = _knownTypes[pantherVar.Type];
            switch (pantherVar)
            {
                case FieldSymbol globalVariableSymbol:
                    // TODO: figure out reassignment in case where type changes
                    var field = declaringType.Fields.FirstOrDefault(fld => fld.Name == globalVariableSymbol.Name);
                    if (field == null)
                    {
                        field = new FieldDefinition(globalVariableSymbol.Name,
                            FieldAttributes.Public | FieldAttributes.Static,
                            variableType);

                        declaringType.Fields.Add(field);
                        _fields[globalVariableSymbol] = field;
                    }
                    EmitExpression(ilProcessor, variableDeclarationStatement.Expression);
                    ilProcessor.Emit(OpCodes.Stsfld, field);

                    break;

                case LocalVariableSymbol localVariableSymbol:
                    if (!_locals.ContainsKey(localVariableSymbol))
                    {
                        var variableDef = new VariableDefinition(variableType);
                        _locals[localVariableSymbol] = variableDef;

                        ilProcessor.Body.Variables.Add(variableDef);
                    }

                    var index = _locals[localVariableSymbol].Index;

                    EmitExpression(ilProcessor, variableDeclarationStatement.Expression);
                    ilProcessor.Emit(OpCodes.Stloc, index);

                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
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
                    throw new ArgumentOutOfRangeException(nameof(expression), expression.GetType().FullName);
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
            else if (_methodReferences.TryGetValue(callExpression.Method, out var methodRef))
            {
                ilProcessor.Emit(OpCodes.Call, methodRef);
            }
            else
            {
                // search for the method in Predef for now
                var methodReference = ResolveMethod("Panther.Predef", callExpression.Method.Name,
                    callExpression.Method.Parameters.Select(p => _knownTypes[p.Type].FullName).ToArray());

                if (methodReference != null)
                {
                    ilProcessor.Emit(OpCodes.Call, methodReference);
                    return;
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
                    ilProcessor.Emit(OpCodes.Box, _knownTypes[TypeSymbol.Bool]);
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
                case FieldSymbol globalVariableSymbol:
                    ilProcessor.Emit(OpCodes.Ldsfld, _fields[globalVariableSymbol]);
                    break;

                case ParameterSymbol parameterSymbol:
                    ilProcessor.Emit(OpCodes.Ldarg, parameterSymbol.Index);
                    break;

                case LocalVariableSymbol localVariableSymbol:
                    ilProcessor.Emit(OpCodes.Ldloc, _locals[localVariableSymbol]);
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

        private readonly Dictionary<string, ImmutableArray<TypeDefinition>> _typeCache;

        private ImmutableArray<TypeDefinition> FindTypesByName(string metadataTypeName)
        {
            if (_typeCache.TryGetValue(metadataTypeName, out var results))
            {
                return results;
            }

            return ImmutableArray<TypeDefinition>.Empty;
        }
    }
}