// IL code: GlobalStatements
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
		// Code size 17 (0x11)
		.maxstack 1
		.entrypoint
		.locals (
			[0] int32,
			[1] object
		)

		IL_0000: ldc.i4.s 12
		IL_0002: stloc.0
		IL_0003: ldloc.0
		IL_0004: call string [System.Private.CoreLib]System.Convert::ToString(int32)
		IL_0009: stloc.1
		IL_000a: ldloc.1
		IL_000b: call void [Panther.StdLib]Panther.Predef::println(object)
		IL_0010: ret
	} // end of method Program::main

} // end of class Program


