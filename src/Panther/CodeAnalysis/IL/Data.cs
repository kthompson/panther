using System.Collections.Generic;

namespace Panther.CodeAnalysis.IL;

public record Data;

/// <summary>
/// Allocate an zeroed object with the specified Size
/// </summary>
/// <param name="Size"></param>
public record DataZero(int Size) : Data;

/// <summary>
/// Allocate a length prefixed string
/// </summary>
/// <param name="Value"></param>
public record DataString(string Value) : Data;

/// <summary>
/// Allocate an array of raw bytes
/// </summary>
/// <param name="Value"></param>
public record DataBytes(IReadOnlyList<byte> Value) : Data;