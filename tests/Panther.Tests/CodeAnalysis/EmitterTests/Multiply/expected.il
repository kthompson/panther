// IL code: Multiply
.class private auto ansi '<Module>'
{
} // end of class <Module>

.class private auto ansi abstract sealed Program
	extends [System.Private.CoreLib]System.Object
{
	// Methods
	.method private static
		void main () cil managed
	{
		// Method begins at RVA 0x2050
		// Code size 36 (0x24)
		.maxstack 8
		.entrypoint

		IL_0000: ldc.i4.1
		IL_0001: ldc.i4.7
		IL_0002: call int32 Program::multiply(int32, int32)
		IL_0007: call string [System.Private.CoreLib]System.Convert::ToString(int32)
		IL_000c: call void [System.Console]System.Console::WriteLine(string)
		IL_0011: ldc.i4.2
		IL_0012: ldc.i4.s 18
		IL_0014: call int32 Program::multiply(int32, int32)
		IL_0019: call string [System.Private.CoreLib]System.Convert::ToString(int32)
		IL_001e: call void [System.Console]System.Console::WriteLine(string)
		IL_0023: ret
	} // end of method Program::main

	.method private static
		int32 multiply (
			int32 a,
			int32 b
		) cil managed
	{
		// Method begins at RVA 0x2075
		// Code size 4 (0x4)
		.maxstack 8

		IL_0000: ldarg.0
		IL_0001: ldarg.1
		IL_0002: mul
		IL_0003: ret
	} // end of method Program::multiply

} // end of class Program


