// IL code: Multiply
.class private auto ansi '<Module>'
{
} // end of class <Module>

.class public auto ansi sealed $Program
	extends [System.Private.CoreLib]System.Object
{
	// Methods
	.method public static
		void main () cil managed
	{
		// Method begins at RVA 0x2050
		// Code size 52 (0x34)
		.maxstack 2
		.entrypoint
		.locals init (
			[0] int32,
			[1] int32,
			[2] object,
			[3] int32,
			[4] int32,
			[5] object
		)

		IL_0000: ldc.i4.1
		IL_0001: stloc.0
		IL_0002: ldc.i4.7
		IL_0003: stloc.1
		IL_0004: ldloc.0
		IL_0005: ldloc.1
		IL_0006: call int32 $Program::multiply(int32, int32)
		IL_000b: call string [System.Private.CoreLib]System.Convert::ToString(int32)
		IL_0010: stloc.2
		IL_0011: ldloc.2
		IL_0012: call void [Panther.StdLib]Panther.Predef::println(object)
		IL_0017: ldc.i4.2
		IL_0018: stloc.3
		IL_0019: ldc.i4.s 18
		IL_001b: stloc.s 4
		IL_001d: ldloc.3
		IL_001e: ldloc.s 4
		IL_0020: call int32 $Program::multiply(int32, int32)
		IL_0025: call string [System.Private.CoreLib]System.Convert::ToString(int32)
		IL_002a: stloc.s 5
		IL_002c: ldloc.s 5
		IL_002e: call void [Panther.StdLib]Panther.Predef::println(object)
		IL_0033: ret
	} // end of method $Program::main

	.method public static
		int32 multiply (
			int32 a,
			int32 b
		) cil managed
	{
		// Method begins at RVA 0x2090
		// Code size 4 (0x4)
		.maxstack 8

		IL_0000: ldarg.0
		IL_0001: ldarg.1
		IL_0002: mul
		IL_0003: ret
	} // end of method $Program::multiply

} // end of class $Program


