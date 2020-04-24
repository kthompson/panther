using System;
using System.Linq;
using Mono.Cecil;
using System.Collections.Generic;
using System.Collections.Immutable;
using Mono.Cecil.Cil;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Emit
{
    internal class Emitter
    {
        private readonly DiagnosticBag _diagnostics = new DiagnosticBag();

        private readonly Dictionary<TypeSymbol, TypeReference> _knownTypes = new Dictionary<TypeSymbol, TypeReference>();
        private readonly MethodReference? _consoleWriteLineReference;
        private readonly AssemblyDefinition _assemblyDefinition;
        private readonly List<AssemblyDefinition> _assemblies = new List<AssemblyDefinition>();

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
            var typeDef = new TypeDefinition("", "Program", TypeAttributes.Abstract | TypeAttributes.Sealed, objectType);
            _assemblyDefinition.MainModule.Types.Add(typeDef);

            var unitType = _knownTypes[TypeSymbol.Unit];
            var mainMethod = new MethodDefinition("Main", MethodAttributes.Static | MethodAttributes.Private, unitType);
            typeDef.Methods.Add(mainMethod);

            var ilProcessor = mainMethod.Body.GetILProcessor();
            ilProcessor.Emit(OpCodes.Ldstr, "Hello world");
            ilProcessor.Emit(OpCodes.Call,  _consoleWriteLineReference);
            ilProcessor.Emit(OpCodes.Ret);

            _assemblyDefinition.EntryPoint = mainMethod;
            _assemblyDefinition.Write(outputPath);

            return _diagnostics.ToImmutableArray();
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