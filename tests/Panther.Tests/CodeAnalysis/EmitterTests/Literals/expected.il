// IL code: Literals
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
		// Code size 101 (0x65)
		.maxstack 1
		.entrypoint
		.locals (
			[0] object,
			[1] object,
			[2] object,
			[3] object,
			[4] object,
			[5] object,
			[6] object
		)

		IL_0000: ldc.i4.1
		IL_0001: call string [System.Private.CoreLib]System.Convert::ToString(bool)
		IL_0006: stloc.0
		IL_0007: ldloc.0
		IL_0008: call void [Panther.StdLib]Panther.Predef::println(object)
		IL_000d: ldc.i4.0
		IL_000e: call string [System.Private.CoreLib]System.Convert::ToString(bool)
		IL_0013: stloc.1
		IL_0014: ldloc.1
		IL_0015: call void [Panther.StdLib]Panther.Predef::println(object)
		IL_001a: ldc.i4.1
		IL_001b: call string [System.Private.CoreLib]System.Convert::ToString(int32)
		IL_0020: stloc.2
		IL_0021: ldloc.2
		IL_0022: call void [Panther.StdLib]Panther.Predef::println(object)
		IL_0027: ldc.i4.m1
		IL_0028: call string [System.Private.CoreLib]System.Convert::ToString(int32)
		IL_002d: stloc.3
		IL_002e: ldloc.3
		IL_002f: call void [Panther.StdLib]Panther.Predef::println(object)
		IL_0034: ldc.i4.0
		IL_0035: call string [System.Private.CoreLib]System.Convert::ToString(int32)
		IL_003a: stloc.s 4
		IL_003c: ldloc.s 4
		IL_003e: call void [Panther.StdLib]Panther.Predef::println(object)
		IL_0043: ldc.i4 1234567890
		IL_0048: call string [System.Private.CoreLib]System.Convert::ToString(int32)
		IL_004d: stloc.s 5
		IL_004f: ldloc.s 5
		IL_0051: call void [Panther.StdLib]Panther.Predef::println(object)
		IL_0056: ldstr "hello"
		IL_005b: stloc.s 6
		IL_005d: ldloc.s 6
		IL_005f: call void [Panther.StdLib]Panther.Predef::println(object)
		IL_0064: ret
	} // end of method Program::main

} // end of class Program


