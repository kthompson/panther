using System;

namespace Panther.CodeAnalysis.Binding.Environment;

/// <summary>
///
/// </summary>
/// <param name="Namespace"></param>
/// <param name="Name"></param>
/// <param name="BaseType"></param>
/// <param name="ContainingType">Specifies the type this type is defined in</param>
/// <param name="FieldList">Id of first field for this type</param>
/// <param name="MethodList">Id of first method for this type. Next type record defines the last</param>
record TypeRecord(
    string Namespace,
    string Name,
    int? BaseType,
    int? ContainingType,
    int FieldList,
    int MethodList
);
