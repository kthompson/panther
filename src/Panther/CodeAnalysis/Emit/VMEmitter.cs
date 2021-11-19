using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Symbols;
using Type = Panther.CodeAnalysis.Symbols.Type;

namespace Panther.CodeAnalysis.Emit
{
    class VMEmitter
    {
        private readonly BoundAssembly _assembly;
        private readonly Dictionary<string, int> _localToIndex = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _parameterToIndex = new Dictionary<string, int>();
        private readonly Dictionary<string, Dictionary<string, int>> _typeToFieldToIndex = new();

        public VMEmitter(BoundAssembly assembly)
        {
            _assembly = assembly;
        }

        public static IEmitResult Emit(BoundAssembly assembly, string outputPath)
        {
            var emitter = new VMEmitter(assembly);
            return emitter.Emit(outputPath);
        }

        public IEmitResult Emit(string outputPath)
        {
            // define field offsets within classes
            foreach (var assemblyType in _assembly.Types.Where(t => t.IsClass))
            {
                var fieldToIndex = new Dictionary<string, int>();
                for (var index = 0; index < assemblyType.Fields.Length; index++)
                {
                    var assemblyTypeField = assemblyType.Fields[index];
                    fieldToIndex.Add(assemblyTypeField.Name, index);
                }
                _typeToFieldToIndex.Add(assemblyType.FullName, fieldToIndex);
            }

            foreach (var assemblyType in _assembly.Types)
            {
                var path = Path.Join(outputPath, assemblyType.Name + ".vm");
                using var processor = new VMProcessor(path);
                EmitType(assemblyType, processor);
            }

            return null!;
        }

        private int GetFieldIndex(Symbol type, Symbol field)
        {
            return _typeToFieldToIndex[type.FullName][field.Name];
        }

        private void EmitType(Symbol type, VMProcessor processor)
        {
            foreach (var method in type.Methods)
            {
                _parameterToIndex.Clear();
                var hasThis = method.IsStatic ? 0 : 1;
                for (var index = 0; index < method.Parameters.Length; index++)
                {
                    var parameter = method.Parameters[index];
                    _parameterToIndex.Add(parameter.Name, hasThis + index);
                }

                _localToIndex.Clear();
                for (var index = 0; index < method.Locals.Length; index++)
                {
                    var local = method.Locals[index];
                    _localToIndex.Add(local.Name, hasThis + index);
                }

                EmitMethod(type, method, processor);
            }
        }

        private void EmitMethod(Symbol type, Symbol method, VMProcessor processor)
        {
            var body = _assembly.MethodDefinitions[method];
            var methodName = $"{type.Name}.{method.Name}";
            processor.EmitFunction(methodName, method.Locals.Length);

            foreach (var statement in body.Statements)
            {
                EmitStatement(statement, processor);
            }

            EmitExpression(body.Expression, processor);
            processor.EmitReturn();
        }

        private void EmitExpression(BoundExpression expression, VMProcessor processor)
        {
            switch (expression)
            {
                case BoundBinaryExpression binaryExpression:
                    EmitBinaryExpression(binaryExpression, processor);
                    break;

                case BoundCallExpression callExpression:
                    EmitCallExpression(callExpression, processor);
                    break;

                case BoundFieldExpression fieldExpression:
                    EmitFieldExpression(fieldExpression, processor);
                    break;

                case BoundConversionExpression conversionExpression:
                    EmitConversionExpression(conversionExpression, processor);
                    break;

                case BoundUnaryExpression unaryExpression:
                    EmitUnaryExpression(unaryExpression, processor);
                    break;

                case BoundUnitExpression:
                    EmitUnitExpression(processor);
                    break;

                case BoundVariableExpression variableExpression:
                    EmitVariableExpression(variableExpression, processor);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(expression), expression.GetType().FullName);
            }
        }

        private void EmitUnaryExpression(BoundUnaryExpression expression, VMProcessor processor)
        {
            EmitExpression(expression.Operand, processor);

            switch (expression.Operator.Kind)
            {
                case BoundUnaryOperatorKind.Identity:
                    // No op
                    break;

                case BoundUnaryOperatorKind.Negation:
                    processor.EmitArithmetic(Arithmetic.Neg);
                    break;

                // logical negation only works on bools so this works the same as bitwise Not
                case BoundUnaryOperatorKind.LogicalNegation:
                    goto case BoundUnaryOperatorKind.BitwiseNegation;

                case BoundUnaryOperatorKind.BitwiseNegation:
                    processor.EmitArithmetic(Arithmetic.Not);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void EmitUnitExpression(VMProcessor processor)
        {
            // treat this as a nop
        }

        private void EmitVariableExpression(BoundVariableExpression expression, VMProcessor processor)
        {
            // variable can be a parameter, local, or field
            var variable = expression.Variable;
            switch (variable)
            {
                case { IsParameter: true }:
                {
                    processor.EmitPush(Segment.Argument, _parameterToIndex[variable.Name]);
                    break;
                }

                case { IsLocal: true }:
                {
                    processor.EmitPush(Segment.Local, _localToIndex[variable.Name]);
                    break;
                }

                // field on an object
                case { IsField: true, IsStatic: true }:
                    // we dont support accessing object/static fields at this time
                    // the static segment is something that is setup when you enter a method
                    // based on the class/object you are in so we can access fields from methods

                    throw new NotImplementedException();

                // field in a class
                case { IsField: true, IsStatic: false }:
                    // Variable expression can only access fields within their own type, otherwise they would be
                    // FieldExpressions therefore we can assume we use the `this` segment
                    var index = GetFieldIndex(variable.Owner, variable);
                    processor.EmitPop(Segment.This, index);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void EmitCallExpression(BoundCallExpression expression, VMProcessor processor)
        {
            var argCount = expression.Arguments.Length;
            if (expression.Expression != null && expression.Expression is not BoundTypeExpression)
            {
                argCount += 1;
                EmitExpression(expression.Expression, processor);
            }

            foreach (var argument in expression.Arguments)
            {
                EmitExpression(argument, processor);
            }

            processor.EmitCall(expression.Method.FullName, argCount);
        }

        private void EmitStatement(BoundStatement statement, VMProcessor processor)
        {
            switch (statement)
            {
                case BoundAssignmentStatement node:
                    EmitAssignmentStatement(node, processor);
                    break;
                case BoundConditionalGotoStatement node:
                    EmitConditionalGotoStatement(node, processor);
                    break;
                case BoundExpressionStatement node:
                    EmitExpressionStatement(node, processor);
                    break;
                case BoundGotoStatement node:
                    EmitGotoStatement(node, processor);
                    break;
                case BoundLabelStatement node:
                    EmitLabelStatement(node, processor);
                    break;
                case BoundNopStatement node:
                    EmitNopStatement(node, processor);
                    break;
                case BoundVariableDeclarationStatement node:
                    EmitVariableDeclarationStatement(node, processor);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(statement));
            }
        }

        private void EmitVariableDeclarationStatement(BoundVariableDeclarationStatement node, VMProcessor processor)
        {
            if (node.Expression != null)
                EmitAssignment(node.Variable, node.Expression, processor);
        }

        private void EmitAssignmentStatement(BoundAssignmentStatement node, VMProcessor processor)
        {
            EmitAssignment(node.Variable, node.Expression, processor);
        }

        private void EmitAssignment(Symbol variable, BoundExpression expression, VMProcessor processor)
        {
            EmitExpression(expression, processor);

            switch (variable)
            {
                case { IsParameter: true }:
                {
                    processor.EmitPop(Segment.Argument, _parameterToIndex[variable.Name]);
                    break;
                }

                case { IsLocal: true }:
                {
                    processor.EmitPop(Segment.Local, _localToIndex[variable.Name]);
                    break;
                }

                // field on an object
                case { IsField: true, IsStatic: true }:
                    throw new NotImplementedException();

                // field in a class
                case { IsField: true, IsStatic: false }:
                    throw new NotImplementedException();

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void EmitNopStatement(BoundNopStatement node, VMProcessor processor)
        {
            processor.EmitComment("nop");
        }

        private void EmitLabelStatement(BoundLabelStatement node, VMProcessor processor)
        {
            processor.EmitLabel(node.BoundLabel.Name);
        }

        private void EmitGotoStatement(BoundGotoStatement node, VMProcessor processor)
        {
            processor.EmitGotoLabel(node.BoundLabel.Name);
        }

        private void EmitExpressionStatement(BoundExpressionStatement node, VMProcessor processor)
        {
            EmitExpression(node.Expression, processor);

            // pop non-unit expressions off the stack as their result wont be used
            // unit expression have no result on the stack
            if (node.Expression.Type != Type.Unit)
                processor.EmitPop(Segment.Temp, 0);
        }

        private void EmitConditionalGotoStatement(BoundConditionalGotoStatement node, VMProcessor processor)
        {
            EmitExpression(node.Condition, processor);

            // in HACK false is 0 and true is -1
            if (!node.JumpIfTrue)
            {
                // invert the top of the stack to convert this into a branch-if-false
                processor.EmitArithmetic(Arithmetic.Not);
            }

            processor.EmitIfGotoLabel(node.BoundLabel.Name);
        }

        private void EmitFieldExpression(BoundFieldExpression expression, VMProcessor processor)
        {
            if (expression.Expression != null)
            {
                // class instance field access
                EmitExpression(expression.Expression, processor);
            }
            else
            {
                // static field access
            }

            // TODO: fetch field from the above type
            throw new NotImplementedException();
        }

        private void EmitConversionExpression(BoundConversionExpression expression, VMProcessor processor)
        {
            EmitExpression(expression.Expression, processor);
            throw new NotImplementedException();
        }

        private void EmitBinaryExpression(BoundBinaryExpression expression, VMProcessor processor)
        {
            EmitExpression(expression.Left, processor);
            EmitExpression(expression.Right, processor);

            switch (expression.Operator.Kind)
            {
                case BoundBinaryOperatorKind.Addition:
                    processor.EmitArithmetic(Arithmetic.Add);
                    break;

                case BoundBinaryOperatorKind.LogicalAnd: // if the inputs are bool then the output will be bool
                case BoundBinaryOperatorKind.BitwiseAnd:
                    processor.EmitArithmetic(Arithmetic.And);
                    break;

                case BoundBinaryOperatorKind.LogicalOr: // if the inputs are bool then the output will be bool
                case BoundBinaryOperatorKind.BitwiseOr:
                    processor.EmitArithmetic(Arithmetic.Or);
                    break;

                case BoundBinaryOperatorKind.Equal:
                    processor.EmitArithmetic(Arithmetic.Eq);
                    break;

                case BoundBinaryOperatorKind.GreaterThan:
                    processor.EmitArithmetic(Arithmetic.Gt);
                    break;

                case BoundBinaryOperatorKind.GreaterThanOrEqual:
                    // opposite of < is >= so perform < and then not
                    processor.EmitArithmetic(Arithmetic.Lt);
                    processor.EmitArithmetic(Arithmetic.Not);
                    break;

                case BoundBinaryOperatorKind.LessThan:
                    processor.EmitArithmetic(Arithmetic.Lt);
                    break;

                case BoundBinaryOperatorKind.LessThanOrEqual:
                    // opposite of > is <= so perform > and then not
                    processor.EmitArithmetic(Arithmetic.Gt);
                    processor.EmitArithmetic(Arithmetic.Not);
                    break;

                case BoundBinaryOperatorKind.NotEqual:
                    processor.EmitArithmetic(Arithmetic.Eq);
                    processor.EmitArithmetic(Arithmetic.Not);
                    break;

                case BoundBinaryOperatorKind.Subtraction:
                    processor.EmitArithmetic(Arithmetic.Sub);
                    break;

                case BoundBinaryOperatorKind.BitwiseXor:
                case BoundBinaryOperatorKind.Multiplication:
                case BoundBinaryOperatorKind.Division:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}