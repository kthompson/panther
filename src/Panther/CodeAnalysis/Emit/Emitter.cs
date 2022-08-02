using System;
using System.Linq;
using Mono.Cecil;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Text;
using OpCode = System.Reflection.Emit.OpCode;
using Type = Panther.CodeAnalysis.Symbols.Type;

namespace Panther.CodeAnalysis.Emit;

internal class Emitter
{
    private readonly DiagnosticBag _diagnostics = new();

    private readonly Dictionary<Symbol, TypeReference> _knownTypes = new();
    private readonly Dictionary<Symbol, TypeDefinition> _types = new();
    private readonly Dictionary<Symbol, MethodDefinition> _methods = new();
    private readonly Dictionary<Symbol, VariableDefinition> _locals = new();
    private readonly Dictionary<Symbol, FieldReference> _fields = new();
    private Symbol? _currentType;

    private readonly AssemblyDefinition _assemblyDefinition;

    private readonly TypeReference? _voidType;
    private readonly MethodReference? _stringConcatReference;
    private readonly MethodReference? _convertBoolToString;
    private readonly MethodReference? _convertInt32ToString;
    private readonly MethodReference? _convertCharToString;
    private readonly MethodReference? _convertToBool;
    private readonly MethodReference? _convertToInt32;
    private readonly FieldReference? _unit;

    private readonly Dictionary<Symbol, FieldReference> _globals;
    private readonly Dictionary<Symbol, MethodReference> _methodReferences;

    private readonly Dictionary<TypedLabel, int> _labels = new();
    private readonly List<(int InstructionIndex, TypedLabel Target)> _branchInstructionsToPatch =
        new();

    private Emitter(
        string moduleName,
        ImmutableArray<AssemblyDefinition> references,
        Dictionary<Symbol, FieldReference>? previousFields = null,
        Dictionary<Symbol, MethodReference>? previousMethods = null
    )
    {
        _typeCache = (
            from asm in references
            from module in asm.Modules
            from type in module.Types
            group type by type.FullName into g
            select g
        ).ToDictionary(g => g.Key, g => g.ToImmutableArray());

        var assemblyName = new AssemblyNameDefinition(moduleName, new Version(1, 0));
        _assemblyDefinition = AssemblyDefinition.CreateAssembly(
            assemblyName,
            moduleName,
            ModuleKind.Console
        );

        // import known types
        var builtinTypes = new List<(TypeSymbol type, string MetadataType)>
        {
            (TypeSymbol.Any, "System.Object"),
            (TypeSymbol.Bool, "System.Boolean"),
            (TypeSymbol.Char, "System.Char"),
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
        _globals = (previousFields ?? new Dictionary<Symbol, FieldReference>())
            .Select(kv => (kv.Key, _assemblyDefinition.MainModule.ImportReference(kv.Value)))
            .ToDictionary(kv => kv.Item1, kv => kv.Item2);

        _methodReferences = (previousMethods ?? new Dictionary<Symbol, MethodReference>())
            .Select(kv => (kv.Key, _assemblyDefinition.MainModule.ImportReference(kv.Value)))
            .ToDictionary(kv => kv.Item1, kv => kv.Item2);

        _voidType = ResolveBuiltinType(TypeSymbol.Unit.Name, "System.Void");

        _stringConcatReference = ResolveMethod(
            "System.String",
            "Concat",
            new[] { "System.String", "System.String" }
        );

        _convertBoolToString = ResolveMethod(
            "System.Convert",
            "ToString",
            new[] { "System.Boolean" }
        );
        _convertInt32ToString = ResolveMethod(
            "System.Convert",
            "ToString",
            new[] { "System.Int32" }
        );
        _convertCharToString = ResolveMethod("System.Convert", "ToString", new[] { "System.Char" });
        _convertToBool = ResolveMethod("System.Convert", "ToBoolean", new[] { "System.Object" });
        _convertToInt32 = ResolveMethod("System.Convert", "ToInt32", new[] { "System.Object" });
        _unit = ResolveField("Panther.Unit", "Default");
    }

    private TypeReference LookupType(Symbol symbol) =>
        _knownTypes.TryGetValue(symbol, out var knownType) ? knownType : _types[symbol];

    public static EmitResult Emit(
        TypedAssembly assembly,
        string moduleName,
        string outputPath,
        Dictionary<Symbol, FieldReference>? previousGlobals = null,
        Dictionary<Symbol, MethodReference>? previousMethods = null
    )
    {
        if (assembly.Diagnostics.Any())
            return new EmitResult(
                assembly.Diagnostics,
                previousGlobals ?? new Dictionary<Symbol, FieldReference>(),
                previousMethods ?? new Dictionary<Symbol, MethodReference>(),
                null,
                null
            );

        var emitter = new Emitter(
            moduleName,
            assembly.References,
            previousGlobals,
            previousMethods
        );
        return emitter.Emit(assembly, outputPath);
    }

    public EmitResult Emit(TypedAssembly assembly, string outputPath)
    {
        if (_diagnostics.Any())
            return new EmitResult(
                _diagnostics.ToImmutableArray(),
                _globals,
                _methodReferences,
                null,
                null
            );

        // ensure all functions exist first so we can reference them
        // WORKAROUND order these so that our emitter tests are consistent. is there a better way?
        IterateTypes(assembly, EmitTypeDeclaration);

        // emit fields now
        IterateTypes(assembly, EmitFields);

        // emit the function bodies now
        IterateTypes(
            assembly,
            type =>
            {
                var typeDef = _types[type];
                _currentType = type;

                foreach (var functionSignature in type.Methods)
                {
                    var boundBlockExpression = assembly.MethodDefinitions[functionSignature];
                    EmitFunctionBody(typeDef, functionSignature, boundBlockExpression);
                }
            }
        );

        if (assembly.EntryPoint != null)
        {
            _assemblyDefinition.EntryPoint = _methods[assembly.EntryPoint.Symbol];
        }

        _assemblyDefinition.Write(outputPath);

        return new EmitResult(
            _diagnostics.ToImmutableArray(),
            _globals,
            _methodReferences,
            _assemblyDefinition,
            outputPath
        );
    }

    private void IterateTypes(TypedAssembly assembly, Action<Symbol> action)
    {
        foreach (var type in assembly.RootSymbol.Types.Where(symbol => !symbol.IsImport))
        {
            _currentType = type;
            action(type);
        }
    }

    private void EmitFields(Symbol type)
    {
        var typeDef = _types[type];
        // _fields.Clear();
        var fieldAttributes =
            (type.Flags & SymbolFlags.Object) != 0
                ? FieldAttributes.Public | FieldAttributes.Static
                : FieldAttributes.Public;
        var defaultType = type.Name == "$Program";
        foreach (var fieldMember in type.Fields)
        {
            var fieldType = LookupType(fieldMember.Type.Symbol);
            var field = new FieldDefinition(fieldMember.Name, fieldAttributes, fieldType);
            _fields[fieldMember] = field;
            if (defaultType)
            {
                if (!_globals.ContainsKey(fieldMember))
                {
                    _globals[fieldMember] = field;
                }
                else
                {
                    throw new InvalidOperationException($"_globals[{fieldMember}] exists");
                }
            }

            typeDef.Fields.Add(field);
        }
    }

    private void EmitTypeDeclaration(Symbol type)
    {
        var objectType = _knownTypes[TypeSymbol.Any];

        // TODO keep track of current namespace
        var typeDef = new TypeDefinition(
            "",
            type.Name,
            TypeAttributes.Sealed | TypeAttributes.Public,
            objectType
        );

        // PERF: this order by isn't necessary but its here so that tests pass
        foreach (var functionSignature in type.Methods.OrderBy(x => x.Name))
        {
            EmitFunctionDeclaration(typeDef, functionSignature);
        }

        _types[type] = typeDef;
        _assemblyDefinition.MainModule.Types.Add(typeDef);
    }

    private void EmitFunctionDeclaration(TypeDefinition typeDef, Symbol method)
    {
        var type = method.ReturnType;
        var returnType = type == Type.Unit ? _voidType : LookupType(type.Symbol);
        var isStatic = method.IsStatic;

        var methodAttributes = MethodAttributes.Public;
        if (method.IsConstructor)
        {
            methodAttributes |=
                MethodAttributes.SpecialName
                | MethodAttributes.RTSpecialName
                | MethodAttributes.HideBySig;

            if (isStatic)
            {
                methodAttributes |= MethodAttributes.Private;
                methodAttributes ^= MethodAttributes.Public;
            }
        }

        if (isStatic)
            methodAttributes |= MethodAttributes.Static;

        var methodDefinition = new MethodDefinition(method.Name, methodAttributes, returnType);
        var methodParams =
            from parameter in method.Parameters
            let paramType = LookupType(parameter.Type.Symbol)
            select new ParameterDefinition(parameter.Name, ParameterAttributes.None, paramType);

        if (!isStatic)
        {
            methodDefinition.HasThis = true;
        }

        foreach (var p in methodParams)
            methodDefinition.Parameters.Add(p);

        _methods[method] = methodDefinition;
        _methodReferences[method] = methodDefinition;

        typeDef.Methods.Add(methodDefinition);
    }

    private void EmitFunctionBody(
        TypeDefinition declaringType,
        Symbol method,
        TypedBlockExpression block
    )
    {
        var methodDefinition = _methods[method];
        var ilProcessor = methodDefinition.Body.GetILProcessor();

        _locals.Clear();
        _labels.Clear();
        _branchInstructionsToPatch.Clear();

        foreach (var statement in block.Statements)
            EmitStatement(declaringType, ilProcessor, statement);

        // only emit block's expression if its a non-unit expression or we are not void
        if (block.Expression.Kind != TypedNodeKind.UnitExpression || method.ReturnType != Type.Unit)
        {
            // emit expression
            EmitExpression(ilProcessor, block.Expression);

            // pop non-unit expressions off the stack for unit functions
            if (block.Expression.Type != Type.Unit && method.ReturnType == Type.Unit)
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
        methodDefinition.Body.InitLocals = _locals.Any();
    }

    private void EmitStatement(
        TypeDefinition declaringType,
        ILProcessor ilProcessor,
        TypedStatement statement
    )
    {
        switch (statement)
        {
            case TypedConditionalGotoStatement conditionalGotoStatement:
                EmitConditionalGotoStatement(ilProcessor, conditionalGotoStatement);
                break;

            case TypedExpressionStatement expressionStatement:
                EmitExpressionStatement(ilProcessor, expressionStatement);
                break;

            case TypedGotoStatement gotoStatement:
                EmitGotoStatement(ilProcessor, gotoStatement);
                break;

            case TypedLabelStatement labelStatement:
                EmitLabelStatement(ilProcessor, labelStatement);
                break;

            case TypedVariableDeclarationStatement variableDeclarationStatement:
                EmitVariableDeclarationStatement(ilProcessor, variableDeclarationStatement);
                break;

            case TypedAssignmentStatement assignmentStatement:
                EmitAssignmentStatement(ilProcessor, assignmentStatement);
                break;

            case TypedNopStatement nopStatement:
                EmitNopStatement(ilProcessor, nopStatement);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(statement), statement.Kind.ToString());
        }
    }

    private void EmitNopStatement(ILProcessor ilProcessor, TypedNopStatement nopStatement)
    {
        ilProcessor.Emit(OpCodes.Nop);
    }

    private void EmitAssignmentStatement(
        ILProcessor ilProcessor,
        TypedAssignmentStatement assignmentStatement
    )
    {
        var current = assignmentStatement.Left;

        // check for simple cases first
        // local = expr
        if (current is TypedVariableExpression(_, var variableSymbol))
        {
            EmitExpression(ilProcessor, assignmentStatement.Right);
            ilProcessor.Emit(OpCodes.Stloc, _locals[variableSymbol]);
            return;
        }

        // this.field = expr | Object.field = expr
        if (current is TypedFieldExpression(_, null, var fieldSymbol))
        {
            // field is local to the current type
            var fieldReference = _fields[fieldSymbol];
            var field = fieldReference.Resolve();
            var isStatic = (field.Attributes & FieldAttributes.Static) != 0;
            if (isStatic)
            {
                // load RHS to stack
                EmitExpression(ilProcessor, assignmentStatement.Right);
                ilProcessor.Emit(OpCodes.Stsfld, fieldReference);
                return;
            }

            ilProcessor.Emit(OpCodes.Ldarg_0); // load `this`
            EmitExpression(ilProcessor, assignmentStatement.Right);

            // perform: this.fieldReference = value
            ilProcessor.Emit(OpCodes.Stfld, fieldReference);

            return;
        }

        if (current is TypedFieldExpression(_, var leftOfField, var variable))
        {
            // field is local to the current type
            var field = _fields[variable].Resolve();
            var isStatic = (field.Attributes & FieldAttributes.Static) != 0;
            if (isStatic)
            {
                EmitExpression(ilProcessor, assignmentStatement.Right);
                ilProcessor.Emit(OpCodes.Stsfld, field);
            }
            else if (leftOfField == null)
            {
                ilProcessor.Emit(OpCodes.Ldarg_0); // load `this`
                EmitExpression(ilProcessor, assignmentStatement.Right);
                ilProcessor.Emit(OpCodes.Stfld, field);
            }
            else
            {
                EmitExpression(ilProcessor, leftOfField);
                EmitExpression(ilProcessor, assignmentStatement.Right);
                ilProcessor.Emit(OpCodes.Stfld, field);
            }
        }
        else
        {
            throw new ArgumentOutOfRangeException();
        }
    }

    private void EmitVariableDeclarationStatement(
        ILProcessor ilProcessor,
        TypedVariableDeclarationStatement variableDeclarationStatement
    )
    {
        var pantherVar = variableDeclarationStatement.Variable;
        var variableType = LookupType(pantherVar.Type.Symbol);
        if (pantherVar.IsField)
        {
            // TODO: figure out reassignment in case where type changes
            var field = _fields[pantherVar];
            if (variableDeclarationStatement.Expression != null)
            {
                EmitExpression(ilProcessor, variableDeclarationStatement.Expression);
                ilProcessor.Emit(pantherVar.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, field);
            }

            return;
        }

        switch (pantherVar)
        {
            case { IsLocal: true }
            or { IsParameter: true }:
            {
                if (!_locals.ContainsKey(pantherVar))
                {
                    var variableDef = new VariableDefinition(variableType);
                    _locals[pantherVar] = variableDef;

                    ilProcessor.Body.Variables.Add(variableDef);
                }

                var index = _locals[pantherVar].Index;

                if (variableDeclarationStatement.Expression != null)
                {
                    EmitExpression(ilProcessor, variableDeclarationStatement.Expression);
                    ilProcessor.Emit(OpCodes.Stloc, index);
                }

                break;
            }
            default:

                throw new ArgumentOutOfRangeException();
        }
    }

    private void EmitLabelStatement(ILProcessor ilProcessor, TypedLabelStatement labelStatement)
    {
        _labels[labelStatement.TypedLabel] = ilProcessor.Body.Instructions.Count;
    }

    private void EmitGotoStatement(ILProcessor ilProcessor, TypedGotoStatement gotoStatement)
    {
        _branchInstructionsToPatch.Add(
            (ilProcessor.Body.Instructions.Count, gotoStatement.TypedLabel)
        );
        ilProcessor.Emit(OpCodes.Br, Instruction.Create(OpCodes.Nop));
    }

    private void EmitConditionalGotoStatement(
        ILProcessor ilProcessor,
        TypedConditionalGotoStatement conditionalGotoStatement
    )
    {
        EmitExpression(ilProcessor, conditionalGotoStatement.Condition);

        var op = conditionalGotoStatement.JumpIfTrue ? OpCodes.Brtrue : OpCodes.Brfalse;
        _branchInstructionsToPatch.Add(
            (ilProcessor.Body.Instructions.Count, conditionalGotoStatement.TypedLabel)
        );
        ilProcessor.Emit(op, Instruction.Create(OpCodes.Nop));
    }

    private void EmitExpressionStatement(
        ILProcessor ilProcessor,
        TypedExpressionStatement expressionStatement
    )
    {
        EmitExpression(ilProcessor, expressionStatement.Expression);

        // pop non-unit expressions off the stack as their result wont be used
        // unit expression have no result on the stack
        if (expressionStatement.Expression.Type != Type.Unit)
            ilProcessor.Emit(OpCodes.Pop);
    }

    private void EmitExpression(ILProcessor ilProcessor, TypedExpression expression)
    {
        var constant = ConstantFolding.Fold(expression);
        if (constant != null)
        {
            EmitConstantExpression(ilProcessor, expression, constant);
            return;
        }

        switch (expression)
        {
            case TypedBinaryExpression binaryExpression:
                EmitBinaryExpression(ilProcessor, binaryExpression);
                break;

            case TypedCallExpression callExpression:
                EmitCallExpression(ilProcessor, callExpression);
                break;

            case TypedConversionExpression conversionExpression:
                EmitConversionExpression(ilProcessor, conversionExpression);
                break;

            case TypedFieldExpression fieldExpression:
                EmitFieldExpression(ilProcessor, fieldExpression);
                break;

            case TypedNewExpression newExpression:
                EmitNewExpression(ilProcessor, newExpression);
                break;

            case TypedUnaryExpression unaryExpression:
                EmitUnaryExpression(ilProcessor, unaryExpression);
                break;

            case TypedUnitExpression unitExpression:
                EmitUnitExpression(ilProcessor, unitExpression);
                break;

            case TypedVariableExpression variableExpression:
                EmitVariableExpression(ilProcessor, variableExpression);
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(expression),
                    expression.GetType().FullName
                );
        }
    }

    private void EmitNewExpression(ILProcessor ilProcessor, TypedNewExpression node)
    {
        foreach (var argExpr in node.Arguments)
            EmitExpression(ilProcessor, argExpr);

        if (_methods.TryGetValue(node.Constructor, out var method))
        {
            ilProcessor.Emit(OpCodes.Newobj, method);
        }
        else if (_methodReferences.TryGetValue(node.Constructor, out var methodRef))
        {
            ilProcessor.Emit(OpCodes.Newobj, methodRef);
        }
        else
        {
            throw new ArgumentOutOfRangeException();
        }
    }

    private void EmitConstantExpression(
        ILProcessor ilProcessor,
        TypedExpression node,
        TypedConstant constant
    )
    {
        if (node.Type == Type.Bool)
        {
            ilProcessor.Emit((bool)constant.Value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
        }
        else if (node.Type == Type.Int)
        {
            ilProcessor.Emit(OpCodes.Ldc_I4, (int)constant.Value);
        }
        else if (node.Type == Type.String)
        {
            ilProcessor.Emit(OpCodes.Ldstr, (string)constant.Value);
        }
        else if (node.Type == Type.Char)
        {
            ilProcessor.Emit(OpCodes.Ldc_I4, (int)(char)constant.Value);
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    private void EmitBinaryExpression(
        ILProcessor ilProcessor,
        TypedBinaryExpression binaryExpression
    )
    {
        EmitExpression(ilProcessor, binaryExpression.Left);
        EmitExpression(ilProcessor, binaryExpression.Right);

        var @operator = binaryExpression.Operator;
        switch (@operator.Kind)
        {
            case TypedBinaryOperatorKind.Addition:
                // string or int
                if (@operator.LeftType == Type.String)
                {
                    ilProcessor.Emit(OpCodes.Call, _stringConcatReference);
                }
                else if (@operator.LeftType == Type.Int)
                {
                    ilProcessor.Emit(OpCodes.Add);
                }
                else
                {
                    throw new NotImplementedException();
                }

                break;

            case TypedBinaryOperatorKind.BitwiseAnd:
                // int
                ilProcessor.Emit(OpCodes.And);
                break;

            case TypedBinaryOperatorKind.BitwiseOr:
                // int
                ilProcessor.Emit(OpCodes.Or);
                break;

            case TypedBinaryOperatorKind.BitwiseXor:
                // int
                ilProcessor.Emit(OpCodes.Xor);
                break;

            case TypedBinaryOperatorKind.Division:
                // int
                ilProcessor.Emit(OpCodes.Div);
                break;

            case TypedBinaryOperatorKind.Equal:
                // int, bool, string
                ilProcessor.Emit(OpCodes.Ceq);
                break;

            case TypedBinaryOperatorKind.GreaterThan:
                // int
                ilProcessor.Emit(OpCodes.Cgt);
                break;

            case TypedBinaryOperatorKind.GreaterThanOrEqual:
                // int
                // convert a >= b to !(a < b) or  (a < b) == false
                ilProcessor.Emit(OpCodes.Clt);
                ilProcessor.Emit(OpCodes.Ldc_I4_0);
                ilProcessor.Emit(OpCodes.Ceq);
                break;

            case TypedBinaryOperatorKind.LessThan:
                // int
                ilProcessor.Emit(OpCodes.Clt);
                break;

            case TypedBinaryOperatorKind.LessThanOrEqual:
                // int
                ilProcessor.Emit(OpCodes.Cgt);
                ilProcessor.Emit(OpCodes.Ldc_I4_0);
                ilProcessor.Emit(OpCodes.Ceq);
                break;

            case TypedBinaryOperatorKind.LogicalAnd:
                // bool
                ilProcessor.Emit(OpCodes.And);
                break;

            case TypedBinaryOperatorKind.LogicalOr:
                // bool
                ilProcessor.Emit(OpCodes.Or);
                break;

            case TypedBinaryOperatorKind.Multiplication:
                // int
                ilProcessor.Emit(OpCodes.Mul);
                break;

            case TypedBinaryOperatorKind.NotEqual:
                // int, bool, string
                ilProcessor.Emit(OpCodes.Ceq);
                ilProcessor.Emit(OpCodes.Ldc_I4_0);
                ilProcessor.Emit(OpCodes.Ceq);
                break;

            case TypedBinaryOperatorKind.Subtraction:
                // int
                ilProcessor.Emit(OpCodes.Sub);
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void EmitCallExpression(ILProcessor ilProcessor, TypedCallExpression callExpression)
    {
        if (
            callExpression.Expression != null
            && callExpression.Expression is not TypedTypeExpression
        )
        {
            EmitExpression(ilProcessor, callExpression.Expression);
        }

        foreach (var argExpr in callExpression.Arguments)
            EmitExpression(ilProcessor, argExpr);

        var methodSymbol = callExpression.Method;
        if (_methods.TryGetValue(methodSymbol, out var method))
        {
            ilProcessor.Emit(OpCodes.Call, method);
            return;
        }

        var methodReference = ResolveMethod(methodSymbol);
        if (methodReference != null)
        {
            ilProcessor.Emit(OpCodes.Call, methodReference);
            return;
        }

        throw new InvalidProgramException();
    }

    private MethodReference? ResolveMethod(Symbol methodSymbol)
    {
        if (_methodReferences.TryGetValue(methodSymbol, out var method))
        {
            return method;
        }

        var parameterTypeNames = methodSymbol.Parameters
            .Select(p => LookupType(p.Type.Symbol).FullName)
            .ToArray();
        var methodName = methodSymbol.Name;

        // convert builtin types to real types ie `string` to `System.String`
        var methodOwnerFullName = _knownTypes.TryGetValue(methodSymbol.Owner, out var knownType)
            ? knownType.FullName
            : methodSymbol.Owner.FullName;
        var methodReference =
            ResolveMethod(methodOwnerFullName, methodName, parameterTypeNames, false)
            ?? ResolveMethod("Panther.Predef", methodName, parameterTypeNames, false);

        if (methodReference != null)
        {
            _methodReferences[methodSymbol] = methodReference;
        }
        else
        {
            _diagnostics.ReportRequiredMethodNotFound(
                methodOwnerFullName,
                methodName,
                parameterTypeNames
            );
        }

        return methodReference;
    }

    private void EmitConversionExpression(
        ILProcessor ilProcessor,
        TypedConversionExpression conversionExpression
    )
    {
        EmitExpression(ilProcessor, conversionExpression.Expression);

        var fromType = conversionExpression.Expression.Type;
        var toType = conversionExpression.Type;
        if (toType == Type.String)
        {
            if (fromType == Type.Bool)
            {
                ilProcessor.Emit(OpCodes.Call, _convertBoolToString);
                return;
            }

            if (fromType == Type.Int)
            {
                ilProcessor.Emit(OpCodes.Call, _convertInt32ToString);
                return;
            }

            if (fromType == Type.Char)
            {
                ilProcessor.Emit(OpCodes.Call, _convertCharToString);
                return;
            }
        }

        if (toType == Type.Any)
        {
            if (fromType == Type.Bool)
            {
                ilProcessor.Emit(OpCodes.Box, _knownTypes[TypeSymbol.Bool]);
                return;
            }

            if (fromType == Type.Int)
            {
                ilProcessor.Emit(OpCodes.Box, _knownTypes[TypeSymbol.Int]);
                return;
            }

            if (fromType == Type.Char)
            {
                ilProcessor.Emit(OpCodes.Box, _knownTypes[TypeSymbol.Char]);
                return;
            }

            if (fromType == Type.String)
            {
                // no conversion required
                return;
            }

            if (fromType == Type.Unit)
            {
                // pop the expression if it was a ()
                ilProcessor.Emit(OpCodes.Pop);
                ilProcessor.Emit(OpCodes.Ldsfld, _unit);
                return;
            }
        }

        if (toType == Type.Int)
        {
            if (fromType == Type.String)
            {
                ilProcessor.Emit(OpCodes.Call, _convertToInt32);
                return;
            }
        }

        throw new ArgumentOutOfRangeException(
            nameof(conversionExpression),
            $"Could not find conversion from '{fromType}' to '{toType}'"
        );
    }

    private void EmitUnaryExpression(ILProcessor ilProcessor, TypedUnaryExpression unaryExpression)
    {
        EmitExpression(ilProcessor, unaryExpression.Operand);
        switch (unaryExpression.Operator.Kind)
        {
            case TypedUnaryOperatorKind.Identity:
                // no op
                break;

            case TypedUnaryOperatorKind.Negation:
                // f(i) = -i
                ilProcessor.Emit(OpCodes.Neg);
                break;

            case TypedUnaryOperatorKind.LogicalNegation:
                // f(b) = !b (logical)
                ilProcessor.Emit(OpCodes.Ldc_I4_0);
                ilProcessor.Emit(OpCodes.Ceq);
                break;

            case TypedUnaryOperatorKind.BitwiseNegation:
                // f(i) = ~i
                ilProcessor.Emit(OpCodes.Not);
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void EmitUnitExpression(ILProcessor ilProcessor, TypedUnitExpression unitExpression)
    {
        ilProcessor.Emit(OpCodes.Ldsfld, _unit);
    }

    private void EmitFieldExpression(ILProcessor ilProcessor, TypedFieldExpression node)
    {
        // TODO: field can be a global which is in the `_globals` array
        // _fields are all possible fields for any type
        var variable = node.Field;
        var field = _globals.TryGetValue(variable, out var aField) ? aField : _fields[variable];

        if (node.Expression is TypedTypeExpression(_, _) || variable.IsStatic)
        {
            ilProcessor.Emit(OpCodes.Ldsfld, field);
            return;
        }

        if (node.Expression != null)
        {
            EmitExpression(ilProcessor, node.Expression);
            ilProcessor.Emit(OpCodes.Ldfld, field);
        }
        else
        {
            ilProcessor.Emit(OpCodes.Ldarg_0); // load this
            ilProcessor.Emit(OpCodes.Ldfld, field);
        }
    }

    private void EmitVariableExpression(
        ILProcessor ilProcessor,
        TypedVariableExpression variableExpression
    )
    {
        var variable = variableExpression.Variable;
        switch (variable)
        {
            case { IsParameter: true }:
                ilProcessor.Emit(OpCodes.Ldarg, variable.Index);
                break;

            case { IsLocal: true }:
                ilProcessor.Emit(OpCodes.Ldloc, _locals[variable]);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(variable));
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

    private TypeDefinition? FindTypeByName(string typeName, bool reportTypeDiagnostics = true)
    {
        var foundTypes = FindTypesByName(typeName);

        switch (foundTypes.Length)
        {
            case 0 when reportTypeDiagnostics:
                _diagnostics.ReportTypeNotFound(typeName);
                break;

            case 1:
                return foundTypes[0];

            default:
                if (reportTypeDiagnostics)
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

    private MethodReference? ResolveMethod(
        string typeName,
        string methodName,
        string[] parameterTypeNames,
        bool reportNotFoundDiagnostic = true
    )
    {
        // var types = FindTypesByName(typeName);
        var type = FindTypeByName(typeName, reportNotFoundDiagnostic);
        if (type == null)
            return null;

        foreach (var methodDefinition in type.Methods.Where(method => method.Name == methodName))
        {
            if (methodDefinition.Parameters.Count != parameterTypeNames.Length)
                continue;

            var matches = methodDefinition.Parameters
                .Select(p => p.ParameterType.FullName)
                .Zip(
                    parameterTypeNames,
                    (methodParam, searchParamName) => methodParam == searchParamName
                )
                .ToArray();

            var allParamsMatch = matches.All(matches => matches);

            if (!allParamsMatch)
            {
                continue;
            }

            return _assemblyDefinition.MainModule.ImportReference(methodDefinition);
        }

        if (reportNotFoundDiagnostic)
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
