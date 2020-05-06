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
		// Code size 102 (0x66)
		.maxstack 1
		.entrypoint
		.locals (
			[0] string,
			[1] string,
			[2] string,
			[3] string,
			[4] string,
			[5] string,
			[6] string
		)

		IL_0000: ldc.i4.1
		IL_0001: call string [System.Private.CoreLib]System.Convert::ToString(bool)
		IL_0006: stloc.0
		IL_0007: ldloc.0
		IL_0008: call void [System.Console]System.Console::WriteLine(string)
		IL_000d: ldc.i4.0
		IL_000e: call string [System.Private.CoreLib]System.Convert::ToString(bool)
		IL_0013: stloc.1
		IL_0014: ldloc.1
		IL_0015: call void [System.Console]System.Console::WriteLine(string)
		IL_001a: ldc.i4.1
		IL_001b: call string [System.Private.CoreLib]System.Convert::ToString(int32)
		IL_0020: stloc.2
		IL_0021: ldloc.2
		IL_0022: call void [System.Console]System.Console::WriteLine(string)
		IL_0027: ldc.i4.1
		IL_0028: neg
		IL_0029: call string [System.Private.CoreLib]System.Convert::ToString(int32)
		IL_002e: stloc.3
		IL_002f: ldloc.3
		IL_0030: call void [System.Console]System.Console::WriteLine(string)
		IL_0035: ldc.i4.0
		IL_0036: call string [System.Private.CoreLib]System.Convert::ToString(int32)
		IL_003b: stloc.s 4
		IL_003d: ldloc.s 4
		IL_003f: call void [System.Console]System.Console::WriteLine(string)
		IL_0044: ldc.i4 1234567890
		IL_0049: call string [System.Private.CoreLib]System.Convert::ToString(int32)
		IL_004e: stloc.s 5
		IL_0050: ldloc.s 5
		IL_0052: call void [System.Console]System.Console::WriteLine(string)
		IL_0057: ldstr "hello"
		IL_005c: stloc.s 6
		IL_005e: ldloc.s 6
		IL_0060: call void [System.Console]System.Console::WriteLine(string)
		IL_0065: ret
	} // end of method Program::main

} // end of class Program


